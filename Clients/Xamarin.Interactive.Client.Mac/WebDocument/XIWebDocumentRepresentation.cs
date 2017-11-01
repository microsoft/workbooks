//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Foundation;
using WebKit;

namespace Xamarin.Interactive.Client.Mac.WebDocument
{
    abstract class XIWebDocumentRepresentation : WebDocumentRepresentation
    {
        public override void FinishedLoading (WebDataSource dataSource)
        {
        }

        public override void ReceivedData (NSData data, WebDataSource dataSource)
        {
        }

        public override void ReceivedError (NSError error, WebDataSource dataSource)
        {
        }

        public override void SetDataSource (WebDataSource dataSource)
        {
        }

        public override bool CanProvideDocumentSource => false;
        public override string DocumentSource => null;
        public override string Title => null;
    }
}