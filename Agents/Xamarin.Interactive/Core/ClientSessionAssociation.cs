//
// ClientSessionAssociation.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;

namespace Xamarin.Interactive.Core
{
	[Serializable]
	sealed class ClientSessionAssociation : MainThreadRequest<SuccessResponse>
	{
		public ClientSessionAssociationKind Kind { get; }
		public FilePath WorkingDirectory { get; }

		public ClientSessionAssociation (ClientSessionAssociationKind kind, FilePath workingDirectory)
		{
			Kind = kind;
			WorkingDirectory = workingDirectory;
		}

		protected override Task<SuccessResponse> HandleAsync (Agent agent)
		{
			agent.ChangeDirectory (Kind != ClientSessionAssociationKind.Dissociating ? WorkingDirectory : FilePath.Empty);

			return SuccessResponse.Task;
		}
	}
}