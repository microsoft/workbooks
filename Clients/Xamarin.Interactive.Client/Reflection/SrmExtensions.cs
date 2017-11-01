//
// SrmExtensions.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace Xamarin.Interactive.Reflection
{
    static class SrmExtensions
    {
        static AssemblyName ReadAssemblyName (MetadataReader reader,
            StringHandle nameHandle, Version version,
            StringHandle cultureInfoNameHandle)
        {
            var asmName = new AssemblyName {
                Name = reader.GetString (nameHandle),
                Version = version
            };

            var cultureInfoName = reader.GetString (cultureInfoNameHandle);
            if (cultureInfoName != null)
                asmName.CultureInfo = CultureInfo.GetCultureInfo (cultureInfoName);

            return asmName;
        }

        public static AssemblyName ReadAssemblyName (this AssemblyDefinition assemblyDefinition,
            MetadataReader reader)
        {
            var asmName = ReadAssemblyName (
                reader,
                assemblyDefinition.Name,
                assemblyDefinition.Version,
                assemblyDefinition.Culture);

            var publicKey = reader.GetBlobBytes (assemblyDefinition.PublicKey);
            if (publicKey != null)
                asmName.SetPublicKey (publicKey);

            return asmName;
        }

        public static AssemblyName ReadAssemblyName (this AssemblyReference assemblyReference,
            MetadataReader reader)
        {
            var asmName = ReadAssemblyName (
                reader,
                assemblyReference.Name,
                assemblyReference.Version,
                assemblyReference.Culture);

            var f = asmName.FullName;

            var publicKeyOrToken = reader.GetBlobBytes (assemblyReference.PublicKeyOrToken);
            if (publicKeyOrToken != null)
                asmName.SetPublicKeyToken (publicKeyOrToken);

            return asmName;
        }

        public static IEnumerable<string> GetLinkWithLibraryNames (this MetadataReader reader)
        {
            foreach (var attrHandle in reader.GetAssemblyDefinition ().GetCustomAttributes ()) {
                var attr = reader.GetCustomAttribute (attrHandle);
                if (attr.Constructor.Kind != HandleKind.MemberReference)
                    continue;

                var ctor = reader.GetMemberReference ((MemberReferenceHandle)attr.Constructor);

                var typeReference = reader.GetTypeReference ((TypeReferenceHandle)ctor.Parent);
                if (reader.GetString (typeReference.Namespace) != "ObjCRuntime" ||
                    reader.GetString (typeReference.Name) != "LinkWithAttribute")
                    continue;

                // Unfortunately SRM does not yet have a number of facilities made public,
                // such as the *.Decoding namespace. Below we verify the raw signature of the
                // attribute, ensuring `void ObjCRuntime.LinkWithAttribute::.ctor(string)`.
                // See Decoding/MethodSignature.cs.cs in SRM.
                //
                // (header)(generic-type-param-count?)(param-count)(return-type)(param-types...)
                var sigReader = reader.GetBlobReader (ctor.Signature);
                if (sigReader.ReadSignatureHeader ().IsGeneric || // (skip generics)
                    sigReader.ReadCompressedInteger () != 1 || // (param-count)
                    sigReader.ReadCompressedInteger () != (int)SignatureTypeCode.Void || // (return-type)
                    sigReader.ReadCompressedInteger () != (int)SignatureTypeCode.String) // (first param-type)
                    continue;

                // I could not actually find any uses of ReadSerializedString in SRM code,
                // but § II.23.3 Custom attributes of ECMA 335 indicates that we should expect
                // an LE unsigned int16 prolog of 0x0001 immediately followed by the fixed
                // argument value list, the format of which is defined by the ctor signature
                // itself as verified above, and is not repeated in the value blob.
                var valReader = reader.GetBlobReader (attr.Value);
                if (valReader.ReadUInt16 () == 0x0001)
                    yield return valReader.ReadSerializedString ();
            }
        }

        public static bool IsExtractable (this ManifestResource resource) =>
            resource.Implementation.Kind == HandleKind.AssemblyFile &&
            resource.Implementation.IsNil; // row id == 0

        public static unsafe Stream GetStream (this ManifestResource resource, PEReader peReader)
        {
            if (!resource.IsExtractable ())
                throw new ArgumentException ("not an embedded extractable resource", nameof (resource));

            var resourcesRva = peReader.PEHeaders.CorHeader.ResourcesDirectory.RelativeVirtualAddress;
            var section = peReader.GetSectionData (resourcesRva);
            return new EmbeddedResourceStream (section.Pointer + resource.Offset);
        }

        unsafe class EmbeddedResourceStream : Stream
        {
            readonly byte* startPtr;
            readonly byte* endPtr;
            readonly uint length;

            byte* positionPtr;

            public EmbeddedResourceStream (byte* offsetPtr)
            {
                length = *(uint*)offsetPtr;
                startPtr = positionPtr = offsetPtr + 4;
                endPtr = startPtr + length;
            }

            public override bool CanRead => true;
            public override bool CanSeek => true;
            public override bool CanWrite => false;
            public override long Length => length;

            public override long Position {
                get { return positionPtr - startPtr; }
                set {
                    if (value == 0) { // valid even if length==0
                        positionPtr = startPtr;
                        return;
                    }

                    if (value < 0 || value >= length)
                        throw new ArgumentOutOfRangeException (nameof (value), $"= {value}");

                    positionPtr = startPtr + value;
                }
            }

            public override int Read (byte [] buffer, int offset, int count)
            {
                count = (int)Math.Min (count, endPtr - positionPtr);
                if (count > 0) {
                    Marshal.Copy ((IntPtr)(void*)positionPtr, buffer, offset, count);
                    positionPtr += count;
                }
                return count;
            }

            public override long Seek (long offset, SeekOrigin origin)
            {
                byte *seekPtr;

                try {
                    switch (origin) {
                    case SeekOrigin.Begin:
                        seekPtr = startPtr + offset;
                        break;
                    case SeekOrigin.End:
                        seekPtr = endPtr + offset;
                        break;
                    case SeekOrigin.Current:
                        seekPtr = positionPtr + offset;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException (nameof (origin));
                    }
                } catch (OverflowException) {
                    throw new ArgumentOutOfRangeException (nameof (offset));
                }

                if (seekPtr < startPtr || seekPtr >= endPtr)
                    throw new ArgumentOutOfRangeException (nameof (offset));

                positionPtr = seekPtr;

                return positionPtr - startPtr;
            }

            public override void Flush ()
            {
            }

            public override void SetLength (long value)
            {
                throw new NotSupportedException ();
            }

            public override void Write (byte [] buffer, int offset, int count)
            {
                throw new NotSupportedException ();
            }
        }
    }
}