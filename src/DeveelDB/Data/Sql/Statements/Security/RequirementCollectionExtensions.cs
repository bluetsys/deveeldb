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
using System.Threading.Tasks;

using Deveel.Data.Security;

namespace Deveel.Data.Sql.Statements.Security {
	public static class RequirementCollectionExtensions {
		public static void Append(this IRequirementCollection requirements, IEnumerable<IRequirement> collection) {
			if (collection == null)
				return;

			foreach (var requirement in collection) {
				requirements.Require(requirement);
			}
		}

		public static void Require(this IRequirementCollection requirements, ObjectName objName, Privilege privilege) {
			requirements.Require(new PrivilegesRequirement(objName, privilege));
		}

		public static void RequireSelectPrivilege(this IRequirementCollection requirements, ObjectName tableName)
			=> requirements.Require(tableName, SqlPrivileges.Select);

		public static void RequireUpdatePrivilege(this IRequirementCollection requirements, ObjectName tableName)
			=> requirements.Require(tableName, SqlPrivileges.Update);

		public static void Require(this IRequirementCollection collection, Func<IContext, Task<bool>> requirement)
			=> collection.Require(new DelegatedRequirement(requirement));
	}
}