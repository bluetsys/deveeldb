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

namespace Deveel.Data {
	internal class JournalCommand {
		// (params: table_id, row_index)
		internal const byte TABLE_ADD = 1;         // Add a row to a table.
		// (params: table_id, row_index)
		internal const byte TABLE_REMOVE = 2;         // Remove a row from a table.
		internal const byte TABLE_UPDATE_ADD = 5;  // Add a row from an update.
		internal const byte TABLE_UPDATE_REMOVE = 6;  // Remove a row from an update.
	}
}