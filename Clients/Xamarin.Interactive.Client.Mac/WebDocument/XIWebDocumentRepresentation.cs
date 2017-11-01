//
// XIWebDocumentRepresentation.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

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