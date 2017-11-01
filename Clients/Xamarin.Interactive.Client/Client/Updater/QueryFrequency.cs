//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.I18N;

namespace Xamarin.Interactive.Client.Updater
{
    enum QueryFrequency
    {
        Startup,
        Hourly,
        Daily,
        Weekly,
        Never
    }

    static class QueryFrequencyNames
    {
        public static readonly string [] All = {
            Catalog.GetString ("At Startup"),
            Catalog.GetString ("Hourly"),
            Catalog.GetString ("Daily"),
            Catalog.GetString ("Weekly"),
            Catalog.GetString ("Never")
        };

        public static string ToLocalizedName (this QueryFrequency frequency)
            => All [(int)frequency];
    }
}