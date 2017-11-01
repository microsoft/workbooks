//
// NewWorkbookFeature.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Client;

namespace Xamarin.Interactive.Workbook.NewWorkbookFeatures
{
    abstract class NewWorkbookFeature : INotifyPropertyChanged
    {
		static readonly Lazy<IReadOnlyDictionary<string, NewWorkbookFeature>> allFeatures
			= new Lazy<IReadOnlyDictionary<string, NewWorkbookFeature>> (() =>
				new Dictionary<string, NewWorkbookFeature> {
					[XamarinFormsFeature.SharedInstance.Id] = XamarinFormsFeature.SharedInstance
				});

		public static IReadOnlyDictionary<string, NewWorkbookFeature> AllFeatures => allFeatures.Value;

		public abstract string Id { get; }

		public abstract string Label { get; }

		public abstract string Description { get; }

		bool enabled;
		public bool Enabled {
			get => enabled;
			set {
				if (enabled != value) {
					enabled = value;
					NotifyPropertyChanged ();
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void NotifyPropertyChanged ([CallerMemberName] string propertyName = null)
			=> PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));

		public abstract Task ConfigureClientSession (
			ClientSession clientSession,
			CancellationToken cancellationToken = default (CancellationToken));
	}
}