// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Core;

[assembly: Xamarin.Interactive.CodeAnalysis.WorkspaceService (
    "testlang",
    typeof (Xamarin.Interactive.CodeAnalysis.TestWorkspaceService.Activator))]

namespace Xamarin.Interactive.CodeAnalysis
{
    /// <summary>
    /// Intended as a base class for unit testing.
    /// </summary>
    public class TestWorkspaceService : WorkspaceServiceBase
    {
        public sealed class Activator : IWorkspaceServiceActivator
        {
            public Task<IWorkspaceService> CreateNew (
                LanguageDescription languageDescription,
                WorkspaceConfiguration configuration,
                CancellationToken cancellationToken)
                => Task.FromResult<IWorkspaceService> (new TestWorkspaceService (configuration));
        }

        readonly List<FilePath> packageAssemblyReferences = new List<FilePath> ();
        public IReadOnlyList<FilePath> PackageAssemblyReferences => packageAssemblyReferences;

        public TestWorkspaceService (WorkspaceConfiguration configuration = null)
            : base (configuration)
        {
        }

        static readonly Regex nugetRef = new Regex (@"#r\s+\""nugetref:([^\""]+)\""");

        protected override void OnCellUpdated (CellData cellData)
        {
            if (GetTopologicallySortedCellIds ().First () == cellData.Id) {
                packageAssemblyReferences.Clear ();

                foreach (var line in cellData.Buffer?.Split ('\n') ?? Array.Empty<string> ()) {
                    var assembly = nugetRef.Match (line);
                    if (assembly.Success)
                        packageAssemblyReferences.Add (assembly.Groups [1].Value);
                }
            }
        }
    }
}