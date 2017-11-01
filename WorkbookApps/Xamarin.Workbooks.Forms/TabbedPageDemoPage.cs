//
// TabbedPageDemoPage.cs
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

    class TabbedPageDemoPage : TabbedPage
    {
        public TabbedPageDemoPage ()
        {
            this.Title = "TabbedPage";

            this.ItemsSource = new NamedColor [] {
                new NamedColor ("Red", Color.Red),
                new NamedColor ("Yellow", Color.Yellow),
                new NamedColor ("Green", Color.Green),
                new NamedColor ("Aqua", Color.Aqua),
                new NamedColor ("Blue", Color.Blue),
                new NamedColor ("Purple", Color.Purple)
            };

            this.ItemTemplate = new DataTemplate (() => {
                return new NamedColorPage ();
            });
        }
    }
}
