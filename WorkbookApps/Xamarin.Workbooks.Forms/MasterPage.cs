//
// MasterPage.cs
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
using System.Collections.Generic;

using Xamarin.Forms;

namespace MasterDetailPageNavigation
{
    public class MasterPage : ContentPage
    {
        public ListView ListView { get { return listView; } }

        ListView listView;

        public MasterPage ()
        {
            var masterPageItems = new List<MasterPageItem> ();
            masterPageItems.Add (new MasterPageItem {
                Title = "Contacts",
                IconSource = "contacts.png",
                TargetType = typeof (ContactsPage)
            });
            masterPageItems.Add (new MasterPageItem {
                Title = "TodoList",
                IconSource = "todo.png",
                TargetType = typeof (TodoListPage)
            });
            masterPageItems.Add (new MasterPageItem {
                Title = "Reminders",
                IconSource = "reminders.png",
                TargetType = typeof (ReminderPage)
            });

            listView = new ListView {
                ItemsSource = masterPageItems,
                ItemTemplate = new DataTemplate (() => {
                    var imageCell = new ImageCell ();
                    imageCell.SetBinding (TextCell.TextProperty, "Title");
                    imageCell.SetBinding (ImageCell.ImageSourceProperty, "IconSource");
                    return imageCell;
                }),
                VerticalOptions = LayoutOptions.FillAndExpand,
                SeparatorVisibility = SeparatorVisibility.None
            };

            Padding = new Thickness (0, 40, 0, 0);
            Icon = "hamburger.png";
            Title = "Personal Organiser";
            Content = new StackLayout {
                VerticalOptions = LayoutOptions.FillAndExpand,
                Children = {
                    listView
                }
            };
        }
    }
}
