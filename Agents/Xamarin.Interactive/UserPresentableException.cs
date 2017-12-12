//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Text;

using Xamarin.Interactive.Messages;

namespace Xamarin.Interactive
{
    /// <summary>
    /// Throw this exception to present the message to the user via a GUI.
    /// </summary>
    class UserPresentableException : Exception
    {
        public string Details { get; }
        public object UIContext { get; }

        public string CallerMemberName { get; }
        public string CallerFilePath { get; }
        public int CallerLineNumber { get; }

        public UserPresentableException (
            string message,
            string details = null,
            object uiContext = null,
            [CallerMemberName] string callerMemberName = null,
            [CallerFilePath] string callerFilePath = null,
            [CallerLineNumber] int callerLineNumber = 0)
            : this (null, message, details, uiContext, callerMemberName, callerFilePath, callerLineNumber)
        {
        }

        public UserPresentableException (
            Exception innerException,
            string message,
            string details = null,
            object uiContext = null,
            [CallerMemberName] string callerMemberName = null,
            [CallerFilePath] string callerFilePath = null,
            [CallerLineNumber] int callerLineNumber = 0)
            : base (message, innerException)
        {
            Details = details;
            UIContext = uiContext;

            CallerMemberName = callerMemberName;
            CallerFilePath = callerFilePath;
            CallerLineNumber = callerLineNumber;
        }

        public override string ToString ()
        {
            // Based on implementation of Exception.ToString in reference source.
            // Just inserts our Details into the output.
            var s = GetType ().ToString ();
            if (Message?.Length > 0)
                s += ": " + Message;
            if (Details?.Length > 0)
                s += (Message?.Length > 0 ? Environment.NewLine + "Additional Details: " : ": ") + Details.Trim();
            if (InnerException != null)
                s += " ---> " + InnerException + Environment.NewLine + "   --- End of inner exception stack trace ---";
            if (StackTrace != null)
                s += Environment.NewLine + StackTrace;
            return s;
        }
    }

    static class UserPresentableExceptionExtensions
    {
        public static UserPresentableException ToUserPresentable (
            this Exception e,
            string message,
            [CallerMemberName] string callerMemberName = null,
            [CallerFilePath] string callerFilePath = null,
            [CallerLineNumber] int callerLineNumber = 0)
            => new UserPresentableException (
                e,
                message,
                GetUserDetails (e),
                null,
                callerMemberName,
                callerFilePath,
                callerLineNumber);

        public static Message ToAlertMessage (this Exception e, string genericMessage)
            => Message.CreateErrorAlert (
                e as UserPresentableException ?? e.ToUserPresentable (genericMessage));

        static string GetUserDetails (Exception e)
        {
            var aggregate = e as AggregateException;
            if (aggregate != null) {
                var builder = new StringBuilder ();
                foreach (var inner in aggregate.Flatten ().InnerExceptions)
                    builder.AppendLine (inner.Message);
                return builder.ToString ();
            }
            return e.Message;
        }
    }
}