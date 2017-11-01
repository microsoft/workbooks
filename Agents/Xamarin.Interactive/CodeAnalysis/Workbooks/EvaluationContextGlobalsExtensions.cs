//
// EvaluationContextGlobalsExtensions.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.CodeAnalysis.Workbooks
{
	public static class EvaluationContextGlobalsExtensions
	{
		public static VerbatimHtml AsHtml (this string str)
			=> new VerbatimHtml (str);
	}
}