//
// WorkbookPage.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2016-2017 Microsoft. All rights reserved.

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.I18N;
using Xamarin.Interactive.NuGet;
using Xamarin.Interactive.Workbook.Structure;

namespace Xamarin.Interactive.Workbook.Models
{
	sealed class WorkbookPage : IWorkbookTitledNode, INotifyPropertyChanged
	{
		const string TAG = nameof (WorkbookPage);

		public static readonly string DefaultTitle = Catalog.GetString ("Untitled");

		public WorkbookPackage Workbook { get; }
		public WorkbookDocumentManifest Manifest { get; } = new WorkbookDocumentManifest ();
		public WorkbookDocument Contents { get; }

		public Guid Guid => Manifest.Guid;
		public ImmutableArray<AgentType> PlatformTargets => Manifest.PlatformTargets;
		public ImmutableArray<InteractivePackage> Packages => Manifest.Packages;

		public FileNode TreeNode { get; }
		public TableOfContentsNode TableOfContents { get; }

		FilePath path;

		public FilePath Path {
			get { return path; }
			set {
				if (path != value) {
					path = value;
					TreeNode.FileName = path.Name;
					NotifyPropertyChanged ();
				}
			}
		}

		string title;
		public string Title {
			get { return IsUntitled ? Workbook.DefaultTitle : title; }
			set {
				if (title != value) {
					title = value;
					NotifyPropertyChanged ();
				}
			}
		}

		public bool IsUntitled => string.IsNullOrEmpty (title);

		public bool IsDirty => title != Manifest.Title;

		public event PropertyChangedEventHandler PropertyChanged;

		public WorkbookPage (WorkbookPackage workbook, params AgentType [] platformTargets)
		{
			if (workbook == null)
				throw new ArgumentNullException (nameof (workbook));

			Workbook = workbook;
			Manifest.PlatformTargets = platformTargets.ToImmutableArray ();
			Contents = new WorkbookDocument ();

			TableOfContents = new TableOfContentsNode ();

			TreeNode = new WorkbookTitledNode (this) {
				FileName = ".workbook",
				Children = TableOfContents.Children,
				IsSelectable = true
			};
		}

		void NotifyPropertyChanged ([CallerMemberName] string propertyName = null)
			=> PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));

		public void Write (TextWriter writer, InteractivePackageManager packageManager)
		{
			if (writer == null)
				throw new ArgumentNullException (nameof (writer));

			var manifestCell = Contents.FirstCell as YamlMetadataCell;
			if (manifestCell == null) {
				manifestCell = new YamlMetadataCell ();
				if (Contents.FirstCell == null)
					Contents.AppendCell (manifestCell);
				else
					Contents.InsertCellBefore (Contents.FirstCell, manifestCell);
			}

			Manifest.Title = title;

			if (packageManager?.InstalledPackages != null)
				Manifest.Packages = packageManager.InstalledPackages.ToImmutableArray ();

			manifestCell.Buffer.Value = Manifest.ToString ();

			Contents.Write (writer);

			Contents.RemoveCell (manifestCell);
		}

		public void Read (TextReader reader)
		{
			if (reader == null)
				throw new ArgumentNullException (nameof (reader));

			Contents.Read (reader);
			Manifest.Read (Contents);

			Title = Manifest.Title;
		}
	}
}