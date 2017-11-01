//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

using NuGet.Common;

namespace Xamarin.Interactive.NuGet
{
    sealed class InteractivePackageLogger : ILogger
    {
        const string TAG = "NuGet";

        public void LogDebug (string data)
            => Log (LogLevel.Debug, data);

        public void LogVerbose (string data)
            => Log (LogLevel.Verbose, data);

        public void LogInformation (string data)
            => Log (LogLevel.Information, data);

        public void LogMinimal (string data)
            => Log (LogLevel.Minimal, data);

        public void LogWarning (string data)
            => Log (LogLevel.Warning, data);

        public void LogError (string data)
            => Log (LogLevel.Error, data);

        public void LogInformationSummary (string data)
            => Log (LogLevel.Information, data);

        public void Log (LogLevel level, string data)
            => Logging.Log.Commit (
                level.ToInteractiveLogLevel (),
                TAG,
                data);

        public Task LogAsync (LogLevel level, string data)
        {
            Log (level, data);
            return Task.CompletedTask;
        }

        public void Log (ILogMessage message)
        {
            if (message != null)
                Log (message.Level, message.FormatWithCode ());
        }

        public Task LogAsync (ILogMessage message)
        {
            if (message != null)
                return LogAsync (message.Level, message.FormatWithCode ());
            return Task.CompletedTask;
        }
    }

    static class LogExtensions
    {
        public static Logging.LogLevel ToInteractiveLogLevel (this LogLevel level)
        {
            switch (level) {
            case LogLevel.Debug:
                return Logging.LogLevel.Debug;
            case LogLevel.Error:
                return Logging.LogLevel.Error;
            case LogLevel.Verbose:
                return Logging.LogLevel.Verbose;
            case LogLevel.Warning:
                return Logging.LogLevel.Warning;
            default:
                return Logging.LogLevel.Info;
            }
        }
    }
}