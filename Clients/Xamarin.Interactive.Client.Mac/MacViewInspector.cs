//
// Authors:
//   Larry Ewing <lewing@xamarin.com>
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using System.Threading.Tasks;
using Xamarin.Interactive.Client.ViewInspector;

namespace Xamarin.Interactive.Client.Mac
{
    class MacViewInspector : ViewInspectorViewModel
    {
        ViewInspectorMainViewController viewController;

        public MacViewInspector (ViewInspectorMainViewController viewController, ClientSession session)
            : base (session)
        {
            this.viewController = viewController;
            OnPropertyChanged (nameof(RootModel));
        }

        public override async Task RefreshVisualTreeAsync ()
        {
            await base.RefreshVisualTreeAsync ();
            viewController.View?.Window?.Toolbar?.ValidateVisibleItems ();
        }

        protected override void OnPropertyChanged (string name)
        {
            base.OnPropertyChanged (name);
            switch (name) {
            case nameof (ViewInspectorViewModel.SelectedView):
            case nameof (ViewInspectorViewModel.RepresentedView):
            case nameof (ViewInspectorViewModel.RootView):
                viewController.ChildViewControllers.OfType<ViewInspectorViewController> ().ForEach (viewController => {
                    viewController.RootView = RootView;
                    viewController.RepresentedView = RepresentedView;
                    viewController.SelectedView = SelectedView;
                });
                break;
            case nameof (ViewInspectorViewModel.RootModel):
                viewController.ChildViewControllers.OfType<ViewInspectorViewController> ().ForEach (viewController => {
                    viewController.Tree = RootModel;
                });
                break;
            case nameof (ViewInspectorViewModel.DisplayMode):
                viewController.ChildViewControllers.OfType<VisualRepViewController> ().ForEach (viewController => {
                    viewController.scnView.DisplayMode = DisplayMode;
                });
                break;
            case nameof (ViewInspectorViewModel.ShowHidden):
                viewController.ChildViewControllers.OfType<VisualRepViewController> ().ForEach (viewController => {
                    viewController.scnView.ShowHiddenViews = ShowHidden;
                });
                break;
            case nameof (ViewInspectorViewModel.RenderingDepth):
                viewController.ChildViewControllers.OfType<VisualRepViewController> ().ForEach (viewController => {
                    viewController.Depth = RenderingDepth;
                });
                break;
            case nameof (ViewInspectorViewModel.SupportedHierarchies):
                var supportedHierarchies = SupportedHierarchies.ToArray ();
                var selected = SelectedHierarchy;

                if (!supportedHierarchies.Contains (SelectedHierarchy))
                    selected = supportedHierarchies? [0];

                viewController.ChildViewControllers.OfType<ViewHierarchyViewController> ().ForEach (vc => {
                    vc.UpdateSupportedHierarchies (supportedHierarchies, selected);
                });
                break;
            case nameof (ViewInspectorViewModel.SelectedHierarchy):
                RefreshVisualTreeAsync ().Forget ();
                break;
            }
        }

        protected override void PresentError (UserPresentableException upe)
            => upe.Present (viewController);
    }
}
