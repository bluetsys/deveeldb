﻿// 
//  Copyright 2010-2014 Deveel
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
using System.Collections.Generic;
using System.Linq;

namespace Deveel.Data {
	public static class SqlQueryExtensions {
		public static void Add(this ICollection<SqlQueryParameter> parameters, string name, object value) {
			parameters.Add(new SqlQueryParameter(name, value));
		}

		public static void Add(this ICollection<SqlQueryParameter> parameters, object value) {
			parameters.Add(new SqlQueryParameter(value));
		}

		public static SqlQueryParameter GetNamedParameter(this IEnumerable<SqlQueryParameter> parameters, string name) {
			return parameters.FirstOrDefault(x => x.Name == name);
		}
	}
}