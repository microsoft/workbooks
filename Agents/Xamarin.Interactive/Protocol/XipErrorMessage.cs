//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text;

using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Protocol
{
    [Serializable]
    class XipErrorMessage
    {
        public string Message { get; set; }
        public ExceptionNode Exception { get; set; }

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
        {
            throw new XipErrorMessageException (this);
        }
    }
}