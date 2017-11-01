//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;

using Xamarin.Forms;

namespace Xamarin.Workbooks.Forms
{
    public partial class App : Application
    {
        public App (params Page [] extraPages)
        {
            InitializeComponent ();

            // Some extra pages are in shared code here, rather than provided by callers.
            var realPages = extraPages.Concat (new [] {
                new ListViewPage ()
            }).ToArray ();

            MainPage = new NavigationPage (new MainPage (realPages));
            // MainPage = new TabbedPageDemoPage ();
            // MainPage = new MasterDetailPageNavigation.MainMasterDetailPage ();
        }
    }
}
