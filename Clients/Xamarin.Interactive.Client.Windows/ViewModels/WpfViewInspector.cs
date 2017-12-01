//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Windows;
using Xamarin.Interactive.Logging;

using Xamarin.Interactive.Client.Windows.Views;
using Xamarin.Interactive.Client.Windows.Commands;
using Xamarin.Interactive.Client.ViewInspector;

namespace Xamarin.Interactive.Client.Windows.ViewModels
{
    sealed class WpfViewInspector<TView> :  ViewInspectorViewModel, IDisposable where TView : Window
    {
        const string TAG = nameof (WpfViewInspector<TView>);

        public DelegateCommand HighlightCommand { get; }
   
        public TView View { get; }

        public WpfDolly Trackball => (WpfDolly)trackball;

        readonly Highlighter highlighter;

        public WpfViewInspector (ClientSession session, TView view) : base (session)
        {
            trackball = new WpfDolly ();

            highlighter = new Highlighter ();
            highlighter.HighlightEnded += OnHighlightEnded;
            highlighter.ViewSelected += OnHighlighterViewSelected;
            View = view;

            HighlightCommand = new DelegateCommand (
              InspectHighlightedView,
              p => !IsHighlighting);

        }

        void InspectHighlightedView (object obj = null)
        {
            if (!Session.Agent.IsConnected)
                return;

            IsHighlighting = true;
            try {
                highlighter.Start (Session, this.View, SelectedHierarchy);
            } catch (Exception e) {
                Log.Error (TAG, e);
                IsHighlighting = false;
            }
        }

        bool isHighlighting;
        bool IsHighlighting {
            get { return isHighlighting; }
            set {
                isHighlighting = value;
                HighlightCommand.InvalidateCanExecute ();
            }
        }
        
        void OnHighlighterViewSelected (object sender, HighlightSelectionEventArgs args)
        {
            try {
                SelectView (args.SelectedView, true, true);
            } catch (Exception e) {
                Log.Error (TAG, e);
            }
        }

        void OnHighlightEnded (object sender, EventArgs args)
            => IsHighlighting = false;

        public void Dispose ()
        {
            highlighter.Dispose ();
            highlighter.HighlightEnded -= OnHighlightEnded;
            highlighter.ViewSelected -= OnHighlighterViewSelected;
        }
    }
}
