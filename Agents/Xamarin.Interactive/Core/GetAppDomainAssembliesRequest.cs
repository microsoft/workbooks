//
// GetAppDomainAssembliesRequest.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.Protocol;

namespace Xamarin.Interactive.Core
{
    [Serializable]
    sealed class GetAppDomainAssembliesRequest : IXipRequestMessage<Agent>
    {
        public Guid MessageId { get; } = Guid.NewGuid ();

        public bool IncludePeImage { get; }

        public GetAppDomainAssembliesRequest (bool includePeImage)
            => IncludePeImage = includePeImage;

        public void Handle (Agent agent, Action<object> responseWriter)
        {
            var assemblies = new List<AssemblyDefinition> ();

            foreach (var asm in Agent.AppDomainStartupAssemblies) {
                if (!asm.IsDynamic && !String.IsNullOrEmpty (asm.Location)) {
                    // HACK: This is a temporary fix to get iOS agent/app assemblies sent to the
                    //       Windows client when using the remote sim.
                    var peImage = IncludePeImage && File.Exists (asm.Location)
                        ? File.ReadAllBytes (asm.Location)
                        : null;

                    assemblies.Add (new AssemblyDefinition (
                        asm.GetName (),
                        asm.Location,
                        peImage: peImage));
                }
            }

            responseWriter (assemblies.ToArray ());
        }
    }
}