//
// CryptoHelpers.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System.Text;

namespace Xamarin.Interactive
{
	static class CryptoHelpers
	{
		public static string ToHexString (this byte [] bytes)
		{
			var sb = new StringBuilder (bytes.Length * 2);
			for (var i = 0; i < bytes.Length; i++)
				sb.AppendFormat ("{0:x2}", bytes [i]);
			return sb.ToString ();
		}
	}
}