// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;

namespace Mono.Terminal
{
    public class Spinner : IDisposable
    {
        public enum Kind
        {
            Classic,
            Dots
        }

        public static Spinner Start (Kind kind, ConsoleColor? color = null)
        {
            switch (kind) {
            case Kind.Classic:
                return new Spinner (
                    color,
                    TimeSpan.FromMilliseconds (100),
                    "-", "\\", "|", "/");
            case Kind.Dots:
                return new Spinner (
                    color,
                    TimeSpan.FromMilliseconds (80),
                    "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏");
            }

            throw new ArgumentOutOfRangeException (nameof (kind));
        }

        ManualResetEvent wait = new ManualResetEvent (false);
        ManualResetEvent stopWait = new ManualResetEvent (false);

        Spinner (ConsoleColor? color, TimeSpan interval, params string [] frames)
        {
            var restoreLeft = Console.CursorLeft;
            var restoreTop = Console.CursorTop;

            bool restoreCursorVisible = true;
            try {
                restoreCursorVisible = Console.CursorVisible;
            } catch (PlatformNotSupportedException) {
                // throws on .NET Core for macOS, so assume we should
                // always restore it back to visible when we"re done
            }

            Console.CursorVisible = false;

            if (color != null)
                Console.ForegroundColor = color.Value;

            ThreadPool.QueueUserWorkItem (o => {
                var frameIndex = 0;

                do {
                    Console.SetCursorPosition (restoreLeft, restoreTop);
                    Console.Write (frames [frameIndex]);
                    frameIndex = (frameIndex + 1) % frames.Length;
                } while (!wait.WaitOne (interval));

                Console.SetCursorPosition (restoreLeft, restoreTop);
                Console.Write (string.Empty.PadLeft (frames [0].Length));
                Console.SetCursorPosition (restoreLeft, restoreTop);

                Console.CursorVisible = restoreCursorVisible;

                if (color != null)
                    Console.ResetColor ();

                stopWait.Set ();
            });
        }

        public void Dispose ()
        {
            wait.Set ();
            stopWait.WaitOne ();

            wait.Dispose ();
            stopWait.Dispose ();
        }
    }
}