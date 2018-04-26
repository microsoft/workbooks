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
        public EvaluationStatus Status { get; }
        public EvaluationResultHandling ResultHandling { get; }
        public IRepresentedType ResultType { get; }
        public IReadOnlyList<object> ResultRepresentations { get; }
        public TimeSpan EvaluationDuration { get; }
        public int CultureLCID { get; }
        public int UICultureLCID { get; }
        public bool InitializedIntegration { get; }
        public IReadOnlyList<AssemblyDefinition> LoadedAssemblies { get; }

        public bool IsNullResult => ResultType == null ||
            ResultRepresentations == null ||
            ResultRepresentations.Count == 0;

        [JsonConstructor]
        Evaluation (
            CodeCellId codeCellId,
            EvaluationStatus status,
            EvaluationResultHandling resultHandling,
            IRepresentedType resultType,
            IReadOnlyList<object> resultRepresentations,
            bool resultIsException,
            TimeSpan evaluationDuration,
            int cultureLCID,
            int uiCultureLCID,
            bool initializedIntegration,
            IReadOnlyList<AssemblyDefinition> loadedAssemblies)
        {
            CodeCellId = codeCellId;
            Status = status;
            ResultHandling = resultHandling;
            ResultType = resultType;
            ResultRepresentations = resultRepresentations;
            EvaluationDuration = evaluationDuration;
            CultureLCID = cultureLCID;
            UICultureLCID = uiCultureLCID;
            InitializedIntegration = initializedIntegration;
            LoadedAssemblies = loadedAssemblies;
        }

        public Evaluation (
            CodeCellId codeCellId,
            EvaluationResultHandling resultHandling,
            object value)
            : this (
                codeCellId,
                EvaluationStatus.Success,
                resultHandling,
                value,
                evaluationDuration: default) // to avoid calling self
        {
        }

        internal Evaluation (
            CodeCellId codeCellId,
            EvaluationStatus status,
            EvaluationResultHandling resultHandling,
            object value,
            TimeSpan evaluationDuration = default,
            int cultureLCID = 0,
            int uiCultureLCID = 0,
            bool initializedIntegration = false,
            IReadOnlyList<AssemblyDefinition> loadedAssemblies = null)
        {
            CodeCellId = codeCellId;
            Status = status;
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

            EvaluationDuration = evaluationDuration;
            CultureLCID = cultureLCID;
            UICultureLCID = uiCultureLCID;
            InitializedIntegration = initializedIntegration;
            LoadedAssemblies = loadedAssemblies ?? Array.Empty<AssemblyDefinition> ();
        }
    }
}