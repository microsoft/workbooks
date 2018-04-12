// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text;

using Newtonsoft.Json;

using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Protocol
{
    [JsonObject]
    sealed class XipErrorMessage
    {
        public string Message { get; }
        public ExceptionNode Exception { get; }

        public XipErrorMessage (string message)
            : this (message, (ExceptionNode)null)
        {
        }

        public XipErrorMessage (ExceptionNode exception)
            : this (null, exception)
        {
        }

        public XipErrorMessage (Exception exception)
            : this (null, ExceptionNode.Create (exception))
        {
        }

        public XipErrorMessage (string message, Exception exception)
            : this (message, ExceptionNode.Create (exception))
        {
        }

        [JsonConstructor]
        public XipErrorMessage (string message, ExceptionNode exception)
        {
            Message = message;
            Exception = exception;
        }

        public override string ToString ()
        {
            var builder = new StringBuilder ();

            if (Message != null)
                builder.Append (Message);

            if (Exception != null) {
                if (builder.Length > 0)
                    builder.Append (": ");
                builder.Append (Exception);
            }

            return builder.ToString ();
        }

        public void Throw ()
            => throw new XipErrorMessageException (this);
    }
}