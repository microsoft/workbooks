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
    interface IEvaluationService : IDisposable
    {
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