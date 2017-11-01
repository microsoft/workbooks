//
// IEvaluationContextIntegration.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

namespace Xamarin.Interactive.CodeAnalysis
{
	public interface IEvaluationContextIntegration
	{
		void IntegrateWith (IEvaluationContext evaluationContext);
	}
}