//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.Interactive.CodeAnalysis
{
    /// <summary>
    /// Do not implement! This is just a temporary measure to allow
    /// WorkbookPageViewModel to coexist with EvaluationService and
    /// plumb through ClientSession.
    /// This goes away when WorkbookPageViewModel goes away.
    /// </summary>
    interface IEvaluationService : IDisposable
    {
        EvaluationContextId Id { get; }
        bool CanEvaluate { get; }
        void OutdateAllCodeCells ();
        IDisposable InhibitEvaluate ();
        Task EvaluateAsync (string input, CancellationToken cancellationToken = default);
        Task EvaluateAllAsync (CancellationToken cancellationToken = default);
        Task LoadWorkbookDependencyAsync (string dependency, CancellationToken cancellationToken = default);
        Task<bool> AddTopLevelReferencesAsync (
            IReadOnlyList<string> references,
            CancellationToken cancellationToken = default);
    }
}