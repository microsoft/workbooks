// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive
{
    public struct SdkId : IEquatable<SdkId>
    {
        readonly string id;

        public bool IsNull => id == null;

        public SdkId (string id)
            => this.id = id;

        public bool Equals (SdkId id)
            => id.id == this.id;

        public override bool Equals (object obj)
            => obj is SdkId id && Equals (id);

        public override int GetHashCode ()
            => id == null ? 0 : id.GetHashCode ();

        public override string ToString ()
            => id;

        public static bool operator == (SdkId a, SdkId b)
            => a.Equals (b);

        public static bool operator != (SdkId a, SdkId b)
            => !a.Equals (b);

        public static implicit operator SdkId (string id)
            => new SdkId (id);

        public static implicit operator string (SdkId id)
            => id.ToString ();

        #region Well Known IDs

        public static readonly SdkId XamarinIos = "ios-xamarinios";
        public static readonly SdkId XamarinMacFull = "mac-xamarinmac-full";
        public static readonly SdkId XamarinMacModern = "mac-xamarinmac-modern";
        public static readonly SdkId XamarinAndroid = "android-xamarinandroid";
        public static readonly SdkId Wpf = "wpf";
        public static readonly SdkId ConsoleNetFramework = "console";
        public static readonly SdkId ConsoleNetCore = "console-netcore";

        #endregion
    }
}