// 
//  Copyright 2010  Deveel
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


using System;

using Deveel.Data.Collections;

namespace Deveel.Data {
	/// <summary>
	/// A composite of two or more datasets used to implement <see cref="CompositeFunction.Union"/>, 
	/// <see cref="CompositeFunction.Intersect"/>, and <see cref="CompositeFunction.Except"/>.
	/// </summary>
	public class CompositeTable : Table, IRootTable {

		// ---------- Members ----------

		/// <summary>
		/// The 'master table' used to resolve information about this table such as
		/// fields and field types.
		/// </summary>
		private readonly Table master_table;

		/// <summary>
		/// The tables being made a composite of.
		/// </summary>
		private readonly Table[] composite_tables;

		/// <summary>
		/// The list of indexes of rows to include in each table.
		/// </summary>
		private IntegerVector[] table_indexes;

		/// <summary>
		/// The schemes to describe the entity relation in the given column.
		/// </summary>
		private readonly SelectableScheme[] column_scheme;

		/// <summary>
		/// The number of root locks on this table.
		/// </summary>
		private int roots_locked;

		/// <summary>
		/// Constructs the composite table given the <paramref name="master_table"/> 
		/// (the column structure this composite table is based on), and 
		/// a list of tables to be the composite of this table.
		/// </summary>
		/// <param name="master_table">The table defining the master structure 
		/// for the composition. this must be one of the tables listed in
		/// <paramref name="composite_list"/>.</param>
		/// <param name="composite_list">The list of tables to compose given 
		/// the structure of the master table.</param>
		/// <remarks>
		/// <b>Note:</b> This does not set up table indexes for a composite 
		/// function.
		/// </remarks>
		public CompositeTable(Table master_table, Table[] composite_list)
			: base() {
			this.master_table = master_table;
			composite_tables = composite_list;
			column_scheme = new SelectableScheme[master_table.ColumnCount];
		}

		/// <summary>
		/// Consturcts the composite table assuming the first item in the 
		/// list is the master table.
		/// </summary>
		/// <param name="composite_list">The list of the tables to compose.</param>
		public CompositeTable(Table[] composite_list)
			: this(composite_list[0], composite_list) {
		}


		/// <summary>
		/// Removes duplicate rows from the table.
		/// </summary>
		/// <param name="pre_sorted">If <b>true</b>, each composite index 
		/// is already in sorted order.</param>
		private void RemoveDuplicates(bool pre_sorted) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Sets up the indexes in this composite table by performing for 
		/// composite function on the tables.
		/// </summary>
		/// <param name="function"></param>
		/// <param name="all">If <b>true</b>, duplicated rows are removed.</param>
		public void SetupIndexesForCompositeFunction(CompositeFunction function, bool all) {
			int size = composite_tables.Length;
			table_indexes = new IntegerVector[size];

			if (function == CompositeFunction.Union) {
				// Include all row sets in all tables
				for (int i = 0; i < size; ++i) {
					table_indexes[i] = composite_tables[i].SelectAll();
				}
				if (!all) {
					RemoveDuplicates(false);
				}
			} else {
				throw new ApplicationException("Unrecognised composite function");
			}

		}

		// ---------- Implemented from Table ----------

		/// <inheritdoc/>
		public override Database Database {
			get { return master_table.Database; }
		}

		/// <inheritdoc/>
		public override int ColumnCount {
			get { return master_table.ColumnCount; }
		}

		/// <inheritdoc/>
		public override int RowCount {
			get {
				int row_count = 0;
				for (int i = 0; i < table_indexes.Length; ++i) {
					row_count += table_indexes[i].Count;
				}
				return row_count;
			}
		}

		/// <inheritdoc/>
		public override int FindFieldName(VariableName v) {
			return master_table.FindFieldName(v);
		}

		/// <inheritdoc/>
		public override DataTableInfo DataTableInfo {
			get { return master_table.DataTableInfo; }
		}

		/// <inheritdoc/>
		public override VariableName GetResolvedVariable(int column) {
			return master_table.GetResolvedVariable(column);
		}

		/// <inheritdoc/>
		internal override SelectableScheme GetSelectableSchemeFor(int column, int originalColumn, Table table) {

			SelectableScheme scheme = column_scheme[column];
			if (scheme == null) {
				scheme = new BlindSearch(this, column);
				column_scheme[column] = scheme;
			}

			// If we are getting a scheme for this table, simple return the information
			// from the column_trees Vector.
			if (table == this) {
				return scheme;
			}
				// Otherwise, get the scheme to calculate a subset of the given scheme.
			else {
				return scheme.GetSubsetScheme(table, originalColumn);
			}
		}

		/// <inheritdoc/>
		internal override void SetToRowTableDomain(int column, IntegerVector rowSet,
								 ITableDataSource ancestor) {
			if (ancestor != this) {
				throw new Exception("Method routed to incorrect table ancestor.");
			}
		}

		/// <inheritdoc/>
		internal override RawTableInformation ResolveToRawTable(RawTableInformation info) {
			Console.Error.WriteLine("Efficiency Warning in DataTable.ResolveToRawTable.");
			IntegerVector row_set = new IntegerVector();
			IRowEnumerator e = GetRowEnumerator();
			while (e.MoveNext()) {
				row_set.AddInt(e.RowIndex);
			}
			info.Add(this, row_set);
			return info;
		}

		/// <inheritdoc/>
		public override TObject GetCellContents(int column, int row) {
			for (int i = 0; i < table_indexes.Length; ++i) {
				IntegerVector ivec = table_indexes[i];
				int sz = ivec.Count;
				if (row < sz) {
					return composite_tables[i].GetCellContents(column, ivec[row]);
				} else {
					row -= sz;
				}
			}
			throw new ApplicationException("Row '" + row + "' out of bounds.");
		}

		/// <inheritdoc/>
		public override IRowEnumerator GetRowEnumerator() {
			return new SimpleRowEnumerator(RowCount);
		}


		/// <inheritdoc/>
		public override void LockRoot(int lockKey) {
			// For each table, recurse.
			roots_locked++;
			for (int i = 0; i < composite_tables.Length; ++i) {
				composite_tables[i].LockRoot(lockKey);
			}
		}

		/// <inheritdoc/>
		public override void UnlockRoot(int lockKey) {
			// For each table, recurse.
			roots_locked--;
			for (int i = 0; i < composite_tables.Length; ++i) {
				composite_tables[i].UnlockRoot(lockKey);
			}
		}

		/// <inheritdoc/>
		public override bool HasRootsLocked {
			get { return roots_locked != 0; }
		}

		// ---------- Implemented from IRootTable ----------

		/// <inheritdoc/>
		public bool TypeEquals(IRootTable table) {
			return (this == table);
			//    return true;
		}
	}
}