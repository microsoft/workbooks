// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

using Xunit;

namespace Xamarin.Interactive.CodeAnalysis.Resolving
{
    public class DllMapTests
    {
        static DllMap.Filter ParseFilter (string filter)
        {
            string NullIfEmpty (string str)
                => string.IsNullOrEmpty (str) ? null : str;

            var parts = filter.Split (';');
            return new DllMap.Filter (
                NullIfEmpty (parts [0]),
                parts.Length > 1 ? NullIfEmpty (parts [1]) : null,
                parts.Length > 2 ? NullIfEmpty (parts [2]) : null);
        }

        [Theory]
        [InlineData ("linux", "linux", true)]
        [InlineData ("linux,osx", "linux", true)]
        [InlineData ("linux,osx", "osx", true)]
        [InlineData ("!linux", "windows", true)]
        [InlineData ("!linux", "linux", false)]
        [InlineData ("!linux,osx", "osx", false)]
        [InlineData ("!linux,osx", "linux", false)]
        [InlineData ("!linux,osx", "windows", true)]
        [InlineData ("!linux,osx;!x86-64", "windows;x86", true)]
        [InlineData ("!linux,osx;x86-64", "windows;x86", false)]
        [InlineData ("!linux,osx;x86-64", "windows;x86-64", true)]
        [InlineData ("!linux,osx;x86-64,arm", "windows;arm", true)]
        [InlineData ("!linux,osx;!x86-64,arm", "windows;arm", false)]
        [InlineData ("!linux,osx;!x86-64,arm", "windows;armv8", true)]
        [InlineData ("windows;;32", "windows;;32", true)]
        [InlineData ("windows;;!32", "windows;;32", false)]
        [InlineData ("windows;;!64", "windows;;32", true)]
        public void FilterMatch (string predicate, string host, bool expectedMatch)
            => Assert.Equal (
                expectedMatch,
                ParseFilter (predicate).Matches (ParseFilter (host)));

        [Theory]
        [InlineData ("liba", null, "liba-mapped", null)]
        [InlineData ("liba", "specialfunc", "libspecialfunc", "specialfunc")]
        [InlineData ("liba", "specialfunc2", "libspecialfunc", "newspecialfunc2")]
        public void CoreMaps (
            string sourceLibrary,
            string sourceSymbol,
            string targetLibrary,
            string targetSymbol)
        {
            var map = new DllMap {
                {
                    new DllMap.Entity ("liba"),
                    new DllMap.Entity ("liba-mapped")
                },
                {
                    new DllMap.Entity ("liba", "specialfunc"),
                    new DllMap.Entity ("libspecialfunc")
                },
                {
                    new DllMap.Entity ("liba", "specialfunc2"),
                    new DllMap.Entity ("libspecialfunc", "newspecialfunc2")
                },
            };

            var source = new DllMap.Entity (sourceLibrary, sourceSymbol);
            var expectedTarget = new DllMap.Entity (targetLibrary, targetSymbol);

            Assert.True (map.TryMap (source, out var target));
            Assert.Equal (expectedTarget, target);
        }

        const string dllmapXml = @"
            <configuration>
              <dllmap dll='libc' target='preload-libc'/>
              <dllmap dll='libc'>
                <dllentry dll='libdifferent.so' name='somefunction' target='differentfunction'/>
                <dllentry os='solaris,freebsd' dll='libanother.so' name='somefunction' target='differentfunction'/>
              </dllmap>
              <dllmap os='!windows' dll='SolarSystem'>
                <dllentry dll='libearth.so' name='get_Animals'/>
                <dllentry dll='libmars.so' name='get_Plants'/>
              </dllmap>
            </configuration>
        ";

        [Theory]
        [InlineData ("windows", "libc", "somefunction", "libdifferent.so", "differentfunction", true)]
        [InlineData ("solaris", "libc", "somefunction", "libanother.so", "differentfunction", true)]
        [InlineData ("freebsd", "libc", "somefunction", "libanother.so", "differentfunction", true)]
        [InlineData ("linux", "libc", "anyotherfunction", "preload-libc", "anyotherfunction", true)]
        [InlineData ("osx", "libc", null, "preload-libc", null, true)]
        [InlineData ("windows", "SolarSystem", "get_Animals", "SolarSystem", "get_Animals", false)]
        [InlineData ("windows", "SolarSystem", "get_Plants", "SolarSystem", "get_Plants", false)]
        [InlineData ("linux", "SolarSystem", "get_Animals", "libearth.so", "get_Animals", true)]
        [InlineData ("linux", "SolarSystem", "get_Plants", "libmars.so", "get_Plants", true)]
        public void XmlMap (
            string host,
            string sourceLibrary,
            string sourceSymbol,
            string targetLibrary,
            string targetSymbol,
            bool expectedMatch)
        {
            var map = new DllMap (new Runtime (OSPlatform.Create (host))).LoadXml (dllmapXml);

            var source = new DllMap.Entity (sourceLibrary, sourceSymbol);
            var expectedTarget = new DllMap.Entity (targetLibrary, targetSymbol);

            Assert.Equal (expectedMatch, map.TryMap (source, out var target));
            Assert.Equal (expectedTarget, target);
        }
    }
}