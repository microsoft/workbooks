//
// FileNode.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using Xamarin.Interactive.TreeModel;

namespace Xamarin.Interactive.Workbook.Structure
{
	class FileNode : TreeNode
	{
		public FileNode ()
		{
			IsRenamable = true;
		}

		string fileName;
		public string FileName {
			get { return fileName; }
			set {
				if (fileName != value) {
					fileName = value;
					NotifyPropertyChanged ();
					UpdateIcon ();
				}
			}
		}

		void UpdateIcon ()
		{
			string extension = null;
			if (fileName != null)
				extension = System.IO.Path.GetExtension (fileName).ToLowerInvariant ();

			switch (extension) {
			case ".workbook":
				IconName = "file-workbook-page";
				break;
			case ".csx":
			case ".cs":
				IconName = "file-source";
				break;
			case ".png":
			case ".jpg":
			case ".jpeg":
			case ".gif":
				IconName = "file-image";
				break;
			case ".mp3":
			case ".m4a":
			case ".aac":
			case ".flac":
			case ".ogg":
			case ".wav":
				IconName = "file-audio";
				break;
			case ".txt":
			case ".md":
				IconName = "file-text";
				break;
			default:
				IconName = "file-generic";
				break;
			}
		}
	}
}