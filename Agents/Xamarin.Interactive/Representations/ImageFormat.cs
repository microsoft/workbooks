//
// ImageFormat.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.Representations
{
    [Serializable]
    public enum ImageFormat
    {
        Unknown,
        Png,
        Jpeg,
        Gif,
        Rgba32,
        Rgb24,
        Bgra32,
        Bgr24,
        Uri,
        Svg
    }
}