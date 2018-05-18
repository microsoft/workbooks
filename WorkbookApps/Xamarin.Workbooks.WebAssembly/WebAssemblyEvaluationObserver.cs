//
// Authors:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Xamarin.Interactive.CodeAnalysis.Evaluating;
using Xamarin.Interactive.CodeAnalysis.Events;
using Xamarin.Interactive.Json;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Serialization;

namespace Xamarin.Workbooks.WebAssembly
{
    sealed class WebAssemblyEvaluationObserver : IObserver<ICodeCellEvent>
    {
        const string TAG = nameof (WebAssemblyEvaluationObserver);
        readonly JsonSerializerSettings serializerSettings = InteractiveJsonSerializerSettings.SharedInstance;

        public void OnCompleted ()
        {
        }

        public void OnError (Exception error)
        {
            global::WebAssembly.Runtime.InvokeJS (
                $"window.EvaluationObserver.onException('{JsonConvert.SerializeObject (error, serializerSettings)}')",
                out int _);
        }

        public void OnNext (ICodeCellEvent value)
        {
            if (value is EvaluationInFlight)
                return;

            global::WebAssembly.Runtime.InvokeJSRaw<string, object> (
                out var exception,
                "window.EvaluationObserver.onNext",
                JsonConvert.SerializeObject (value, serializerSettings));

            if (exception != null)
                Log.Error (TAG, "Got exception invoking JavaScript-side EvaluationObserver.onNext.", exception);
        }
    }
}