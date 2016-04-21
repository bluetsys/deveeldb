﻿// 
//  Copyright 2010-2016 Deveel
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

namespace Deveel.Data.Store.Journaled {
	public interface IJournaledResource : IDisposable {
		long Id { get; }

		int PageSize { get; }

		long Size { get; }

		bool Exists { get; }


		void Read(long pageNumber, byte[] buffer, int offset);

		void Write(long pageNumber, byte[] buffer, int offset, int count);

		void SetSize(long value);

		void Open(bool readOnly);

		void Close();

		void Delete();
	}
}