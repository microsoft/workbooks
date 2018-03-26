//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.CrossBrowser;
using Xamarin.Interactive.CodeAnalysis.Models;

namespace Xamarin.Interactive.CodeAnalysis.Monaco
{
    static class MonacoExtensions
    {
        const string TAG = nameof (MonacoExtensions);

        public static CancellationToken FromMonacoCancellationToken (dynamic token)
        {
            var cts = new CancellationTokenSource ();
            token.onCancellationRequested ((ScriptAction)((self, args) => {
                cts.Cancel ();
            }));

            if ((bool)token.isCancellationRequested)
                cts.Cancel ();

            return cts.Token;
        }

        public static dynamic ToMonacoPromise<TResult> (
            this ScriptContext context,
            Task<TResult> task,
            Func<ScriptContext, TResult, object> converter,
            TaskScheduler continuationScheduler,
            bool logError = true,
            bool raiseErrors = true)
        {
            return context.GlobalObject.xiexports.monaco.Promise ((ScriptAction)((self, args) => {
                var complete = args [0];
                var error = args [1];

                task.ContinueWith (t => {
                    // There doesn't appear to be any way to notify the promise that the
                    // task was canceled. The assumption appears to be that cancellation
                    // originated with a JS CancellationTokenSource (which we do not get
                    // access to). So...just doing nothing on cancellation.
                    if (t.IsFaulted) {
                        if (logError)
                            Logging.Log.Error (TAG, t.Exception);
                        if (raiseErrors)
                            error (context.GlobalObject.xiexports.createError (t.Exception?.Message));
                    } else if (t.IsCompleted && !t.IsCanceled) {
                        try {
                            complete (converter (context, t.Result));
                        } catch (Exception e) {
                            if (logError)
                                Logging.Log.Error (TAG, e);
                            if (raiseErrors)
                                error (context.GlobalObject.xiexports.createError (e.Message));
                        }
                    }
                }, continuationScheduler);
            }));
        }

        public static dynamic ToMonacoRange (this ScriptContext context, Models.Range range)
            => context.CreateObject (o => {
                o.startLineNumber = range.StartLineNumber;
                o.startColumn = range.StartColumn;
                o.endLineNumber = range.EndLineNumber;
                o.endColumn = range.EndColumn;
            });

        public static dynamic ToMonacoPosition (this ScriptContext context, Position position)
            => context.CreateObject (o => {
                o.lineNumber = position.LineNumber;
                o.column = position.Column;
            });

        public static Position FromMonacoPosition (dynamic position)
            => new Position ((int)position.lineNumber, (int)position.column);
    }
}