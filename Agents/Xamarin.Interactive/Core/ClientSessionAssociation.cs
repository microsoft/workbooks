//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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