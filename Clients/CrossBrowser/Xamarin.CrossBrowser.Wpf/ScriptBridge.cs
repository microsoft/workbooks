//
// ScriptBridge.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;
using System.IO;
using System.Reflection;

namespace Xamarin.CrossBrowser
{
	sealed class ScriptBridge
	{
		public static readonly string BackingJS = new StreamReader (
			typeof (ScriptBridge)
			.Assembly
			.GetManifestResourceStream ("Xamarin.CrossBrowser.ScriptBridge.js")
		).ReadToEnd ();

		readonly ScriptContext context;
		readonly object comInstance;
		readonly Type comType;

		public ScriptBridge (ScriptContext context, object comInstance)
		{
			this.context = context;
			this.comInstance = comInstance;
			this.comType = comInstance.GetType ();
		}

		object ComInvoke (string methodName, params object [] args)
			=> comType.InvokeMember (methodName, BindingFlags.InvokeMethod, null, comInstance, args);

		object Invoke (string methodName, params object [] args)
			=> context.FromComObject (ComInvoke (methodName, context.ToComObjectArray (args)));

		public object CreateObject () => Invoke ("createObject");

		public object CreateArray () => Invoke ("createArray");

		public object CreateFunction (int functionId)
			=> ComInvoke ("createFunction", functionId);

		public object GetProperty (object target, string propertyName)
			=> Invoke ("getProperty", target, propertyName);

		public object GetPropertyNames (object target)
			=> Invoke ("getPropertyNames", target);

		public bool HasProperty (object target, string propertyName)
			=> (bool)Invoke ("hasProperty", target, propertyName);

		public void SetProperty (object target, string propertyName, object value)
			=> Invoke ("setProperty", target, propertyName, value);

		public object ApplyFunction (object target, object [] arguments)
		{
			if (arguments == null || arguments.Length == 0)
				return Invoke ("applyFunction", target);

			var applyArguments = new object [arguments.Length + 1];
			applyArguments [0] = target;
			Array.Copy (arguments, 0, applyArguments, 1, arguments.Length);

			return Invoke ("applyFunction", applyArguments);
		}
	}
}