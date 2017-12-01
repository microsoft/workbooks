//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Remote;

using Xamarin.Interactive.Camera;

namespace Xamarin.Interactive.Client.ViewInspector
{
    class ViewInspectorViewModel : INotifyPropertyChanged, IObserver<ClientSessionEvent>
    {
        const string TAG = nameof (ViewInspectorViewModel);

        protected IDolly trackball;

        public event PropertyChangedEventHandler PropertyChanged;

        public ClientSession Session { get; }

        public DelegateCommand RefreshVisualTreeCommand { get; }
        
        public DelegateCommand ShowPaneCommand { get; }

        public ViewInspectorViewModel (ClientSession session)
        {
            Session = session;

            Session?.Subscribe (this);

            RootModel = InspectTreeRoot.CreateRoot (this);

            RefreshVisualTreeCommand = new DelegateCommand (
                RefreshVisualTree,
                p => !IsVisualTreeRefreshing);
          
            ShowPaneCommand = new DelegateCommand (p => {
                if (int.TryParse (p.ToString (), out var pane))
                    SelectedPane = pane;
            });
        }

        protected virtual void OnPropertyChanged ([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (name));

        public ObservableCollection<string> SupportedHierarchies { get; } = new ObservableCollection<string> ();

        bool noSupportedHierarchies = true;
        public bool NoSupportedHierarchies {
            get { return noSupportedHierarchies; }
            set {
                noSupportedHierarchies = value;
                if (noSupportedHierarchies)
                    SelectedPane = 0;
                OnPropertyChanged ();
            }
        }

        string title;
        public string Title {
            get { return title; }
            set {
                title = value;
                OnPropertyChanged ();
            }
        }

        string sessionTitle;
        public string SessionTitle {
            get { return sessionTitle; }
            set {
                sessionTitle = value;
                OnPropertyChanged ();
            }
        }

        string agentTitle;
        public string AgentTitle {
            get { return agentTitle; }
            set {
                agentTitle = value;
                OnPropertyChanged ();
            }
        }

        int selectedPane;
        public int SelectedPane {
            get { return selectedPane; }
            set {
                selectedPane = value;
                OnPropertyChanged ();
            }
        }

        public bool IsInspector { get; } = ClientInfo.Flavor == ClientFlavor.Inspector;

        DisplayMode displayMode = DisplayMode.FramesAndContent;
        public DisplayMode DisplayMode {
            get { return displayMode; }
            set {
                displayMode = value;
                OnPropertyChanged (nameof (DisplayContent));
                OnPropertyChanged (nameof (DisplayFrames));
                OnPropertyChanged ();
            }
        }

        public bool DisplayContent {
            get => displayMode.HasFlag (DisplayMode.Content);
            set {
                DisplayMode = SetFlag (displayMode, DisplayMode.Content, value);
            }
        }

        public bool DisplayFrames {
            get => displayMode.HasFlag (DisplayMode.Frames);
            set {
                DisplayMode = SetFlag (displayMode, DisplayMode.Frames, value);             
            }
        }

        static DisplayMode SetFlag (DisplayMode mode, DisplayMode flag, bool isSet) =>
            isSet ? mode | flag : mode & ~flag;

        RenderingDepth renderingDepth = RenderingDepth.ThreeDimensional;
        public RenderingDepth RenderingDepth {
            get { return renderingDepth; }
            set {
                renderingDepth = value;
                trackball?.Reset ();
                OnPropertyChanged (nameof (IsOrthographic));
                OnPropertyChanged ();
            }
        }

        public InspectTreeRoot RootModel {
            get;
        }

        public bool IsOrthographic {
            get { return RenderingDepth == RenderingDepth.TwoDimensional; }
            set {
                RenderingDepth = value ? RenderingDepth.TwoDimensional : RenderingDepth.ThreeDimensional;
            }
        }

        bool showHiddenViews = false;
        public bool ShowHidden {
            get { return showHiddenViews; }
            set {
                showHiddenViews = value;
                OnPropertyChanged ();
            }
        }

        string selectedHierarchy;
        public string SelectedHierarchy {
            get { return selectedHierarchy; }
            set {
                selectedHierarchy = value;
                OnPropertyChanged ();
                RefreshVisualTreeAsync ().Forget ();
            }
        }

        InspectView selectedView;
        public InspectView SelectedView {
            get { return selectedView; }
            set {
                if (selectedView != value) {
                    selectedView = value;
                    SelectView (selectedView, false, false);
                    OnPropertyChanged ();
                }
            }
        }

        InspectView representedView;
        public InspectView RepresentedView {
            get { return representedView; }
            set {
                if (representedView != value) {
                    representedView = value;
                    OnPropertyChanged ();
                }
            }
        }

        InspectView rootView;
        public InspectView RootView {
            get { return rootView; }
            set {
                if (rootView != value) {
                    rootView = value;
                    OnPropertyChanged ();
                }
            }
        }

        public virtual async Task RefreshVisualTreeAsync ()
        {
            if (IsVisualTreeRefreshing)
                return;

            InspectView remoteView = null;
            try {
                IsVisualTreeRefreshing = true;

                try {
                    var supportedHierarchies = Session.Agent.Features?.SupportedViewInspectionHierarchies;
                    UpdateSupportedHierarchies (supportedHierarchies);

                    if (supportedHierarchies != null && supportedHierarchies.Length > 0) {
                        if (!supportedHierarchies.Contains (selectedHierarchy)) {
                            selectedHierarchy = supportedHierarchies [0];
                            OnPropertyChanged (nameof (SelectedHierarchy));
                        }

                        remoteView = await Session.Agent.Api.GetVisualTreeAsync (
                            selectedHierarchy,
                            captureViews: true);
                    } else {
                        selectedHierarchy = null;
                    }
                } catch (Exception e) {
                    PresentError (e.ToUserPresentable (Catalog.GetString ("Error trying to inspect remote view")));
                }

                SelectView (remoteView, false, false);
            } finally {
                IsVisualTreeRefreshing = false;
            }
        }

        protected virtual void PresentError (UserPresentableException upe)
        {
            throw upe;
        }

        void RefreshVisualTree (object o = null) => RefreshVisualTreeAsync ().Forget ();

        bool isVisualTreeRefreshing;
        public bool IsVisualTreeRefreshing {
            get { return isVisualTreeRefreshing; }
            set {
                isVisualTreeRefreshing = value;
                RefreshVisualTreeCommand.InvalidateCanExecute ();
            }
        }

        public void SelectView (InspectView view, bool withinExistingTree, bool setSelectedView)
        {
            if (!Session.Agent.IsConnected)
                return;

            if (withinExistingTree && RootView != null && view != null)
                view = RootView.FindSelfOrChild (v => v.Handle == view.Handle);

            var root = view?.Root;
            var current = view;

            // find the "window" to represent in the 3D view by either
            // using the root node of the tree for trees with real roots,
            // or by walking up to find the real root below the fake root
            if (root != null &&
                root.IsFakeRoot &&
                current != root) {
                while (current.Parent != null && current.Parent != root)
                    current = current.Parent;
            } else
                current = root;

            RootView = root;
            RepresentedView = current;
            if (setSelectedView)
                SelectedView = view;
        }

        protected void UpdateSupportedHierarchies (IEnumerable<string> hierarchies)
        {
            var hash = new HashSet<string> (hierarchies);

            // Merge the two collections.
            for (var i = 0; i < SupportedHierarchies.Count; i++) {
                var hierarchy = SupportedHierarchies[i];
                if (!hash.Contains (hierarchy))
                    SupportedHierarchies.RemoveAt (i);
                else hash.Remove (hierarchy);
            }

            foreach (var hierarchy in hash)
                SupportedHierarchies.Add (hierarchy);

            NoSupportedHierarchies = SupportedHierarchies.Count == 0;

            OnPropertyChanged (nameof (SupportedHierarchies));
        }

        void OnSessionTitleUpdated ()
        {
            var appName = ClientInfo.FullProductName.ToUpperInvariant ();
            SessionTitle = Session.Title;
            if (Session.SecondaryTitle != null)
                AgentTitle = $" - {Session.SecondaryTitle}";
            Title = $"{Session.Title} - {ClientInfo.FullProductName}";
        }

        public void OnSessionAvailable () { }

        public void OnAgentConnected () {
            NoSupportedHierarchies = Session.Agent.Features.SupportedViewInspectionHierarchies.Length == 0;
            if (SelectedPane == 1)
                RefreshVisualTree ();
        }

        void IObserver<ClientSessionEvent>.OnNext (ClientSessionEvent evnt)
        {
            switch (evnt.Kind) {
            case ClientSessionEventKind.SessionAvailable:
                OnSessionAvailable ();
                break;
            case ClientSessionEventKind.SessionTitleUpdated:
                OnSessionTitleUpdated ();
                break;
            case ClientSessionEventKind.AgentConnected:
                OnAgentConnected ();
                break;
            }
        }

        void IObserver<ClientSessionEvent>.OnError (Exception error)
        {
        }

        void IObserver<ClientSessionEvent>.OnCompleted ()
        {
        }
    }
}
