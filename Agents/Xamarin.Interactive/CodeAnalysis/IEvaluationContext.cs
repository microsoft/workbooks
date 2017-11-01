//
// IEvaluationContext.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.CodeAnalysis
{
	public interface IEvaluationContext
	{
		EvaluationContextId Id { get; }
		IObservable<IEvaluation> Evaluations { get; }
	}
}