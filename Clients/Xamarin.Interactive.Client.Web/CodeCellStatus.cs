//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.Client.Monaco;

namespace Xamarin.Interactive.Client.Web
{
    class CodeCellStatus
    {
        public bool IsSubmissionComplete { get; set; }

        public MonacoModelDeltaDecoration [] Diagnostics { get; set; }
    }
}