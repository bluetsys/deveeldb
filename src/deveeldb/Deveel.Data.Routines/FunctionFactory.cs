// 
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
using System.Collections;
using System.Reflection;

using Deveel.Data.DbSystem;

namespace Deveel.Data.Routines {
	/// <summary>
	/// A factory that generates <see cref="IFunction"/> objects given 
	/// a function name and a set of expression's that represent parameters.
	/// </summary>
	/// <remarks>
	/// A developer may create their own instance of this class and register 
	/// the factory with the <see cref="DatabaseContext"/>. When the SQL grammer 
	/// comes across a function, it will try and resolve the function name against 
	/// the registered function factories.
	/// </remarks>
	public abstract class FunctionFactory : IFunctionLookup {
		private bool initd;
		private static readonly Expression GlobExpression;

		static FunctionFactory() {
			GlobExpression = new Expression();
			GlobExpression.AddElement(TObject.CreateString("*"));
			GlobExpression.Text.Append("*");

			GlobList = new Expression[] { GlobExpression };
		}

		///<summary>
		/// Represents a function argument * for glob's such as <c>count(*)</c>
		///</summary>
		public static readonly Expression[] GlobList;

		/// <summary>
		/// The mapping of 'fun_name' to 'fun_type' for each function that's 
		/// registered with this factory.
		/// </summary>
		private readonly Hashtable fun_class_mapping;

		/// <summary>
		/// Constructor arguments types for the function.
		/// </summary>
		private readonly Type[] construct_proto;

		protected FunctionFactory() {
			fun_class_mapping = new Hashtable(CaseInsensitiveHashCodeProvider.DefaultInvariant, CaseInsensitiveComparer.DefaultInvariant);
			// The is the prototype for the constructor when creating a new function.
			construct_proto = new Type[1];
			Object exp_arr_ob =
				Array.CreateInstance(typeof(Expression), 0);
			construct_proto[0] = exp_arr_ob.GetType();
		}

		/// <summary>
		/// Adds a new function to this factory.
		/// </summary>
		/// <param name="fun_name">The name of the function (eg. 'sum', 'concat').</param>
		/// <param name="fun_class">The <see cref="IFunction"/> type that we instantiate 
		/// for this function.</param>
		/// <param name="fun_type">That type of function.</param>
		/// <remarks>
		/// Takes a function name and a class that is the <see cref="IFunction"/>
		/// implementation. When the <see cref="GenerateFunction"/> method is called, 
		/// it looks up the class with the function name and returns a new instance 
		/// of the function.
		/// </remarks>
		protected void AddFunction(String fun_name, Type fun_class, FunctionType fun_type) {
			try {
				String lf_name = fun_name.ToLower();
				if (fun_class_mapping[lf_name] == null) {
					FF_FunctionInfo ff_info = new FF_FunctionInfo(this, fun_name, fun_type,
					                                              fun_class.GetConstructor(construct_proto));
					fun_class_mapping[lf_name] = ff_info;
				} else {
					throw new ApplicationException("Function '" + fun_name +
					                               "' already defined in factory.");
				}
			} catch (Exception e) {
				throw new Exception(e.Message);
			}
		}

		protected void AddFunction(String fun_name, Type fun_class) {
			AddFunction(fun_name, fun_class, FunctionType.Static);
		}

		/// <summary>
		/// Removes a static function from this factory.
		/// </summary>
		/// <param name="fun_name"></param>
		protected void RemoveFunction(String fun_name) {
			String lf_name = fun_name.ToLower();
			if (fun_class_mapping[lf_name] != null) {
				fun_class_mapping.Remove(fun_name.ToLower());
			} else {
				throw new ApplicationException("Function '" + lf_name +
				                               "' is not defined in this factory.");
			}
		}

		/// <summary>
		/// Returns true if the function name is defined in this factory.
		/// </summary>
		/// <param name="fun_name"></param>
		/// <returns></returns>
		protected bool IsFunctionDefined(String fun_name) {
			String lf_name = fun_name.ToLower();
			return fun_class_mapping[lf_name] != null;
		}

		/// <summary>
		/// Initializes this <see cref="FunctionFactory"/>.
		/// </summary>
		/// <remarks>
		/// This is an abstract method that needs to be implemented. (It doesn't 
		/// need to do anything if a developer implements their own version 
		/// of <see cref="GenerateFunction"/>).
		/// </remarks>
		public void Init() {
			if (!initd) {
				OnInit();
				initd = true;
			}
		}

		protected abstract void OnInit();

		/// <summary>
		/// Creates a <see cref="IFunction"/> object for the function 
		/// with the given name with the given arguments.
		/// </summary>
		/// <param name="functionDef"></param>
		/// <remarks>
		/// If this factory does not handle a function with the given name then it returns null.
		/// </remarks>
		/// <returns></returns>
		public IFunction GenerateFunction(FunctionDef functionDef) {
			String func_name = functionDef.Name;
			Expression[] parameterss = functionDef.Parameters;

			// This will lookup the function name (case insensitive) and if a
			// function class was registered, instantiates and returns it.

			FF_FunctionInfo ff_info = (FF_FunctionInfo)fun_class_mapping[func_name.ToLower()];
			if (ff_info == null)
				// Function not handled by this factory so return null.
				return null;

			ConstructorInfo fun_constructor = ff_info.Constructor;
			object[] args = new Object[] {parameterss};
			try {
				return (IFunction) fun_constructor.Invoke(args);
			} catch (TargetInvocationException e) {
				throw new Exception(e.InnerException.Message);
			} catch (Exception e) {
				throw new Exception(e.Message);
			}
		}

		/// <summary>
		/// Checks if the given function is aggregate.
		/// </summary>
		/// <param name="functionDef"></param>
		/// <returns>
		/// Returns true if the function defined by <see cref="FunctionDef"/> is 
		/// an aggregate function, or false otherwise.
		/// </returns>
		public bool IsAggregate(FunctionDef functionDef) {
			IFunctionInfo f_info = GetFunctionInfo(functionDef.Name);
			if (f_info == null)
				// Function not handled by this factory so return false.
				return false;
			return (f_info.Type == FunctionType.Aggregate);
		}

		///<summary>
		/// Returns a <see cref="IFunctionInfo"/> instance of the function with the given 
		/// name that this <see cref="FunctionFactory"/> manages.
		///</summary>
		///<param name="fun_name"></param>
		/// <remarks>
		/// If <see cref="GenerateFunction"/> is reimplemented then this method should be 
		/// rewritten also.
		/// </remarks>
		///<returns></returns>
		public IFunctionInfo GetFunctionInfo(String fun_name) {
			FF_FunctionInfo ff_info = (FF_FunctionInfo)fun_class_mapping[fun_name.ToLower()];
			return ff_info;
		}

		///<summary>
		/// Returns the list of all function names that this FunctionFactory manages.
		///</summary>
		/// <remarks>
		/// This is used to compile information about the function factories.  If
		/// <see cref="GenerateFunction"/> is reimplemented then this method should 
		/// be rewritten also.
		/// </remarks>
		///<returns></returns>
		public IFunctionInfo[] GetAllFunctionInfo() {
			ICollection keys = fun_class_mapping.Keys;
			int list_size = keys.Count;
			IFunctionInfo[] list = new IFunctionInfo[list_size];
			IEnumerator i = keys.GetEnumerator();
			int n = 0;
			while (i.MoveNext()) {
				String fun_name = (String)i.Current;
				list[n] = GetFunctionInfo(fun_name);
				++n;
			}
			return list;
		}

		/// <summary>
		/// An implementation of IFunctionInfo.
		/// </summary>
		protected class FF_FunctionInfo : IFunctionInfo {
			private readonly FunctionFactory factory;
			private readonly String name;
			private readonly FunctionType type;
			private readonly ConstructorInfo constructor;

			public FF_FunctionInfo(FunctionFactory factory, String name, FunctionType type, ConstructorInfo constructor) {
				this.factory = factory;
				this.name = name;
				this.type = type;
				this.constructor = constructor;
			}

			public string Name {
				get { return name; }
			}

			public FunctionType Type {
				get { return type; }
			}

			public ConstructorInfo Constructor {
				get { return constructor; }
			}

			public string FunctionFactoryName {
				get { return factory.GetType().ToString(); }
			}
		}
	}
}