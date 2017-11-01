//
// InteractiveSourceReferenceResolver.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2016-2017 Microsoft. All rights reserved.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Compilation.Roslyn
{
	sealed class InteractiveSourceReferenceResolver : SourceReferenceResolver
	{
		readonly InteractiveDependencyResolver dependencyResolver;

		FilePath baseDirectory;
		SourceFileResolver sourceFileResolver;

		public InteractiveSourceReferenceResolver (InteractiveDependencyResolver dependencyResolver)
		{
			if (dependencyResolver == null)
				throw new ArgumentNullException (nameof (dependencyResolver));

			this.dependencyResolver = dependencyResolver;
		}

		void EnsureBaseDirectory ()
		{
			var currentBaseDirectory = dependencyResolver.BaseDirectory;
			if (currentBaseDirectory != baseDirectory || sourceFileResolver == null) {
				baseDirectory = currentBaseDirectory;
				sourceFileResolver = new SourceFileResolver (
					ImmutableArray<string>.Empty,
					baseDirectory);
			}
		}

		public override string NormalizePath (string path, string baseFilePath)
		{
			EnsureBaseDirectory ();
			return sourceFileResolver.NormalizePath (path, baseFilePath);
		}

		public override Stream OpenRead (string resolvedPath)
		{
			EnsureBaseDirectory ();
			return sourceFileResolver.OpenRead (resolvedPath);
		}

		public override string ResolveReference (string path, string baseFilePath)
		{
			EnsureBaseDirectory ();
			return sourceFileResolver.ResolveReference (path, baseFilePath);
		}

		public override bool Equals (object other) => ((object)this).Equals (other);
		public override int GetHashCode () => ((object)this).GetHashCode ();

		public override SourceText ReadText (string resolvedPath)
			=> new ScriptSourceTextContainer (resolvedPath).CurrentText;

		/// <summary>
		/// Stubbed SourceTextContainer that can be extended to monitor files for
		/// changes in the future when https://github.com/dotnet/roslyn/issues/21964
		/// is fixed.
		/// </summary>
		sealed class ScriptSourceTextContainer : SourceTextContainer
		{
			readonly string resolvedPath;
			ScriptSourceText currentText;

			public override SourceText CurrentText => currentText;

			public override event EventHandler<TextChangeEventArgs> TextChanged;

			public ScriptSourceTextContainer (string resolvedPath)
			{
				this.resolvedPath = resolvedPath
					?? throw new ArgumentNullException (nameof (resolvedPath));

				currentText = new ScriptSourceText (this, resolvedPath);
			}

			public void Reload ()
			{
				var oldText = currentText;
				currentText = new ScriptSourceText (this, resolvedPath);
				TextChanged?.Invoke (
					this,
					new TextChangeEventArgs (
						oldText,
						currentText,
						currentText.GetChangeRanges (oldText)));
			}

			sealed class ScriptSourceText : SourceText
			{
				readonly StringBuilder buffer = new StringBuilder ();
				readonly SourceTextContainer container;

				public override SourceTextContainer Container => container;
				public override char this [int position] => buffer [position];
				public override Encoding Encoding => Utf8.Encoding;
				public override int Length => buffer.Length;

				public ScriptSourceText (SourceTextContainer container, string resolvedPath)
				{
					this.container = container;
					buffer.Append (File.ReadAllText (resolvedPath));
				}

				public override void CopyTo (
					int sourceIndex,
					char [] destination,
					int destinationIndex,
					int count)
					=> buffer.CopyTo (sourceIndex, destination, destinationIndex, count);
			}
		}
	}
}