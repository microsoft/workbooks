//
// SrmExtensionsTests.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;

using NUnit.Framework;

using Should;

using Xamarin.Interactive.Reflection;

namespace Xamarin.Interactive.Tests
{
	[TestFixture]
	public class SrmExtensionsTests
	{
		[Test]
		public void TestExtractEmbeddedManifestResources ()
		{
			var asm = typeof (SrmExtensionsTests).Assembly;
			using (var stream = File.OpenRead (asm.Location))
			using (var peReader = new PEReader (stream)) {
				var metadataReader = peReader.GetMetadataReader ();
				foreach (var resourceHandle in metadataReader.ManifestResources) {
					var resource = metadataReader.GetManifestResource (resourceHandle);
					var resourceName = metadataReader.GetString (resource.Name);

					using (var asmStream = asm.GetManifestResourceStream (resourceName).ShouldNotBeNull ())
					using (var srmStream = resource.GetStream (peReader).ShouldNotBeNull ()) {
						srmStream.Length.ShouldEqual (asmStream.Length);

						var asmHash = HexString (SHA512.Create ().ComputeHash (asmStream));
						var srmHash1 = HexString (SHA512.Create ().ComputeHash (srmStream));
						srmStream.Position = 0;
						var srmHash2 = Hash (srmStream, 4096);

						srmHash1.ShouldEqual (asmHash);
						srmHash2.ShouldEqual (asmHash);

						System.Console.WriteLine ("      - {0} is {1} bytes (sha512: {2})",
							resourceName, srmStream.Length, srmHash1);
					}
				}
			}
		}

		/// <summary>
		/// Not using ComputeHash directly since I want to control the buffer size. One test
		/// resource is an exact multiple of 512 (Random4K) to test for unaligned reads.
		/// </summary>
		static string Hash (Stream stream, int bufferSize = 512)
		{
			var buffer = new byte [bufferSize];

			var alg = SHA512.Create ();
			alg.Initialize ();

			while (true) {
				var read = stream.Read (buffer, 0, bufferSize);
				if (read < bufferSize) {
					alg.TransformFinalBlock (buffer, 0, read);
					break;
				}
				alg.TransformBlock (buffer, 0, read, buffer, 0);
			}

			return HexString (alg.Hash);
		}

		static string HexString (byte [] bytes)
		{
			var builder = new StringBuilder ();
			for (int i = 0; i < bytes.Length; i++)
				builder.AppendFormat ("{0:x2}", bytes [i]);
			return builder.ToString ();
		}
	}
}