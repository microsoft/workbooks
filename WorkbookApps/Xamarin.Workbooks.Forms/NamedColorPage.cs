//
// NamedColorPage.cs
//
// Author:
//   Charles Petzold <charles.petzold@xamarin.com>
//
// Copyright 2014 (c) Xamarin
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace Xamarin.Workbooks.Forms
{

    // Data type:

    // Format page
    class NamedColorPage : ContentPage
    {
        public NamedColorPage ()
        {
            // This binding is necessary to label the tabs in
            // the TabbedPage.
            this.SetBinding (ContentPage.TitleProperty, "Name");
            // BoxView to show the color.
            BoxView boxView = new BoxView {
                WidthRequest = 100,
                HeightRequest = 100,
                HorizontalOptions = LayoutOptions.Center
            };
            boxView.SetBinding (BoxView.ColorProperty, "Color");

            // Build the page
            this.Content = boxView;
        }
    }
}
