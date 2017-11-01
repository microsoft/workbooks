//
// PreferencesFeedbackViewController.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.IO;

using AppKit;
using CoreGraphics;
using Foundation;

using CommonMark;
using CommonMark.Formatters;
using CommonMark.Syntax;

using Xamarin.Interactive.Client;

namespace Xamarin.Interactive.Preferences
{
    sealed partial class PreferencesFeedbackViewController : PreferencesViewController
    {
        PreferencesFeedbackViewController (IntPtr handle) : base (handle)
        {
        }

        sealed class CommonMarkNSTextStorageFormatter : HtmlFormatter
        {
            readonly TextWriter writer;

            public CommonMarkNSTextStorageFormatter (TextWriter writer, CommonMarkSettings settings)
                : base (writer, settings)
            {
                this.writer = writer;
            }

            public new void WriteDocument (Block block)
            {
                var font = NSFont.SystemFontOfSize (NSFont.SmallSystemFontSize);

                writer.WriteLine ($@"
				<style>
					body {{
						font-family: '{font.FamilyName}';
						font-size: {font.PointSize}px;
					}}

					a, h1 {{
						font-size: {NSFont.SystemFontSize}px;
						font-weight: bold;
					}}
				</style>");

                base.WriteDocument (block);
            }

            protected override void WriteBlock (
                Block block, bool isOpening, bool isClosing, out bool ignoreChildNodes)
            {
                // NSAttributedString will end up with paragraph spacing at the
                // end of paragraph elements which leaves empty space at the end
                // of the NSAttributedString; prevent this by committing <p> tags
                // for the last paragraph.
                var renderTightParagraphs = block == block.Top.LastChild;
                if (renderTightParagraphs)
                    RenderTightParagraphs.Push (true);

                base.WriteBlock (block, isOpening, isClosing, out ignoreChildNodes);

                if (renderTightParagraphs)
                    RenderTightParagraphs.Pop ();

                // NSAttributedString lacks paragraph spacing between between
                // adjacent <ul> and <p> nodes, so insert a hard break.
                if (block.Tag == BlockTag.List && isClosing)
                    Write ("<br>");
            }
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            var writer = new StringWriter ();

            var formatter = new CommonMarkNSTextStorageFormatter (
                writer,
                ClientInfo.TelemetryNotice.CommonMarkSettings) {
                PlaceholderResolver = ClientInfo.TelemetryNotice.PlaceholderResolver
            };

            formatter.WriteDocument (ClientInfo.TelemetryNotice.Parse ());

            NSDictionary options;
            NSError error;
            noticeTextView.TextStorage.ReadFromData (
                NSData.FromString (writer.ToString ()),
                new NSAttributedStringDocumentAttributes {
                    DocumentType = NSDocumentType.HTML
                },
                out options,
                out error);

            const int margin = 20;

            noticeTextView.TextContainerInset = new CGSize (margin, margin);

            var contentBounds = noticeTextView.TextStorage.BoundingRectWithSize (
                new CGSize (noticeTextView.Frame.Width - 2 * margin, 0),
                NSStringDrawingOptions.UsesLineFragmentOrigin);

            var contentHeight = contentBounds.Height + 2 * margin;
            var deltaHeight = contentHeight - noticeHeightConstraint.Constant;
            var intrinsicContentSize = new CGSize (
                View.Frame.Width,
                View.Frame.Height + deltaHeight);

            noticeHeightConstraint.Constant = contentHeight;

            ((PreferencesView)View).UpdateIntrinsicContentSize (intrinsicContentSize);

            ReadTelemetryEnabled ();
        }

        protected override void ObservePreferenceChange (PreferenceChange change)
        {
            base.ObservePreferenceChange (change);

            if (change.Key == Prefs.Telemetry.Enabled.Key)
                ReadTelemetryEnabled ();
        }

        void ReadTelemetryEnabled ()
        {
            var enabled = Prefs.Telemetry.Enabled.GetValue ();
            optInRadioButton.State = enabled ? NSCellStateValue.On : NSCellStateValue.Off;
            optOutRadioButton.State = enabled ? NSCellStateValue.Off : NSCellStateValue.On;
        }

        partial void OptInOutActivated (NSObject sender)
            => Prefs.Telemetry.Enabled.SetValue (optInRadioButton.State == NSCellStateValue.On);
    }
}