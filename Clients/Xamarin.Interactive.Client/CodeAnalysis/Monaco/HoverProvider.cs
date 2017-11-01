//
// HoverProvider.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using Xamarin.CrossBrowser;

using Xamarin.Interactive.Compilation.Roslyn;

namespace Xamarin.Interactive.CodeAnalysis.Monaco
{
	sealed class HoverProvider : IDisposable
	{
		const string TAG = nameof (HoverProvider);

		readonly RoslynCompilationWorkspace compilationWorkspace;
		readonly ScriptContext context;
		readonly Func<string, SourceText> getSourceTextByModelId;

		#pragma warning disable 0414
		readonly dynamic providerTicket;
		#pragma warning restore 0414

		public HoverProvider (
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

			providerTicket = context.GlobalObject.xiexports.monaco.RegisterWorkbookHoverProvider (
				"csharp",
				(ScriptFunc)ProvideHover);
		}

		public void Dispose ()
			=> providerTicket.dispose ();

		dynamic ProvideHover (dynamic self, dynamic args)
			=> ProvideHover (
				args [0].id.ToString (),
				MonacoExtensions.FromMonacoPosition (args [1]),
				MonacoExtensions.FromMonacoCancellationToken (args [2]));

		object ProvideHover (
			string modelId,
			LinePosition linePosition,
			CancellationToken cancellationToken)
		{
			var sourceTextContent = getSourceTextByModelId (modelId);

			var computeTask = ComputeHoverAsync (
				sourceTextContent.Lines.GetPosition (linePosition),
				sourceTextContent,
				cancellationToken);

			return context.ToMonacoPromise (
				computeTask,
				ToMonacoHover,
				MainThread.TaskScheduler,
				raiseErrors: false);
		}

		async Task<Hover> ComputeHoverAsync (
			int position,
			SourceText sourceText,
			CancellationToken cancellationToken)
		{
			var hover = new Hover ();
			if (position <= 0)
				return hover;

			var document = compilationWorkspace.GetSubmissionDocument (sourceText.Container);
			var root = await document.GetSyntaxRootAsync (cancellationToken);
			var syntaxToken = root.FindToken (position);

			var expression = syntaxToken.Parent as ExpressionSyntax;
			if (expression == null)
				return hover;

			var semanticModel = await document.GetSemanticModelAsync (cancellationToken);
			var symbolInfo = semanticModel.GetSymbolInfo (expression);
			if (symbolInfo.Symbol == null)
				return hover;

			hover.Contents = new [] { symbolInfo.Symbol.ToDisplayString () };
			hover.Range = syntaxToken.GetLocation ().GetLineSpan ().Span;

			return hover;
		}

		static dynamic ToMonacoHover (ScriptContext context, Hover hover)
		{
			if (!(hover.Contents?.Length > 0))
				return null;

			dynamic contents = context.CreateArray ();

			foreach (var content in hover.Contents)
				contents.push (content);

			return context.CreateObject (o => {
				o.contents = contents;
				o.range = context?.ToMonacoRange (hover.Range);
			});
		}

		struct Hover
		{
			public string [] Contents { get; set; }
			public LinePositionSpan Range { get; set; }
		}
	}
}