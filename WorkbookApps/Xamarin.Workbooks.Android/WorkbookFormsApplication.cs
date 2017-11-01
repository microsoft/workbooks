//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Forms;

namespace Xamarin.Workbooks.Android
{
    public class WorkbookFormsApplication : Application
    {
        public WorkbookFormsApplication ()
        {
            MainPage = new ContentPage ();
        }
    }
}
