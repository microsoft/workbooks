//
// ReflectionExtensions.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using System.Reflection;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Xamarin.Interactive.Core
{
	static class ReflectionExtensions
	{
		public static bool HasAttribute (this MemberInfo member, string attrName)
		{
			foreach (var attr in member.GetCustomAttributes ()) {
				if (attr.GetType ().Name == attrName)
					return true;
			}

			return false;
		}

		// adapted from Roslyn Scripting: Microsoft.CodeAnalysis.Scripting.Hosting.CommandLineRunner
		public static bool IsTaskAwaiter (this Type type)
		{
			if (type == null)
				return false;

			if (type == typeof(TaskAwaiter) ||
				type == typeof(ConfiguredTaskAwaitable) ||
				type == typeof(ConfiguredTaskAwaitable<>))
				return true;

			var typeInfo = type?.GetTypeInfo ();
			if (typeInfo != null && typeInfo.IsGenericType)
				return typeInfo.GetGenericTypeDefinition () == typeof(TaskAwaiter<>);

			return false;
		}

		public static MethodInfo GetMethod (this Type type, string methodName, BindingFlags bindingFlags,
			Type argumentType, Type returnType)
		{
			return GetMethod (type, methodName, bindingFlags, new [] { argumentType }, returnType);
		}

		public static MethodInfo GetMethod (this Type type, string methodName, BindingFlags bindingFlags,
			Type [] argumentTypes, Type returnType)
		{
			return type.GetMethod (methodName, bindingFlags,
				new MethodBinder (argumentTypes, returnType), argumentTypes, null);
		}

		class MethodBinder : Binder
		{
			readonly Type [] argumentTypes;
			readonly Type returnType;

			public MethodBinder (Type [] argumentTypes, Type returnType)
			{
				if (returnType == null)
					throw new ArgumentNullException (nameof(returnType));

				this.argumentTypes = argumentTypes;
				this.returnType = returnType;
			}

			public override MethodBase SelectMethod (BindingFlags bindingAttr, MethodBase[] match,
				Type[] types, ParameterModifier[] modifiers)
			{
				foreach (MethodInfo method in match) {
					if (method.ReturnType != returnType)
						continue;

					var args = method.GetParameters ();

					// special case 0/null to allow null to be specified
					if ((args == null || args.Length == 0) &&
						(argumentTypes == null || argumentTypes.Length == 0))
						return method;

					if (args.Length == argumentTypes.Length) {
						for (int i = 0; i < args.Length; i++) {
							if (args [i].ParameterType != argumentTypes [i])
								goto nomatch;
						}
					}

					return method;
				nomatch:
					;
				}

				return null;
			}

			public override FieldInfo BindToField (BindingFlags bindingAttr, FieldInfo[] match,
				object value, CultureInfo culture)
			{
				throw new NotImplementedException ();
			}

			public override MethodBase BindToMethod (BindingFlags bindingAttr, MethodBase[] match,
				ref object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] names, out object state)
			{
				throw new NotImplementedException ();
			}

			public override object ChangeType (object value, Type type, CultureInfo culture)
			{
				throw new NotImplementedException ();
			}

			public override void ReorderArgumentArray (ref object[] args, object state)
			{
				throw new NotImplementedException ();
			}

			public override PropertyInfo SelectProperty (BindingFlags bindingAttr, PropertyInfo[] match,
				Type returnType, Type[] indexes, ParameterModifier[] modifiers)
			{
				throw new NotImplementedException ();
			}
		}
	}
}