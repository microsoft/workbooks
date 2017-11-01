//
// Compilation.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.CodeAnalysis
{
	[Serializable]
	sealed class Compilation : MainThreadRequest<Evaluation>, ICompilation
	{
		IAssemblyDefinition ICompilation.Assembly => ExecutableAssembly;

		public CodeCellId CodeCellId { get; }
		public int SubmissionNumber { get; }
		public int EvaluationSessionId { get; }
		public bool IsResultAnExpression { get; }
		public EvaluationEnvironment EvaluationEnvironment { get; }
		public AssemblyDefinition ExecutableAssembly { get; }
		public AssemblyDefinition [] References { get; }

		public Compilation (
			CodeCellId codeCellId,
			int submissionNumber,
			int evaluationSessionId,
			EvaluationEnvironment evaluationEnvironment,
			bool isResultAnExpression,
			AssemblyDefinition executableAssembly,
			AssemblyDefinition [] references)
		{
			CodeCellId = codeCellId;
			SubmissionNumber = submissionNumber;
			EvaluationSessionId = evaluationSessionId;
			EvaluationEnvironment = evaluationEnvironment;
			IsResultAnExpression = isResultAnExpression;
			ExecutableAssembly = executableAssembly;
			References = references;
		}

		protected override bool CanReturnNull => true;

		protected override async Task<Evaluation> HandleAsync (Agent agent)
		{
			agent.ChangeDirectory (EvaluationEnvironment.WorkingDirectory);
			await agent.GetEvaluationContext (EvaluationSessionId).RunAsync (this);
			return null;
		}
	}
}