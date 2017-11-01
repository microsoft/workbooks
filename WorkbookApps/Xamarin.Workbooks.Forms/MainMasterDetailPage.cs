//
// MainMasterDetailPage.cs
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
using System;

using Xamarin.Forms;

namespace MasterDetailPageNavigation
{
    public class MainMasterDetailPage : MasterDetailPage
    {
        MasterPage masterPage;

        public MainMasterDetailPage ()
        {
            masterPage = new MasterPage ();
            Master = masterPage;
            Detail = new NavigationPage (new ContactsPage ());

            masterPage.ListView.ItemSelected += OnItemSelected;

            if (Device.RuntimePlatform == Device.Windows)
                Master.Icon = "swap.png";
        }

        void OnItemSelected (object sender, SelectedItemChangedEventArgs e)
        {
            var item = e.SelectedItem as MasterPageItem;
            if (item != null) {
                Detail = new NavigationPage ((Page)Activator.CreateInstance (item.TargetType));
                masterPage.ListView.SelectedItem = null;
                IsPresented = false;
            }
        }
    }
}
