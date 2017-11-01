//
// ReminderPage.cs
//
// Author:
//   David Britch <david.britch@xamarin.com>
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
// limitations under the License
using Xamarin.Forms;

namespace MasterDetailPageNavigation
{
    public class ReminderPage : ContentPage
    {
        public ReminderPage ()
        {
            Title = "Reminder Page";
            Content = new StackLayout {
                Children = {
                    new Label {
                        Text = "Reminder data goes here",
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.CenterAndExpand
                    }
                }
            };
        }
    }
}
