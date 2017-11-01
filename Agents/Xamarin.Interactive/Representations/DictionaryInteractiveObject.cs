//
// DictionaryInteractiveObject.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;

using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Representations
{
	[Serializable]
	sealed class DictionaryInteractiveObject : InteractiveObject
	{
		[NonSerialized] readonly List<Tuple<RepresentedMemberInfo, object>> values
			= new List<Tuple<RepresentedMemberInfo, object>> ();

		public string Title { get; }

		public DictionaryInteractiveObject (int depth, InteractiveItemPreparer itemPreparer,
			string title = null)
			: base (depth, itemPreparer)
		{
			Title = title;
		}

		public override void Initialize ()
		{
			base.Initialize ();
			Interact (false, null);
		}

		public void Add (MemberInfo memberInfo, object value, bool wrapAsMemberValueError = false)
		{
			if (wrapAsMemberValueError && value is Exception) {
				var e = (Exception)value;
				if (value is TargetInvocationException)
					value = new GetMemberValueError (e.InnerException);
				else
					value = new GetMemberValueError (e);
			}

			values.Add (Tuple.Create (new RepresentedMemberInfo (memberInfo), value));
		}

		protected override void Prepare ()
		{
			HasMembers = values.Count > 0;
		}

		protected override void ReadMembers ()
		{
			Members = new RepresentedMemberInfo [values.Count];
			Values = new object [values.Count];

			for (int i = 0; i < values.Count; i++) {
				var entry = values [i];

				var value = new RepresentedObject (entry.Item2?.GetType ());
				ItemPreparer (value, Depth + 1, entry.Item2);
				if (value.Count == 0)
					value = null;

				Members [i] = entry.Item1;
				Values [i] = value;
			}
		}
	}
}