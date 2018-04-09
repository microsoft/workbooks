// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using Xamarin.Interactive.CodeAnalysis.Events;
using Xamarin.Interactive.CodeAnalysis.Resolving;
using Xamarin.Interactive.Protocol;
using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.CodeAnalysis
{
    [Serializable]
    public sealed class Evaluation : IXipResponseMessage, ICodeCellEvent
    {
        readonly Guid requestId;
        Guid IXipResponseMessage.RequestId => requestId;

        public CodeCellId CodeCellId { get; }
        public EvaluationResultHandling ResultHandling { get; }
        public IRepresentedType ResultType { get; }
        public IReadOnlyList<object> ResultRepresentations { get; }
        internal ExceptionNode Exception { get; }
        public TimeSpan EvaluationDuration { get; }
        public int CultureLCID { get; }
        public int UICultureLCID { get; }
        public bool Interrupted { get; }
        public bool InitializedAgentIntegration { get; }
        public IReadOnlyList<AssemblyDefinition> LoadedAssemblies { get; }

        public Evaluation (
            CodeCellId codeCellId,
            EvaluationResultHandling resultHandling,
            object value)
            : this (
                Guid.Empty,
                codeCellId,
                resultHandling,
                value)
        {
        }

        internal Evaluation (
            Guid requestId,
            CodeCellId codeCellId,
            EvaluationResultHandling resultHandling,
            object value,
            ExceptionNode exception = null,
            TimeSpan evaluationDuration = default,
            int cultureLCID = 0,
            int uiCultureLCID = 0,
            bool interrupted = false,
            bool initializedAgentIntegration = false,
            IReadOnlyList<AssemblyDefinition> loadedAssemblies = null)
        {
            this.requestId = requestId;

            CodeCellId = codeCellId;
            ResultHandling = resultHandling;

            switch (value) {
            case null:
                break;
            case RepresentedObject representedObject:
                ResultType = representedObject.RepresentedType;
                ResultRepresentations = representedObject;
                break;
            default:
                ResultType = RepresentedType.Lookup (value.GetType ());
                ResultRepresentations = new [] { value };
                break;
            }

            Exception = exception;
            EvaluationDuration = evaluationDuration;
            CultureLCID = cultureLCID;
            UICultureLCID = uiCultureLCID;
            Interrupted = interrupted;
            InitializedAgentIntegration = initializedAgentIntegration;
            LoadedAssemblies = loadedAssemblies;
        }
    }
}