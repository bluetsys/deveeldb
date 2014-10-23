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

namespace Deveel.Data.Protocol{
	[Serializable]
	public sealed class LargeObjectCreateRequest : IMessage {
		public LargeObjectCreateRequest(ReferenceType referenceType, long objectLength) {
			if (objectLength <= 0)
				throw new ArgumentException("Invalid object length specified.", "objectLength");

			ObjectLength = objectLength;
			ReferenceType = referenceType;
		}

		public ReferenceType ReferenceType { get; private set; }

		public long ObjectLength { get; private set; }
	}
}