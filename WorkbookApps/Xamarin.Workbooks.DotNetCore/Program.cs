// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive;
using Xamarin.Interactive.DotNetCore;

namespace Xamarin.Workbooks.DotNetCore
{
    static class MainClass
    {
        // Usage: Xamarin.Workbooks.DotNetCore {IdentifyAgentRequest args}
        // Passing no arguments is supported for debug scenarios.
        static void Main ()
            => WorkbookApplication.RunAgentOnCurrentThread<DotNetCoreAgent> ();
    }
}