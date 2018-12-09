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

namespace Deveel.Data.Sql.Constraints {
	/// <summary>
	/// The deferrability of a constraint
	/// </summary>
	/// <seealso cref="ConstraintInfo"/>
	public enum ConstraintDeferrability {
		/// <summary>
		/// The constraint is checked at the <c>COMMIT</c>
		/// of each transaction.
		/// </summary>
		InitiallyDeferred = 4,
		
		/// <summary>
		/// The constraint is checked immediately after
		/// each single statement.
		/// </summary>
		InitiallyImmediate = 5,
		
		/// <summary>
		/// A constraint whose check cannot be deferred to the
		/// <c>COMMIT</c> of a transaction.
		/// </summary>
		NotDeferrable = 6,
	}
}