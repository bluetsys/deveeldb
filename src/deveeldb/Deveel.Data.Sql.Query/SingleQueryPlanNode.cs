﻿// 
//  Copyright 2010-2015 Deveel
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
using System.Runtime.Serialization;

using Deveel.Data.Sql.Tables;

namespace Deveel.Data.Sql.Query {
	/// <summary>
	/// A <see cref="IQueryPlanNode"/> with a single child.
	/// </summary>
	abstract class SingleQueryPlanNode : IQueryPlanNode {
		protected SingleQueryPlanNode(IQueryPlanNode child) {
			Child = child;
		}

		protected SingleQueryPlanNode(SerializationInfo info, StreamingContext context) {
			Child = (IQueryPlanNode)info.GetValue("Child", typeof(IQueryPlanNode));
		}

		/// <summary>
		/// Gets the single child node of the plan.
		/// </summary>
		public IQueryPlanNode Child { get; private set; }

		public abstract ITable Evaluate(IRequest context);

		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {
			info.AddValue("Child", Child, typeof(IQueryPlanNode));
			GetData(info, context);
		}
		protected virtual void GetData(SerializationInfo info, StreamingContext context) {
		}
	}
}