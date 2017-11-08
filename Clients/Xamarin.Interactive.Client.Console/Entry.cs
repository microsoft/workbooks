//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Client.Console
{
    static class Entry
    {
        static int Main (string [] args)
        {
            var runContext = new SingleThreadSynchronizationContext ();
            SynchronizationContext.SetSynchronizationContext (runContext);

            var mainTask = MainAsync (args);

            mainTask.ContinueWith (
                task => runContext.Complete (),
                TaskScheduler.Default);

            runContext.RunOnCurrentThread ();

            return mainTask.GetAwaiter ().GetResult ();
        }

        static async Task<int> MainAsync (string [] args)
        {
            if (args.Length == 0 || args [0] == null) {
                System.Console.Error.WriteLine ("usage: WORKBOOK_PATH");
                return 1;
            }

            var path = new FilePath (args [0]);
            Uri.TryCreate ("file://" + path.FullPath, UriKind.Absolute, out var fileUri);

            if (!ClientSessionUri.TryParse (fileUri.AbsoluteUri, out var uri)) {
                System.Console.Error.WriteLine ("Invalid URI");
                return 1;
            }

            new ConsoleClientApp ().Initialize (
                logProvider: new LogProvider (LogLevel.Info, null));

            var session = new ClientSession (uri);
            session.InitializeViewControllers (new ConsoleClientSessionViewControllers ());
            await session.InitializeAsync (new ConsoleWorkbookPageHost ());
            await session.WorkbookPageViewModel.EvaluateAllAsync ();

            return 0;
        }
    }
}