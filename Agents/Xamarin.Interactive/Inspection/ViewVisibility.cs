//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Inspection
{
    /// <summary>
	/// Specifies the visibility of a view.
	/// </summary>
	/// <remarks>
	/// Pretty much all UI toolkits have some notion just like this,
	/// but the WPF names are the best, so stick with them here.
	/// </remarks>
    [Serializable]
    enum ViewVisibility
    {
        /// <summary>
		/// Shown, considered for layout.
		/// </summary>
        Visible,
        /// <summary>
		/// Not shown, considered for layout.
		/// </summary>
        Hidden,
        /// <summary>
		/// Not shown, not considered for layout.
		/// </summary>
        Collapsed,
    }
}
