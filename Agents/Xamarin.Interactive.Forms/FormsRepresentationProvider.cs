//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

using Xamarin.Interactive.Representations.Reflection;
using Xamarin.Interactive.Serialization;

using XF = Xamarin.Forms;
using XIR = Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Forms
{
    sealed class FormsRepresentationProvider : XIR.RepresentationProvider
    {
        public override IEnumerable<object> ProvideRepresentations (object obj)
        {
            yield return ProvideSingleRepresentation (obj);
        }

        ISerializableObject ProvideSingleRepresentation (object obj)
        {
            if (obj is XF.Color) {
                var color = (XF.Color)obj;
                return new XIR.Representation (new XIR.Color (color.R, color.G, color.B, color.A), true);
            }

            return null;
        }

        public override bool TryConvertFromRepresentation (
            IRepresentedType representedType,
            object [] representations,
            out object represented)
        {
            represented = null;

            if (TryConvertRepresentation<XF.Color, XIR.Color> (
                representedType,
                representations,
                out represented,
                color => new XF.Color (r: color.Red, g: color.Green, b: color.Blue, a: color.Alpha)))
                return true;

            return base.TryConvertFromRepresentation (representedType, representations, out represented);
        }
    }
}
