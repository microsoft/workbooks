//
// WorkbookFormsApplication.cs
//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright 2017 Microsoft. All rights reserved.

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
