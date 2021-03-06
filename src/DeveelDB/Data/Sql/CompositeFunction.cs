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

namespace Deveel.Data.Sql {
	/// <summary>
	/// The kind of composite function in a <see cref="CompositeTable"/>.
	/// </summary>
	public enum CompositeFunction {
		/// <summary>
		/// The composite function for finding the union of the tables.
		/// </summary>
		Union = 1,

		/// <summary>
		/// The composite function for finding the interestion of the tables.
		/// </summary>
		Intersect = 2,

		/// <summary>
		/// The composite function for finding the difference of the tables.
		/// </summary>
		Except = 3,

		/// <summary>
		/// An unspecified composite function.
		/// </summary>
		None = -1
	}
}