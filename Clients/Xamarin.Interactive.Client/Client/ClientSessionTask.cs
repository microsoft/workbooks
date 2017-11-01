//
// ClientSessionTask.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.Interactive.Client
{
    delegate Task ClientSessionTaskDelegate (CancellationToken cancellationToken);
    delegate void ClientSessionTaskExceptionHandler (Exception exception);

    /// <summary>
	/// A description of a discrete task that <see cref="T:ClientSession"/> may
	/// perform for which user feedback will be given.
	/// </summary>
    sealed class ClientSessionTask
    {
        public string Description { get; }
        public bool IsSuccessfulCompletionRequired { get; }
        public ClientSessionTaskDelegate Delegate { get; }
        public ClientSessionTaskExceptionHandler ExceptionHandler { get; }

        ClientSessionTask (
            string description,
            bool isSuccessfulCompletionRequired,
            ClientSessionTaskDelegate @delegate,
            ClientSessionTaskExceptionHandler exceptionHandler)
        {
            if (description == null)
                throw new ArgumentNullException (nameof (description));

            if (@delegate == null)
                throw new ArgumentNullException (nameof (@delegate));

            Description = description;
            IsSuccessfulCompletionRequired = isSuccessfulCompletionRequired;
            Delegate = @delegate;
            ExceptionHandler = exceptionHandler;
        }

        /// <summary>
		/// Creates a <see cref="T:ClientSessionTask"/> that cannot recover from
		/// failure and cannot be cancelled. If execution of <paramref name="delegate"/>
		/// fails, the <see cref="T:ClientSession"/> will dispose.
		/// </summary>
        public static ClientSessionTask CreateRequired (
            string description,
            ClientSessionTaskDelegate @delegate,
            ClientSessionTaskExceptionHandler exceptionHandler = null)
            => new ClientSessionTask (description, true, @delegate, exceptionHandler);

        /// <summary>
		/// Creates a <see cref="T:ClientSessionTask"/> that can recover from
		/// failure but cannot be cancelled while executing <param name="delegate"/>.
		/// </summary>
        public static ClientSessionTask Create (
            string description,
            ClientSessionTaskDelegate @delegate,
            ClientSessionTaskExceptionHandler exceptionHandler = null)
            => new ClientSessionTask (description, false, @delegate, exceptionHandler);
    }
}