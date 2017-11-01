//
// XipErrorMessage.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2014-2015 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Text;

using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Protocol
{
	[Serializable]
	class XipErrorMessage
	{
		public string Message { get; set; }
		public ExceptionNode Exception { get; set; }

		public override string ToString ()
		{
			var builder = new StringBuilder ();

			if (Message != null)
				builder.Append (Message);

			if (Exception != null) {
				if (builder.Length > 0)
					builder.Append (": ");
				builder.Append (Exception);
			}

			return builder.ToString ();
		}

		public void Throw ()
		{
			throw new XipErrorMessageException (this);
		}
	}
}