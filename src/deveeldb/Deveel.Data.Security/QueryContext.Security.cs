﻿using System;
using System.Linq;

using Deveel.Data.Routines;
using Deveel.Data.Services;
using Deveel.Data.Sql;
using Deveel.Data.Sql.Expressions;
using Deveel.Data.Sql.Query;
using Deveel.Data.Sql.Tables;

namespace Deveel.Data.Security {
	public static class QueryContext {
		private static IUserManager UserManager(this IQueryContext context) {
			return context.ResolveService<IUserManager>();
		}

		private static IPrivilegeManager PrivilegeManager(this IQueryContext context) {
			return context.ResolveService<IPrivilegeManager>();
		}

		#region Group Management

		public static void CreateUserGroup(this IQueryContext context, string groupName) {
			if (!context.UserCanManageGroups())
				throw new InvalidOperationException(String.Format("User '{0}' has not enough privileges to create a group.", context.UserName()));

			context.ForSystemUser().UserManager().CreateUserGroup(groupName);
		}

		#endregion

		#region User Management

		public static User GetUser(this IQueryContext context, string userName) {
			if (context.UserName().Equals(userName, StringComparison.OrdinalIgnoreCase))
				return new User(context, userName);

			if (!context.UserCanAccessUsers())
				throw new MissingPrivilegesException(context.UserName(), new ObjectName(userName), Privileges.Select,
					String.Format("The user '{0}' has not enough rights to access other users information.", context.UserName()));

			if (!context.ForSystemUser().UserManager().UserExists(userName))
				return null;

			return new User(context, userName);
		}

		public static void SetUserStatus(this IQueryContext queryContext, string username, UserStatus status) {
			if (!queryContext.UserCanManageUsers())
				throw new MissingPrivilegesException(queryContext.UserName(), new ObjectName(username), Privileges.Alter,
					String.Format("User '{0}' cannot change the status of user '{1}'", queryContext.UserName(), username));

			queryContext.ForSystemUser().UserManager().SetUserStatus(username, status);
		}

		public static UserStatus GetUserStatus(this IQueryContext queryContext, string userName) {
			if (!queryContext.UserName().Equals(userName) &&
				!queryContext.UserCanAccessUsers())
				throw new MissingPrivilegesException(queryContext.UserName(), new ObjectName(userName), Privileges.Select,
					String.Format("The user '{0}' has not enough rights to access other users information.", queryContext.UserName()));

			return queryContext.ForSystemUser().UserManager().GetUserStatus(userName);
		}

		public static void SetUserGroups(this IQueryContext context, string userName, string[] groups) {
			if (!context.UserCanManageUsers())
				throw new MissingPrivilegesException(context.UserName(), new ObjectName(userName), Privileges.Alter,
					String.Format("The user '{0}' has not enough rights to modify other users information.", context.UserName()));

			// TODO: Check if the user exists?

			var userGroups = context.ForSystemUser().UserManager().GetUserGroups(userName);
			foreach (var userGroup in userGroups) {
				context.ForSystemUser().UserManager().RemoveUserFromGroup(userName, userGroup);
			}

			foreach (var userGroup in groups) {
				context.ForSystemUser().UserManager().AddUserToGroup(userName, userGroup, false);
			}
		}

		public static bool UserExists(this IQueryContext context, string userName) {
			return context.ForSystemUser().UserManager().UserExists(userName);
		}

		public static void CreatePublicUser(this IQueryContext context) {
			if (!context.User().IsSystem)
				throw new InvalidOperationException("The @PUBLIC user can be created only by the SYSTEM");

			var userName = User.PublicName;
			var userId = UserIdentification.PlainText;
			var userInfo = new UserInfo(userName, userId);

			context.ForSystemUser().UserManager().CreateUser(userInfo, "####");
		}

		public static User CreateUser(this IQueryContext context, string userName, string password) {
			if (String.IsNullOrEmpty(userName))
				throw new ArgumentNullException("userName");
			if (String.IsNullOrEmpty(password))
				throw new ArgumentNullException("password");

			if (!context.UserCanCreateUsers())
				throw new MissingPrivilegesException(userName, new ObjectName(userName), Privileges.Create,
					String.Format("User '{0}' cannot create users.", context.UserName()));

			if (String.Equals(userName, User.PublicName, StringComparison.OrdinalIgnoreCase))
				throw new ArgumentException(
					String.Format("User name '{0}' is reserved and cannot be registered.", User.PublicName), "userName");

			if (userName.Length <= 1)
				throw new ArgumentException("User name must be at least one character.");
			if (password.Length <= 1)
				throw new ArgumentException("The password must be at least one character.");

			var c = userName[0];
			if (c == '#' || c == '@' || c == '$' || c == '&')
				throw new ArgumentException(
					String.Format("User name '{0}' is invalid: cannot start with '{1}' character.", userName, c), "userName");

			var userId = UserIdentification.PlainText;
			var userInfo = new UserInfo(userName, userId);

			context.ForSystemUser().UserManager().CreateUser(userInfo, password);

			return new User(context, userName);
		}

		public static void AlterUserPassword(this IQueryContext queryContext, string username, string password) {
			if (!queryContext.UserCanAlterUser(username))
				throw new MissingPrivilegesException(queryContext.UserName(), new ObjectName(username), Privileges.Alter);

			var userId = UserIdentification.PlainText;
			var userInfo = new UserInfo(username, userId);

			queryContext.ForSystemUser().UserManager().AlterUser(userInfo, password);
		}

		public static bool DeleteUser(this IQueryContext context, string userName) {
			if (String.IsNullOrEmpty(userName))
				throw new ArgumentNullException("userName");

			if (!context.UserCanDropUser(userName))
				throw new MissingPrivilegesException(context.UserName(), new ObjectName(userName), Privileges.Drop);

			return context.ForSystemUser().UserManager().DropUser(userName);
		}

		public static void RemoveUserFromAllGroups(this IQueryContext context, string username) {
			var userExpr = SqlExpression.Constant(DataObject.String(username));

			var table = context.GetMutableTable(SystemSchema.UserGroupTableName);
			var c1 = table.GetResolvedColumnName(0);
			var t = table.SimpleSelect(context, c1, SqlExpressionType.Equal, userExpr);
			table.Delete(t);
		}

		/// <summary>
		/// Authenticates the specified user using the provided credentials.
		/// </summary>
		/// <param name="queryContext">The query context.</param>
		/// <param name="username">The name of the user to authenticate.</param>
		/// <param name="password">The password used to authenticate the user.</param>
		/// <returns></returns>
		/// <exception cref="System.ArgumentNullException">
		/// If either <paramref name="username"/> or <paramref name="password"/> are
		/// <c>null</c> or empty.
		/// </exception>
		/// <exception cref="SecurityException">
		/// If the authentication was not successful for the credentials provided.
		/// </exception>
		/// <exception cref="System.NotImplementedException">The external authentication mechanism is not implemented yet</exception>
		public static User Authenticate(this IQueryContext queryContext, string username, string password) {
			try {
				if (String.IsNullOrEmpty(username))
					throw new ArgumentNullException("username");
				if (String.IsNullOrEmpty(password))
					throw new ArgumentNullException("password");

				var userInfo = queryContext.ForSystemUser().UserManager().GetUser(username);

				if (userInfo == null)
					return null;

				var userId = userInfo.Identification;

				if (userId.Method != "plain")
					throw new NotImplementedException();

				if (!queryContext.ForSystemUser().UserManager().CheckIdentifier(username, password))
					return null;

				// Successfully authenticated...
				return new User(queryContext, username);
			} catch (SecurityException) {
				throw;
			} catch (Exception ex) {
				throw new SecurityException("Could not authenticate user.", ex);
			}
		}

		#region User Grants Management

		public static void AddUserToGroup(this IQueryContext queryContext, string username, string group, bool asAdmin = false) {
			if (String.IsNullOrEmpty(@group))
				throw new ArgumentNullException("group");
			if (String.IsNullOrEmpty(username))
				throw new ArgumentNullException("username");

			if (!queryContext.UserCanAddToGroup(group))
				throw new SecurityException();

			queryContext.ForSystemUser().UserManager().AddUserToGroup(username, group, asAdmin);
		}

		public static void GrantToUserOn(this IQueryContext context, ObjectName objectName, string grantee, Privileges privileges, bool withOption = false) {
			var obj = context.FindObject(objectName);
			if (obj == null)
				throw new ObjectNotFoundException(objectName);

			context.GrantToUserOn(obj.ObjectType, obj.FullName, grantee, privileges, withOption);
		}

		public static void GrantToUserOn(this IQueryContext context, DbObjectType objectType, ObjectName objectName, string grantee, Privileges privileges, bool withOption = false) {
			if (String.Equals(grantee, User.SystemName))       // The @SYSTEM user does not need any other
				return;

			if (!context.ObjectExists(objectType, objectName))
				throw new ObjectNotFoundException(objectName);

			if (!context.UserHasGrantOption(objectType, objectName, privileges))
				throw new MissingPrivilegesException(context.UserName(), objectName, privileges);

			var granter = context.UserName();
			var grant = new Grant(privileges, objectName, objectType, granter, withOption);
			context.ForSystemUser().PrivilegeManager().GrantToUser(grantee, grant);
		}

		public static void GrantToUserOnSchema(this IQueryContext context, string schemaName, string grantee, Privileges privileges, bool withOption = false) {
			context.GrantToUserOn(DbObjectType.Schema, new ObjectName(schemaName), grantee, privileges, withOption);
		}

		public static void GrantToGroupOn(this IQueryContext context, DbObjectType objectType, ObjectName objectName, string groupName, Privileges privileges, bool withOption = false) {
			if (SystemGroups.IsSystemGroup(groupName))
				throw new InvalidOperationException("Cannot grant to a system group.");

			if (!context.UserCanManageGroups())
				throw new MissingPrivilegesException(context.UserName(), new ObjectName(groupName));

			if (!context.ObjectExists(objectType, objectName))
				throw new ObjectNotFoundException(objectName);

			var granter = context.UserName();
			var grant = new Grant(privileges, objectName, objectType, granter, withOption);
			context.ForSystemUser().PrivilegeManager().GrantToGroup(groupName, grant);
		}

		public static void GrantTo(this IQueryContext context, string groupOrUserName, DbObjectType objectType, ObjectName objectName, Privileges privileges, bool withOption = false) {
			if (context.ForSystemUser().UserManager().UserGroupExists(groupOrUserName)) {
				if (withOption)
					throw new SecurityException("User groups cannot be granted with grant option.");

				context.GrantToGroupOn(objectType, objectName, groupOrUserName, privileges);
			} else if (context.ForSystemUser().UserManager().UserExists(groupOrUserName)) {
				context.GrantToUserOn(objectType, objectName, groupOrUserName, privileges, withOption);
			} else {
				throw new SecurityException(String.Format("User or group '{0}' was not found.", groupOrUserName));
			}
		}

		public static void RevokeAllGrantsOnTable(this IQueryContext context, ObjectName objectName) {
			RevokeAllGrantsOn(context, DbObjectType.Table, objectName);
		}

		public static void RevokeAllGrantsOnView(this IQueryContext context, ObjectName objectName) {
			context.RevokeAllGrantsOn(DbObjectType.View, objectName);
		}

		public static void RevokeAllGrantsOn(this IQueryContext context, DbObjectType objectType, ObjectName objectName) {
			var grantTable = context.GetMutableTable(SystemSchema.UserGrantsTableName);

			var objectTypeColumn = grantTable.GetResolvedColumnName(1);
			var objectNameColumn = grantTable.GetResolvedColumnName(2);
			// All that match the given object
			var t1 = grantTable.SimpleSelect(context, objectTypeColumn, SqlExpressionType.Equal,
				SqlExpression.Constant(DataObject.Integer((int)objectType)));
			// All that match the given parameter
			t1 = t1.SimpleSelect(context, objectNameColumn, SqlExpressionType.Equal,
				SqlExpression.Constant(DataObject.String(objectName.FullName)));

			// Remove these rows from the table
			grantTable.Delete(t1);
		}

		public static void GrantToUserOnTable(this IQueryContext context, ObjectName tableName, string grantee, Privileges privileges) {
			context.GrantToUserOn(DbObjectType.Table, tableName, grantee, privileges);
		}

		#endregion

		#endregion

		#region User Grants Query

		public static string[] GetGroupsUserBelongsTo(this IQueryContext queryContext, string username) {
			return queryContext.ForSystemUser().UserManager().GetUserGroups(username);
		}

		public static bool UserBelongsToGroup(this IQueryContext queryContext, string group) {
			return UserBelongsToGroup(queryContext, queryContext.UserName(), group);
		}

		public static bool UserBelongsToGroup(this IQueryContext context, string username, string groupName) {
			return context.ForSystemUser().UserManager().IsUserInGroup(username, groupName);
		}

		public static bool UserCanManageGroups(this IQueryContext context) {
			return context.User().IsSystem || context.UserHasSecureAccess();
		}

		public static bool UserHasSecureAccess(this IQueryContext context) {
			if (context.User().IsSystem)
				return true;

			return context.UserBelongsToSecureGroup();
		}

		public static bool UserBelongsToSecureGroup(this IQueryContext context) {
			return context.UserBelongsToGroup(SystemGroups.SecureGroup);
		}

		public static bool UserHasGrantOption(this IQueryContext context, DbObjectType objectType, ObjectName objectName, Privileges privileges) {
			var user = context.User();
			if (user.IsSystem)
				return true;

			if (context.UserBelongsToSecureGroup())
				return true;

			var grant = context.ForSystemUser().PrivilegeManager().GetUserPrivileges(user.Name, objectType, objectName, true);
			return (grant & privileges) != 0;
		}

		public static bool UserHasPrivilege(this IQueryContext context, DbObjectType objectType, ObjectName objectName, Privileges privileges) {
			var user = context.User();
			if (user.IsSystem)
				return true;

			if (context.UserBelongsToSecureGroup())
				return true;

			var userName = user.Name;
			var grant = context.ForSystemUser().PrivilegeManager().GetUserPrivileges(userName, objectType, objectName, false);
			return (grant & privileges) != 0;
		}

		public static bool UserCanCreateUsers(this IQueryContext context) {
			return context.UserHasSecureAccess() ||
				context.UserBelongsToGroup(SystemGroups.UserManagerGroup);
		}

		public static bool UserCanDropUser(this IQueryContext context, string userToDrop) {
			return context.UserHasSecureAccess() ||
				   context.UserBelongsToGroup(SystemGroups.UserManagerGroup) ||
				   context.UserName().Equals(userToDrop, StringComparison.OrdinalIgnoreCase);
		}

		public static bool UserCanAlterUser(this IQueryContext context, string userName) {
			if (context.UserName().Equals(userName))
				return true;

			if (userName.Equals(User.PublicName, StringComparison.OrdinalIgnoreCase))
				return false;

			return context.UserHasSecureAccess();
		}

		public static bool UserCanManageUsers(this IQueryContext context) {
			return context.UserHasSecureAccess() || context.UserBelongsToGroup(SystemGroups.UserManagerGroup);
		}

		public static bool UserCanAccessUsers(this IQueryContext context) {
			return context.UserHasSecureAccess() || context.UserBelongsToGroup(SystemGroups.UserManagerGroup);
		}

		public static bool UserHasTablePrivilege(this IQueryContext context, ObjectName tableName, Privileges privileges) {
			return context.UserHasPrivilege(DbObjectType.Table, tableName, privileges);
		}

		public static bool UserHasSchemaPrivilege(this IQueryContext context, string schemaName, Privileges privileges) {
			if (context.UserHasPrivilege(DbObjectType.Schema, new ObjectName(schemaName), privileges))
				return true;

			return context.UserHasSecureAccess();
		}

		public static bool UserCanCreateSchema(this IQueryContext context) {
			return context.UserHasSecureAccess();
		}

		public static bool UserCanCreateInSchema(this IQueryContext context, string schemaName) {
			return context.UserHasSchemaPrivilege(schemaName, Privileges.Create);
		}

		public static bool UserCanCreateTable(this IQueryContext context, ObjectName tableName) {
			var schema = tableName.Parent;
			if (schema == null)
				return context.UserHasSecureAccess();

			return context.UserCanCreateInSchema(schema.FullName);
		}

		public static bool UserCanAlterInSchema(this IQueryContext context, string schemaName) {
			if (context.UserHasSchemaPrivilege(schemaName, Privileges.Alter))
				return true;

			return context.UserHasSecureAccess();
		}

		public static bool UserCanAlterTable(this IQueryContext context, ObjectName tableName) {
			var schema = tableName.Parent;
			if (schema == null)
				return false;

			return context.UserCanAlterInSchema(schema.FullName);
		}

		public static bool UserCanSelectFromTable(this IQueryContext context, ObjectName tableName) {
			return UserCanSelectFromTable(context, tableName, new string[0]);
		}

		public static bool UserCanReferenceTable(this IQueryContext context, ObjectName tableName) {
			return context.UserHasTablePrivilege(tableName, Privileges.References);
		}

		public static bool UserCanSelectFromPlan(this IQueryContext context, IQueryPlanNode queryPlan) {
			var selectedTables = queryPlan.DiscoverTableNames();
			return selectedTables.All(context.UserCanSelectFromTable);
		}

		public static bool UserCanSelectFromTable(this IQueryContext context, ObjectName tableName, params string[] columnNames) {
			// TODO: Column-level select will be implemented in the future
			return context.UserHasTablePrivilege(tableName, Privileges.Select);
		}

		public static bool UserCanUpdateTable(this IQueryContext context, ObjectName tableName, params string[] columnNames) {
			// TODO: Column-level select will be implemented in the future
			return context.UserHasTablePrivilege(tableName, Privileges.Update);
		}

		public static bool UserCanInsertIntoTable(this IQueryContext context, ObjectName tableName, params string[] columnNames) {
			// TODO: Column-level select will be implemented in the future
			return context.UserHasTablePrivilege(tableName, Privileges.Insert);
		}

		public static bool UserCanExecute(this IQueryContext context, RoutineType routineType, Invoke invoke) {
			if (routineType == RoutineType.Function &&
				context.IsSystemFunction(invoke)) {
				return true;
			}

			if (context.UserHasSecureAccess())
				return true;

			return context.UserHasPrivilege(DbObjectType.Routine, invoke.RoutineName, Privileges.Execute);
		}

		public static bool UserCanExecuteFunction(this IQueryContext context, Invoke invoke) {
			return context.UserCanExecute(RoutineType.Function, invoke);
		}

		public static bool UserCanExecuteProcedure(this IQueryContext context, Invoke invoke) {
			return context.UserCanExecute(RoutineType.Procedure, invoke);
		}

		public static bool UserCanCreateObject(this IQueryContext context, DbObjectType objectType, ObjectName objectName) {
			return context.UserHasPrivilege(objectType, objectName, Privileges.Create);
		}

		public static bool UserCanDropObject(this IQueryContext context, DbObjectType objectType, ObjectName objectName) {
			return context.UserHasPrivilege(objectType, objectName, Privileges.Drop);
		}

		public static bool UserCanAlterObject(this IQueryContext context, DbObjectType objectType, ObjectName objectName) {
			return context.UserHasPrivilege(objectType, objectName, Privileges.Alter);
		}

		public static bool UserCanAccessObject(this IQueryContext context, DbObjectType objectType, ObjectName objectName) {
			return context.UserHasPrivilege(objectType, objectName, Privileges.Select);
		}

		public static bool UserCanDeleteFromTable(this IQueryContext context, ObjectName tableName) {
			return context.UserHasTablePrivilege(tableName, Privileges.Delete);
		}

		public static bool UserCanAddToGroup(this IQueryContext context, string groupName) {
			if (context.User().IsSystem)
				return true;

			if (context.UserBelongsToSecureGroup() ||
				context.UserBelongsToGroup(SystemGroups.UserManagerGroup))
				return true;

			return context.ForSystemUser().UserManager().IsUserGroupAdmin(context.UserName(), groupName);
		}

		#endregion
	}
}