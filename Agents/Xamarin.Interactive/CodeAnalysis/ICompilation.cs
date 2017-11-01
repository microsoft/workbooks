//
// ICompilation.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

namespace Xamarin.Interactive.CodeAnalysis
{
    /// <summary>
	/// Represents the compilation state of a cell.
	/// </summary>
    public interface ICompilation
    {
        /// <summary>
		/// An identifier that is unique to the cell being evaluated. This should be
		/// passed to <see cref="IAgent.PublishEvaluation"/>.
		/// </summary>
        CodeCellId CodeCellId { get; }

        /// <summary>
		/// Information about the assembly produced by compiling the associated cell.
		/// </summary>
        IAssemblyDefinition Assembly { get; }
    }
}