//
// AgentIntegration.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.IO;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;

using Xamarin.Interactive;
using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.Logging;

[assembly: AgentIntegration (typeof (CompilationIntegration.AgentIntegration))]

namespace CompilationIntegration
{
	public class AgentIntegration : IAgentIntegration, IEvaluationContextIntegration, IObserver<IEvaluation>
	{
		static int id = 0;
		readonly string TAG = nameof (CompilationIntegration) + "." + nameof (AgentIntegration) + "." + id++;

		public void IntegrateWith (IAgent agent)
			=> Log.Info (TAG, $"Integrated with agent {agent}");

		public void IntegrateWith (IEvaluationContext evaluationContext)
		{
			Log.Info (TAG, $"Integrated with evaluation context {evaluationContext.Id}");

			evaluationContext.Evaluations.Subscribe (this);
		}

		void IObserver<IEvaluation>.OnCompleted () { }
		void IObserver<IEvaluation>.OnError (Exception error) { }

		void IObserver<IEvaluation>.OnNext (IEvaluation evaluation)
		{
			// Not all cells may have a compilation since not all cells produce code.
			// Cells that contain _only_ #r directives for example will produce an
			// evaluation without a compilation in order to load the referenced
			// assembly into to evaluation context to be available for subsequent
			// cell evaluations.
			if (evaluation.Compilation == null)
				return;

			// Immediately after the cell has been evaluated, but before any
			// representations have been produced. We're still under the Console
			// capturing as well here (so we can write to Console.Out and have it
			// render in the workbook).
			if (evaluation.Phase != EvaluationPhase.Evaluated)
				return;

			var moduleDefinition = ModuleDefinition.ReadModule (
				evaluation.Compilation.Assembly.Content.OpenPEImage (),
				new ReaderParameters {
					AssemblyResolver = new AppDomainAssemblyResolver ()
				});

			var entryPoint = evaluation.Compilation.Assembly.EntryPoint;

			var cellMethod = moduleDefinition
				?.Types
				?.FirstOrDefault (type => type.FullName == entryPoint.TypeName)
				?.NestedTypes
				?.FirstOrDefault (type => type.Name.IndexOf (
				         "<<Initialize>>",
				         StringComparison.Ordinal) >= 0)
				?.Methods
				?.FirstOrDefault (m => m.Name == "MoveNext" && m.HasBody);

			if (cellMethod == null)
				return;

			// write to a buffer which we'll flush to stdout once disassembly is complete
			// since each write directly to stdout results in pushing data from the agent
			// to the client, and is slow. Ultimately the disassembly should be stored
			// and then retrieved later via an integration with RepresentationManager.
			var writer = new StringWriter ();

			var inCellBody = false;
			foreach (var op in cellMethod.Body.Instructions) {
				// this is a huge hack but essentially we just want to show the instructions
				// that actually make up the cell contents - instructions that [seem] to
				// be between stloc.0 and stloc.1. yay.
				if (!inCellBody && op.OpCode == OpCodes.Stloc_0)
					inCellBody = true;
				else if (inCellBody) {
					if (op.OpCode == OpCodes.Stloc_1)
						break;
					writer.WriteLine ($"IL_{op.Offset:x4} {op.OpCode} {op.Operand}");
				}
			}

			Console.WriteLine (writer);
		}

		sealed class AppDomainAssemblyResolver : IAssemblyResolver
		{
			public void Dispose ()
			{
			}

			public AssemblyDefinition Resolve (AssemblyNameReference name)
				=> Resolve (name, new ReaderParameters { AssemblyResolver = this });

			public AssemblyDefinition Resolve (AssemblyNameReference name, ReaderParameters parameters)
			{
				foreach (var asm in AppDomain.CurrentDomain.GetAssemblies ()) {
					if (asm.GetName ().Name == name.Name &&
						asm.Location != null &&
						File.Exists (asm.Location))
						return AssemblyDefinition.ReadAssembly (
							asm.Location,
							parameters);
				}

				return null;
			}
		}
	}
}