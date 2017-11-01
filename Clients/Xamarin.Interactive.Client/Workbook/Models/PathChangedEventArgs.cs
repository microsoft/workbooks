//
// PathChangedEventArgs.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Workbook.Models
{
	sealed class PathChangedEventArgs : EventArgs
	{
		public FilePath OldPath { get; }
		public FilePath OldBasePath => OldPath.DirectoryExists ? OldPath : OldPath.ParentDirectory;
		public FilePath NewPath { get; }
		public FilePath NewBasePath => NewPath.DirectoryExists ? NewPath : NewPath.ParentDirectory;

		public PathChangedEventArgs (FilePath oldPath, FilePath newPath)
		{
			OldPath = oldPath;
			NewPath = newPath;
		}
	}
}