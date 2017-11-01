//
// RecentDocumentsController.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.IO;
using System.Collections.Specialized;
using System.ComponentModel;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using Xamarin.Interactive.Collections;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Client
{
	sealed class RecentDocumentsController : MostRecentlyUsedCollection<RecentDocument>
	{
		const string TAG = nameof (RecentDocumentsController);

		static bool ValidateDocument (RecentDocument document)
			=> new FilePath (document.Path).Exists;

		readonly FilePath recentDocumentsFile;

		bool loading;

		public RecentDocumentsController () : base (itemValidationDelegate: ValidateDocument)
		{
			recentDocumentsFile = ClientApp
				.SharedInstance
				.Paths
				.PreferencesDirectory
				.Combine ("recent.yaml");

			Load ();
		}

		/// <summary>
		/// For unit test only.
		/// </summary>
		[EditorBrowsable (EditorBrowsableState.Never)]
		internal RecentDocumentsController (
			FilePath recentDocumentsFile,
			Func<RecentDocument, bool> documentValidationDelegate)
			: base (itemValidationDelegate: documentValidationDelegate)
		{
			this.recentDocumentsFile = recentDocumentsFile;
			Load ();
		}

		void Load ()
		{
			MainThread.Ensure ();

			try {
				loading = true;
				if (recentDocumentsFile.Exists) {
					using (var reader = new StreamReader (recentDocumentsFile))
						Load (new DeserializerBuilder ()
							.WithNamingConvention (new CamelCaseNamingConvention ())
							.Build ()
							.Deserialize<RecentDocument []> (reader));
				}
			} catch (Exception e) {
				Log.Error (TAG, "Unable to load recent documents list", e);
			} finally {
				loading = false;
			}
		}

		void Save ()
		{
			MainThread.Ensure ();

			try {
				recentDocumentsFile.ParentDirectory.CreateDirectory ();
				using (var writer = new StreamWriter (recentDocumentsFile))
					new SerializerBuilder ()
						.WithNamingConvention (new CamelCaseNamingConvention ())
						.Build ()
						.Serialize (writer, this);
			} catch (Exception e) {
				Log.Error (TAG, "Unable to save recent documents list", e);
			}
		}

		protected override void OnCollectionChanged (NotifyCollectionChangedEventArgs args)
		{
			base.OnCollectionChanged (args);

			if (!loading)
				Save ();
		}
	}
}