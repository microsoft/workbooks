// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using Newtonsoft.Json;

using Xamarin.Interactive.CodeAnalysis.Events;
using Xamarin.Interactive.CodeAnalysis.Resolving;
using Xamarin.Interactive.Protocol;
using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.CodeAnalysis
{
    [JsonObject]
    public sealed class Evaluation : IXipResponseMessage, ICodeCellEvent
    {
        public Guid RequestId { get; }

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

        public bool IsNullResult => ResultType == null ||
            ResultRepresentations == null ||
            ResultRepresentations.Count == 0;

        [JsonConstructor]
        Evaluation (
            Guid requestId,
            CodeCellId codeCellId,
            EvaluationResultHandling resultHandling,
            IRepresentedType resultType,
            IReadOnlyList<object> resultRepresentations,
            ExceptionNode exception,
            TimeSpan evaluationDuration,
            int cultureLCID,
            int uiCultureLCID,
            bool interrupted,
            bool initializedAgentIntegration,
            IReadOnlyList<AssemblyDefinition> loadedAssemblies)
        {
            RequestId = requestId;
            CodeCellId = codeCellId;
            ResultHandling = resultHandling;
            ResultType = resultType;
            ResultRepresentations = resultRepresentations;
            Exception = exception;
            EvaluationDuration = evaluationDuration;
            CultureLCID = cultureLCID;
            UICultureLCID = uiCultureLCID;
            Interrupted = interrupted;
            InitializedAgentIntegration = initializedAgentIntegration;
            LoadedAssemblies = loadedAssemblies;
        }

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
            RequestId = requestId;
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