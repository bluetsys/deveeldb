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
using System.Threading.Tasks;

using Deveel.Data.Security;

namespace Deveel.Data.Sql.Statements {
	public sealed class SetAccountStatusAction : IAlterUserAction {
		public SetAccountStatusAction(UserStatus newStatus) {
			NewStatus = newStatus;
		}

		public UserStatus NewStatus { get; }

		async Task<bool> IAlterUserAction.AlterUserAsync(string userName, StatementContext context) {
			var securityManager = context.GetUserManager();

			return await securityManager.SetUserStatusAsync(userName, NewStatus);
		}

		void ISqlFormattable.AppendTo(SqlStringBuilder builder) {
			builder.Append("SET ACCOUNT STATUS ");
			builder.Append(NewStatus.ToString().ToUpperInvariant());
		}
	}
}