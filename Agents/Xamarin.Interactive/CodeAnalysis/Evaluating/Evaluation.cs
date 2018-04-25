// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using Newtonsoft.Json;

using Xamarin.Interactive.CodeAnalysis.Events;
using Xamarin.Interactive.CodeAnalysis.Resolving;
using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.CodeAnalysis.Evaluating
{
    [JsonObject]
    public sealed class Evaluation : ICodeCellEvent
    {
        public CodeCellId CodeCellId { get; }
        public EvaluationResultHandling ResultHandling { get; }
        public IRepresentedType ResultType { get; }
        public IReadOnlyList<object> ResultRepresentations { get; }
        internal ExceptionNode Exception { get; }
        public TimeSpan EvaluationDuration { get; }
        public int CultureLCID { get; }
        public int UICultureLCID { get; }
        public bool Interrupted { get; }
        public bool InitializedIntegration { get; }
        public IReadOnlyList<AssemblyDefinition> LoadedAssemblies { get; }

        public bool IsNullResult => ResultType == null ||
            ResultRepresentations == null ||
            ResultRepresentations.Count == 0;

        [JsonConstructor]
        Evaluation (
            CodeCellId codeCellId,
            EvaluationResultHandling resultHandling,
            IRepresentedType resultType,
            IReadOnlyList<object> resultRepresentations,
            ExceptionNode exception,
            TimeSpan evaluationDuration,
            int cultureLCID,
            int uiCultureLCID,
            bool interrupted,
            bool initializedIntegration,
            IReadOnlyList<AssemblyDefinition> loadedAssemblies)
        {
            CodeCellId = codeCellId;
            ResultHandling = resultHandling;
            ResultType = resultType;
            ResultRepresentations = resultRepresentations;
            Exception = exception;
            EvaluationDuration = evaluationDuration;
            CultureLCID = cultureLCID;
            UICultureLCID = uiCultureLCID;
            Interrupted = interrupted;
            InitializedIntegration = initializedIntegration;
            LoadedAssemblies = loadedAssemblies;
        }

        public Evaluation (
            CodeCellId codeCellId,
            EvaluationResultHandling resultHandling,
            object value)
            : this (
                codeCellId,
                resultHandling,
                value,
                exception: null) // to avoid calling self
        {
        }

        internal Evaluation (
            CodeCellId codeCellId,
            EvaluationResultHandling resultHandling,
            object value,
            ExceptionNode exception = null,
            TimeSpan evaluationDuration = default,
            int cultureLCID = 0,
            int uiCultureLCID = 0,
            bool interrupted = false,
            bool initializedIntegration = false,
            IReadOnlyList<AssemblyDefinition> loadedAssemblies = null)
        {
            CodeCellId = codeCellId;
            ResultHandling = resultHandling;

            switch (value) {
            case null:
                ResultRepresentations = Array.Empty<object> ();
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
            InitializedIntegration = initializedIntegration;
            LoadedAssemblies = loadedAssemblies ?? Array.Empty<AssemblyDefinition> ();
        }
    }
}