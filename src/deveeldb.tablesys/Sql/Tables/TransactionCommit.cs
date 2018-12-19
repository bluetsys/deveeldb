﻿// 
//  Copyright 2010-2018 Deveel
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//

using System;
using System.Collections.Generic;
using System.Linq;

using Deveel.Data.Events;
using Deveel.Data.Sql.Constraints;
using Deveel.Data.Sql.Indexes;
using Deveel.Data.Transactions;

namespace Deveel.Data.Sql.Tables {
	class TransactionCommit {
		public TransactionCommit(TableSystemV2 tableSystem, ITransaction transaction, IEnumerable<ITableSource> selectedFromTables, IEnumerable<IMutableTable> touchedTables, ITransactionEventRegistry registry) {
			TableSystem = tableSystem;
			Transaction = transaction;

			SelectedFromTables = selectedFromTables;

			// Get individual journals for updates made to tables in this
			// transaction.
			// The list TableEventRegistry

			ChangedTables = touchedTables.Select(t => t.EventRegistry).Where(tableJournal => tableJournal.EventCount > 0);

			// The list of tables created by this journal.
			CreatedTables = registry.CreatedTables;
			// Ths list of tables dropped by this journal.
			DroppedTables = registry.DroppedTables;
			// The list of tables that constraints were alter by this journal
			ConstraintAlteredTables = registry.ConstraintAlteredTables;

			// Get the list of all database objects that were created in the
			// transaction.
			ObjectsCreated = registry.CreatedObjects;
			// Get the list of all database objects that were dropped in the
			// transaction.
			ObjectsDropped = registry.DroppedObjects;
		}

		public TableSystemV2 TableSystem { get; }

		public ITransaction Transaction { get; }

		public long Id => Transaction.CommitId;

		public IEnumerable<ITableSource> SelectedFromTables { get; }

		public IEnumerable<int> CreatedTables { get; }

		public IEnumerable<int> DroppedTables { get; }

		public IEnumerable<int> ConstraintAlteredTables { get; }

		public IEnumerable<ITableEventRegistry> ChangedTables { get; }

		public IEnumerable<ObjectName> ObjectsCreated { get; }

		public IEnumerable<ObjectName> ObjectsDropped { get; }

		public bool HasTableChanges => CreatedTables.Any() || DroppedTables.Any() || ConstraintAlteredTables.Any() || ChangedTables.Any();

		public bool Done { get; private set; }

		private static bool CommitTableListContains(IEnumerable<CommitTableInfo> list, TableSource master) {
			return list.Any(info => info.Master.Equals(master));
		}

		private CommitTableInfo[] GetNormalizedChangedTables() {
			// Create a normalized list of TableSource of all tables that
			// were either changed (and not dropped), and created (and not dropped).
			// This list represents all tables that are either new or changed in
			// this transaction.

			var normalizedChangedTables = new List<CommitTableInfo>(8);

			// Add all tables that were changed and not dropped in this transaction.

			normalizedChangedTables.AddRange(
				ChangedTables.Select(tableJournal => new { tableJournal, tableId = tableJournal.TableId })
					.Where(t => !DroppedTables.Contains(t.tableId))
					.Select(t => new { t, source = TableSystem.GetTableSource(t.tableId) })
					.Select(t => new CommitTableInfo {
						Master = t.source,
						Journal = t.t.tableJournal,
						ChangesSinceCommit = t.source.FindChangesSinceCommit(Id).ToArray()
					}));

			// Add all tables that were created and not dropped in this transaction.
			foreach (var tableId in CreatedTables) {
				// If this table is not dropped in this transaction then this is a
				// new table in this transaction.
				if (!DroppedTables.Contains(tableId)) {
					TableSource masterTable = TableSystem.GetTableSource(tableId);
					if (!CommitTableListContains(normalizedChangedTables, masterTable)) {

						// This is for entries that are created but modified (no journal).
						var tableInfo = new CommitTableInfo {
							Master = masterTable
						};

						normalizedChangedTables.Add(tableInfo);
					}
				}
			}

			return normalizedChangedTables.ToArray();
		}

		private TableSource[] GetNormalizedDroppedTables() {
			// Create a normalized list of TableSource of all tables that
			// were dropped (and not created) in this transaction.  This list
			// represents tables that will be dropped if the transaction
			// successfully commits.

			var normalizedDroppedTables = new List<TableSource>(8);
			foreach (var tableId in DroppedTables) {
				// Was this dropped table also created?  If it was created in this
				// transaction then we don't care about it.
				if (!CreatedTables.Contains(tableId)) {
					TableSource masterTable = TableSystem.GetTableSource(tableId);
					normalizedDroppedTables.Add(masterTable);
				}
			}

			return normalizedDroppedTables.ToArray();
		}

		private ITable[] FindChangedTables(ITransaction checkTransaction, CommitTableInfo[] normalizedChangedTables) {
			var changedTableSource = new ITable[normalizedChangedTables.Length];

			// Set up the above arrays
			for (int i = 0; i < normalizedChangedTables.Length; ++i) {
				// Get the information for this changed table
				CommitTableInfo tableInfo = normalizedChangedTables[i];

				// Get the master table that changed from the normalized list.
				TableSource master = tableInfo.Master;
				// Did this table change since the transaction started?
				var allTableChanges = tableInfo.ChangesSinceCommit;

				if (allTableChanges == null || allTableChanges.Length == 0) {
					// No changes so we can pick the correct IIndexSet from the current
					// transaction.

					// Get the state of the changed tables from the Transaction
					var mtable = Transaction.GetMutableTable(master.TableInfo.TableName);
					// Get the current index set of the changed table
					tableInfo.IndexSet = Transaction.GetIndexSetForTable(master);
					// Flush all index changes in the table
					mtable.FlushIndexes();

					// Set the 'check_transaction' object with the latest version of the
					// table.
					checkTransaction.UpdateVisibleTable(tableInfo.Master, tableInfo.IndexSet);
				} else {
					// There were changes so we need to merge the changes with the
					// current view of the table.

					// It's not immediately obvious how this merge update works, but
					// basically what happens is we WriteByte the table journal with all the
					// changes into a new IMutableTableDataSource of the current
					// committed state, and then we flush all the changes into the
					// index and then update the 'check_transaction' with this change.

					// Create the IMutableTableDataSource with the changes from this
					// journal.
					var mtable = master.GetMutableTable(checkTransaction, tableInfo.Journal);
					// Get the current index set of the changed table
					tableInfo.IndexSet = checkTransaction.GetIndexSetForTable(master);
					// Flush all index changes in the table
					mtable.FlushIndexes();

					// Dispose the table
					mtable.Dispose();
				}

				// And now refresh the 'changedTableSource' entry
				changedTableSource[i] = checkTransaction.GetTable(master.TableInfo.TableName);
			}

			return changedTableSource;
		}

		private void FireChangeEvents(CommitTableInfo[] normalizedChangedTables) {
			foreach (var tableInfo in normalizedChangedTables) {
				// Get the journal that details the change to the table.
				var changeJournal = tableInfo.Journal;
				if (changeJournal != null) {
					// Get the table name
					var tableName = tableInfo.Master.TableInfo.TableName;
					Transaction.RaiseEvent<TableCommitEvent>(tableName, tableInfo.Master.TableId, tableInfo.NormalizedAddedRows, tableInfo.NormalizedRemovedRows);
				}
			}
		}

		private void CheckConstraintViolations(ITransaction checkTransaction, CommitTableInfo[] normalizedChangedTables, ITable[] changedTableSource) {
			// Any tables that the constraints were altered for we need to check
			// if any rows in the table violate the new constraints.
			foreach (var tableId in ConstraintAlteredTables) {
				// We need to check there are no constraint violations for all the
				// rows in the table.
				for (int n = 0; n < normalizedChangedTables.Length; ++n) {
					CommitTableInfo tableInfo = normalizedChangedTables[n];
					if (tableInfo.Master.TableId == tableId) {
						checkTransaction.CheckAddConstraintViolations(changedTableSource[n], ConstraintDeferrability.InitiallyDeferred);
					}
				}
			}

			// For each changed table we must determine the rows that
			// were deleted and perform the remove constraint checks on the
			// deleted rows.  Note that this happens after the records are
			// removed from the index.

			// For each changed table,
			for (int i = 0; i < normalizedChangedTables.Length; ++i) {
				CommitTableInfo tableInfo = normalizedChangedTables[i];
				// Get the journal that details the change to the table.
				var changeJournal = tableInfo.Journal;
				if (changeJournal != null) {
					// Find the normalized deleted rows.
					var normalizedRemovedRows = changeJournal.GetRemovedRows();
					// Check removing any of the data doesn't cause a constraint
					// violation.
					checkTransaction.CheckRemoveConstraintViolations(changedTableSource[i], normalizedRemovedRows, ConstraintDeferrability.InitiallyDeferred);

					// Find the normalized added rows.
					var normalizedAddedRows = changeJournal.GetAddedRows();
					// Check adding any of the data doesn't cause a constraint
					// violation.
					checkTransaction.CheckAddConstraintViolations(changedTableSource[i], normalizedAddedRows, ConstraintDeferrability.InitiallyDeferred);

					// Set up the list of added and removed rows
					tableInfo.NormalizedAddedRows = normalizedAddedRows;
					tableInfo.NormalizedRemovedRows = normalizedRemovedRows;

				}
			}
		}

		private void AssertNoDirtySelect() {
			// We only perform this check if transaction error on dirty selects
			// are enabled.
			if (Transaction.ErrorOnDirtySelect()) {
				// For each table that this transaction selected from, if there are
				// any committed changes then generate a transaction error.
				foreach (TableSource selectedTable in SelectedFromTables) {
					// Find all committed journals equal to or greater than this
					// transaction's commit_id.
					var journalsSince = selectedTable.FindChangesSinceCommit(Id);
					if (journalsSince.Any()) {
						// Yes, there are changes so generate transaction error and
						// rollback.
						throw new DirtySelectException(selectedTable.TableInfo.TableName);
					}
				}
			}
		}

		private void CheckConflicts(IEnumerable<ObjectCommitState> namespaceJournals) {
			AssertNoDirtySelect();

			// Check there isn't a namespace clash with database objects.
			// We need to create a list of all create and drop activity in the
			// Composite from when the transaction started.
			var allDroppedObs = new List<ObjectName>();
			var allCreatedObs = new List<ObjectName>();
			foreach (var nsJournal in namespaceJournals) {
				if (nsJournal.CommitId >= Id) {
					allDroppedObs.AddRange(nsJournal.DroppedObjects);
					allCreatedObs.AddRange(nsJournal.CreatedObjects);
				}
			}

			// The list of all dropped objects since this transaction
			// began.
			bool conflict5 = false;
			ObjectName conflictName = null;
			string conflictDesc = "";
			foreach (ObjectName droppedOb in allDroppedObs) {
				if (ObjectsDropped.Contains(droppedOb)) {
					conflict5 = true;
					conflictName = droppedOb;
					conflictDesc = "dropped";
				}
			}
			// The list of all created objects since this transaction
			// began.
			foreach (ObjectName createdOb in allCreatedObs) {
				if (ObjectsCreated.Contains(createdOb)) {
					conflict5 = true;
					conflictName = createdOb;
					conflictDesc = "created";
				}
			}
			if (conflict5) {
				// Namespace conflict...
				throw new ObjectDuplicatedConflictException(conflictName, conflictDesc);
			}

			// For each journal,
			foreach (var changeJournal in ChangedTables) {
				// The table the change was made to.
				int tableId = changeJournal.TableId;
				// Get the master table with this table id.
				TableSource master = TableSystem.GetTableSource(tableId);

				// True if the state contains a committed resource with the given name
				bool committedResource = TableSystem.ContainsVisibleResource(tableId);

				// Check this table is still in the committed tables list.
				if (!CreatedTables.Contains(tableId) && !committedResource) {
					// This table is no longer a committed table, so rollback
					throw new NonCommittedConflictException(master.TableInfo.TableName);
				}

				// Since this journal was created, check to see if any changes to the
				// tables have been committed since.
				// This will return all journals on the table with the same commit_id
				// or greater.
				var journalsSince = master.FindChangesSinceCommit(Id);

				// For each journal, determine if there's any clashes.
				foreach (var tableJournal in journalsSince) {
					// This will thrown an exception if a commit classes.
					if (changeJournal.TestCommitClash(tableJournal, out var conflict))
						throw new RowRemoveConflictException(master.TableName, conflict.RowId);
				}
			}

			// Look at the transaction journal, if a table is dropped that has
			// journal entries since the last commit then we have an exception
			// case.
			foreach (int tableId in DroppedTables) {
				// Get the master table with this table id.
				TableSource master = TableSystem.GetTableSource(tableId);
				// Any journal entries made to this dropped table?
				if (master.FindChangesSinceCommit(Id).Any()) {
					// Oops, yes, rollback!
					throw new DroppedModifiedObjectConflictException(master.TableInfo.TableName);
				}
			}
		}

		public IEnumerable<TableSource> Execute(IList<ObjectCommitState> objectStates) {
			var changedTablesList = new List<TableSource>();

			// This is a transaction that will represent the view of the database
			// at the end of the commit
			ITransaction checkTransaction = null;

			bool entriesCommitted = false;

			try {
				// ---- Commit check stage ----
				CheckConflicts(objectStates);

				// Tests passed so go on to commit,

				// ---- Commit stage ----

				var normalizedChangedTables = GetNormalizedChangedTables();
				var normalizedDroppedTables = GetNormalizedDroppedTables();

				// We now need to create a ITransaction object that we
				// use to send to the triggering mechanism.  This
				// object represents a very specific view of the
				// transaction.  This view contains the latest version of changed
				// tables in this transaction.  It also contains any tables that have
				// been created by this transaction and does not contain any tables
				// that have been dropped.  Any tables that have not been touched by
				// this transaction are shown in their current committed state.
				// To summarize - this view is the current view of the database plus
				// any modifications made by the transaction that is being committed.

				// How this works - All changed tables are merged with the current
				// committed table.  All created tables are added into check_transaction
				// and all dropped tables are removed from check_transaction.  If
				// there were no other changes to a table between the time the
				// transaction was created and now, the view of the table in the
				// transaction is used, otherwise the latest changes are merged.

				// Note that this view will be the view that the database will
				// ultimately become if this transaction successfully commits.  Also,
				// you should appreciate that this view is NOT exactly the same as
				// the current trasaction view because any changes that have been
				// committed by concurrent transactions will be reflected in this view.

				// Create a new transaction of the database which will represent the
				// committed view if this commit is successful.
				checkTransaction = TableSystem.Database.CreateTransaction(IsolationLevel.Serializable);

				// Overwrite this view with tables from this transaction that have
				// changed or have been added or dropped.

				// (Note that order here is important).  First drop any tables from
				// this view.
				foreach (TableSource masterTable in normalizedDroppedTables) {
					// Drop this table in the current view
					checkTransaction.RemoveVisibleTable(masterTable);
				}

				// Now add any changed tables to the view.

				// Represents view of the changed tables
				var changedTableSource = FindChangedTables(checkTransaction, normalizedChangedTables);

				// The 'checkTransaction' now represents the view the database will be
				// if the commit succeeds.  We Lock 'checkTransaction' so it is
				// Read-only (the view is immutable).
				checkTransaction.ReadOnly(true);

				CheckConstraintViolations(checkTransaction, normalizedChangedTables, changedTableSource);

				// Deferred trigger events.
				FireChangeEvents(normalizedChangedTables);

				// NOTE: This isn't as fail safe as it could be.  We really need to
				//  do the commit in two phases.  The first writes updated indices to
				//  the index files.  The second updates the header pointer for the
				//  respective table.  Perhaps we can make the header update
				//  procedure just one file Write.

				// Finally, at this point all constraint checks have passed and the
				// changes are ready to finally be committed as permanent changes
				// to the Composite.  All that needs to be done is to commit our
				// IIndexSet indices for each changed table as final.
				// ISSUE: Should we separate the 'committing of indexes' changes and
				//   'committing of delete/add flags' to make the FS more robust?
				//   It would be more robust if all indexes are committed in one go,
				//   then all table flag data.

				// Set flag to indicate we have committed entries.
				entriesCommitted = true;

				// For each change to each table,
				foreach (CommitTableInfo tableInfo in normalizedChangedTables) {
					// Get the journal that details the change to the table.
					var changeJournal = tableInfo.Journal;
					if (changeJournal != null) {
						// Get the master table with this table id.
						TableSource master = tableInfo.Master;
						// Commit the changes to the table.
						// We use 'this.commit_id' which is the current commit level we are
						// at.
						master.CommitTransactionChange(TableSystem.Database.OpenTransactions.CurrentCommitId, changeJournal, tableInfo.IndexSet);
						// Add to 'changed_tables_list'
						changedTablesList.Add(master);
					}
				}

				// Only do this if we've created or dropped tables.
				if (CreatedTables.Any() || DroppedTables.Any()) {
					// Update the committed tables in the Composite state.
					// This will update and synchronize the headers in this Composite.
					TableSystem.CommitToTables(CreatedTables, DroppedTables);
				}

				// Update the namespace clash list
				if (ObjectsCreated.Any() || ObjectsDropped.Any()) {
					objectStates.Add(new ObjectCommitState(Id, ObjectsCreated, ObjectsDropped));
				}
			} finally {
				try {
					// If entries_committed == false it means we didn't get to a point
					// where any changed tables were committed.  Attempt to rollback the
					// changes in this transaction if they haven't been committed yet.
					if (entriesCommitted == false) {
						// For each change to each table,
						foreach (ITableEventRegistry changeJournal in ChangedTables) {
							// The table the changes were made to.
							int tableId = changeJournal.TableId;
							// Get the master table with this table id.
							TableSource master = TableSystem.GetTableSource(tableId);
							// Commit the rollback on the table.
							master.RollbackTransactionChange(changeJournal);
						}

						// TODO: Notify the system we're rolling back
					}
				} finally {
					try {
						// Dispose the 'checkTransaction'
						if (checkTransaction != null) {
							checkTransaction.Dispose();
							TableSystem.CloseTransaction(checkTransaction);
						}
						// Always ensure a transaction close, even if we have an exception.
						// Notify the Composite that this transaction has closed.
						TableSystem.CloseTransaction(Transaction);
					} catch (Exception) {
						// TODO: notify the error
					} finally {
						Done = true;
					}
				}
			}

			return changedTablesList.ToArray();
		}

		#region CommitTableInfo

		
		/// <summary>
		/// A static container class for information collected about a table 
		/// during the commit cycle.
		/// </summary>
		private sealed class CommitTableInfo {
			// The master table
			public TableSource Master;
			// The immutable index set
			public IRowIndexSet IndexSet;
			// The journal describing the changes to this table by this
			// transaction.
			public ITableEventRegistry Journal;
			// A list of journals describing changes since this transaction
			// started.
			public ITableEventRegistry[] ChangesSinceCommit;
			// Break down of changes to the table
			// Normalized list of row ids that were added
			public long[] NormalizedAddedRows;
			// Normalized list of row ids that were removed
			public long[] NormalizedRemovedRows;
		}

		#endregion
	}
}