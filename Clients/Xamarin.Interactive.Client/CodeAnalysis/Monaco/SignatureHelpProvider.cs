//
// SignatureHelpProvider.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using Xamarin.CrossBrowser;

using Xamarin.Interactive.Compilation.Roslyn;

namespace Xamarin.Interactive.CodeAnalysis.Monaco
{
	sealed class SignatureHelpProvider : IDisposable
	{
		const string TAG = nameof (SignatureHelpProvider);

		readonly RoslynCompilationWorkspace compilationWorkspace;
		readonly ScriptContext context;
		readonly Func<string, SourceText> getSourceTextByModelId;

		#pragma warning disable 0414
		readonly dynamic providerTicket;
		#pragma warning restore 0414

		public SignatureHelpProvider (
			RoslynCompilationWorkspace compilationWorkspace,
			ScriptContext context,
			Func<string, SourceText> getSourceTextByModelId)
		{
			this.compilationWorkspace = compilationWorkspace
				?? throw new ArgumentNullException (nameof (compilationWorkspace));

			this.context = context
				?? throw new ArgumentNullException (nameof (context));
			
			this.getSourceTextByModelId = getSourceTextByModelId
				?? throw new ArgumentNullException (nameof (getSourceTextByModelId));

			providerTicket = context.GlobalObject.xiexports.monaco.RegisterWorkbookSignatureHelpProvider (
				"csharp",
				(ScriptFunc)ProvideSignatureHelp);
		}

		public void Dispose ()
			=> providerTicket.dispose ();

		dynamic ProvideSignatureHelp (dynamic self, dynamic args)
			=> ProvideSignatureHelp (
				args [0].id.ToString (),
				MonacoExtensions.FromMonacoPosition (args [1]),
				MonacoExtensions.FromMonacoCancellationToken (args [2]));

		object ProvideSignatureHelp (
			string modelId,
			LinePosition linePosition,
			CancellationToken cancellationToken)
		{
			var sourceTextContent = getSourceTextByModelId (modelId);

			var computeTask = ComputeSignatureHelpAsync (
				sourceTextContent.Lines.GetPosition (linePosition),
				sourceTextContent,
				cancellationToken);

			return context.ToMonacoPromise (
				computeTask,
				ToMonacoSignatureHelp,
				MainThread.TaskScheduler,
				raiseErrors: false);
		}

		async Task<SignatureHelp> ComputeSignatureHelpAsync (
			int position,
			SourceText sourceText,
			CancellationToken cancellationToken)
		{
			var signatureHelp = new SignatureHelp ();
			if (position <= 0)
				return signatureHelp;

			var document = compilationWorkspace.GetSubmissionDocument (sourceText.Container);
			var root = await document.GetSyntaxRootAsync (cancellationToken);
			var syntaxToken = root.FindToken (position);

			var semanticModel = await document.GetSemanticModelAsync (cancellationToken);

			var currentNode = syntaxToken.Parent;
			do {
				var creationExpression = currentNode as ObjectCreationExpressionSyntax;
				if (creationExpression != null)
					return CreateMethodGroupSignatureHelp (
						creationExpression,
						creationExpression.ArgumentList,
						position,
						semanticModel);

				var invocationExpression = currentNode as InvocationExpressionSyntax;
				if (invocationExpression != null)
					return CreateMethodGroupSignatureHelp (
						invocationExpression.Expression,
						invocationExpression.ArgumentList,
						position,
						semanticModel);

				currentNode = currentNode.Parent;
			} while (currentNode != null);

			return signatureHelp;
		}

		static SignatureHelp CreateMethodGroupSignatureHelp (
			ExpressionSyntax expression,
			ArgumentListSyntax argumentList,
			int position,
			SemanticModel semanticModel)
		{
			var signatureHelp = new SignatureHelp ();

			// Happens for object initializers with no preceding parens, as soon as user types comma
			if (argumentList == null)
				return signatureHelp;

			int currentArg;
			if (TryGetCurrentArgumentIndex (argumentList, position, out currentArg))
				signatureHelp.ActiveParameter = currentArg;

			var symbolInfo = semanticModel.GetSymbolInfo (expression);
			var bestGuessMethod = symbolInfo.Symbol as IMethodSymbol;

			var methods = semanticModel
				.GetMemberGroup (expression)
				.OfType<IMethodSymbol> ()
				.ToArray ();

			var signatures = new List<SignatureInformation> ();

			for (var i = 0; i < methods.Length; i++) {
				if (methods [i] == bestGuessMethod)
					signatureHelp.ActiveSignature = i;

				var signatureInfo = new SignatureInformation (methods [i]);

				signatures.Add (signatureInfo);
			}

			signatureHelp.Signatures = signatures.ToArray ();

			return signatureHelp;
		}

		// Pared down from Roslyn's internal CommonSignatureHelpUtilities (which covers more types of argument
		// lists than this version)
		static bool TryGetCurrentArgumentIndex (
			ArgumentListSyntax argumentList,
			int position,
			out int index)
		{
			index = 0;
			if (position < argumentList.OpenParenToken.Span.End)
				return false;

			var closeToken = argumentList.CloseParenToken;
			if (!closeToken.IsMissing && position > closeToken.SpanStart)
				return false;

			foreach (var element in argumentList.Arguments.GetWithSeparators ())
				if (element.IsToken && position >= element.Span.End)
					index++;

			return true;
		}

		static dynamic ToMonacoSignatureHelp (ScriptContext context, SignatureHelp signatureHelp)
		{
			if (!(signatureHelp.Signatures?.Length > 0))
			    return null;

			dynamic signatures = context.CreateArray ();

			foreach (var sig in signatureHelp.Signatures) {
				dynamic parameters = context.CreateArray ();

				if (sig.Parameters?.Length > 0)
					foreach (var param in sig.Parameters)
						parameters.push (context.CreateObject (o => {
							o.label = param.Label;
							o.documentation = param.Documentation;
						}));

				signatures.push (context.CreateObject (o => {
					o.label = sig.Label;
					o.documentation = sig.Documentation;
					o.parameters = parameters;
				}));
			}

			return context.CreateObject (o => {
				o.signatures = signatures;
				o.activeSignature = signatureHelp.ActiveSignature;
				o.activeParameter = signatureHelp.ActiveParameter;
			});
		}

		struct ParameterInformation
		{
			public string Label { get; }
			public string Documentation { get; } // Optional; unused for now

			public ParameterInformation (IParameterSymbol parameter, string documentation = null)
			{
				Documentation = documentation;

				Label = parameter.ToMonacoSignatureString ();
			}
		}

		struct SignatureInformation
		{
			public string Label { get; }
			public string Documentation { get; } // Optional; unused for now
			public ParameterInformation [] Parameters { get; }

			public SignatureInformation (IMethodSymbol method, string documentation = null)
			{
				Documentation = documentation;

				Parameters = method
					.Parameters
					.Select (p => new ParameterInformation (p))
					.ToArray ();

				Label = method.ToMonacoSignatureString ();
			}
		}

		struct SignatureHelp
		{
			public SignatureInformation [] Signatures { get; set; }
			public int ActiveSignature { get; set; }
			public int ActiveParameter { get; set; }
		}
	}
}