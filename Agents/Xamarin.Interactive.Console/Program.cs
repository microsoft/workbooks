// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Interactive.ConsoleAgent
{
    static class MainClass
    {
        // Usage: Xamarin.Interactive.Console.exe {IdentifyAgentRequest args}
        // Passing no arguments is supported for debug scenarios.
        static void Main ()
            => WorkbookApplication.RunAgentOnCurrentThread<ConsoleAgent> (MacIntegration.Integrate);
    }
}