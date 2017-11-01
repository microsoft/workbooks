//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;

namespace ApiDump
{
    static class Program
    {
        static int Usage (string error = null)
        {
            if (error != null) {
                Console.Error.WriteLine ("error: {0}", error);
                Console.Error.WriteLine ();
            }

            Console.Error.WriteLine ("usage: ApiDump [OPTIONS] ASSEMBLY_FILE");
            Console.Error.WriteLine ();
            Console.Error.WriteLine ("OPTIONS:");
            Console.Error.WriteLine ("  -help, -h    show this help");
            Console.Error.WriteLine ("  -o OUTPUT    write output to OUTPUT");
            Console.Error.WriteLine ();
            return 1;
        }

        static int Main (string [] args)
        {
            string assemblyFile = null;
            string outputFile = null;

            for (int i = 0; i < args.Length; i++) {
                switch (args [i]) {
                case "-h":
                case "-help":
                case "--help":
                    return Usage ();
                case "-o":
                    if (i + 1 < args.Length)
                        outputFile = args [++i];
                    else
                        return Usage ("-o missing OUTPUT argument");
                    break;
                default:
                    if (assemblyFile == null)
                        assemblyFile = args [i];
                    else
                        return Usage ("ASSEMBLY_FILE already specified");
                    break;
                }
            }

            if (assemblyFile == null)
                return Usage ("ASSEMBLY_FILE not specified");

            if (!File.Exists (assemblyFile))
                return Usage ($"ASSEMBLY_FILE does not exist: {assemblyFile}");

            var writer = Console.Out;
            if (outputFile != null && outputFile != "-")
                writer = new StreamWriter (outputFile);

            new Driver (assemblyFile).Write (writer);

            writer.Flush ();
            writer.Dispose ();

            return 0;
        }
    }
}