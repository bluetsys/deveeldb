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

namespace Deveel.Data.Sql.Tables {
	public interface ITableFieldCache {
		void SetValue(ObjectName tableName, long row, int column, SqlObject value);

		bool TryGetValue(ObjectName tableName, long row, int column, out SqlObject value);

		void Remove(ObjectName tableName, long row, int column);

		void Clear();
	}
}