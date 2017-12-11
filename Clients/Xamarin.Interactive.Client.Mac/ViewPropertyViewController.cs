//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Remote;
using Xamarin.Interactive.Representations;
using Xamarin.Interactive.PropertyEditor;
using Xamarin.PropertyEditing.Mac;

namespace Xamarin.Interactive.Client.Mac
{
    sealed partial class ViewPropertyViewController : ViewInspectorViewController
    {

        ViewPropertyViewController (IntPtr handle) : base (handle)
        {
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();
            PropertyEditorPanel.ThemeManager.Theme = PropertyEditing.Themes.PropertyEditorTheme.Light;
        }

        protected override void OnSelectedViewChanged ()
            => LoadPropertiesAsync ().Forget ();

        async Task LoadPropertiesAsync ()
        {
            if (!Session.Agent.IsConnected)
                return;

            if (propertyEditor.EditorProvider == null)
                propertyEditor.EditorProvider = new InteractiveEditorProvider (Session, new MacPropertyHelper ());

            InteractiveObject properties = null;
            try {
                propertyEditor.SelectedItems.Clear ();
                if (SelectedView != null) {
                    properties = await Session.Agent.Api.GetObjectMembersAsync (
                        SelectedView.Handle);
                    if (properties != null)
                        propertyEditor.SelectedItems.Add (properties);
                }
            } catch (Exception e) {
                e.ToUserPresentable (Catalog.GetString ("Unable to read view properties"))
                    .Present (this);
            }
        }
    }
}