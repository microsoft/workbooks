//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

using System.Security.Cryptography;

namespace InstallerVerifier
{
    static class WinTrust
    {
        static readonly Guid WINTRUST_ACTION_GENERIC_VERIFY_V2 = new Guid (
            0xaac56b, 0xcd44, 0x11d0, 0x8c, 0xc2, 0x0, 0xc0, 0x4f, 0xc2, 0x95, 0xee);

        static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr (-1);

        const int WTD_UI_NONE = 2;
        const int WTD_CHOICE_FILE = 1;

        const int WTD_REVOKE_NONE = 0;
        const int WTD_REVOKE_WHOLECHAIN = 1;

        const int WTD_STATEACTION_VERIFY = 1;
        const int WTD_STATEACTION_CLOSE = 2;

        const int WTD_UICONTEXT_EXECUTE = 0;
        const int WTD_UICONTEXT_INSTALL = 1;

        [StructLayout (LayoutKind.Sequential)]
        struct WINTRUST_FILE_INFO
        {
            public int cbStruct;
            public IntPtr pcwszFilePath;
            public IntPtr hFile;
            public IntPtr pgKnownSubject;
        }

        [StructLayout (LayoutKind.Sequential)]
        unsafe struct WINTRUST_DATA
        {
            public int cbStruct;
            public IntPtr pPolicyCallbackData;
            public IntPtr pSIPClientData;
            public int dwUIChoice;
            public int fdwRevocationChecks;
            public int dwUnionChoice;
            public WINTRUST_FILE_INFO* pFile;
            public int dwStateAction;
            public IntPtr hWVTStateData;
            public IntPtr pwszURLReference;
            public ProviderFlags dwProvFlags;
            public int dwUIContext;
            public IntPtr pSignatureSettings;
        }

        [DllImport ("wintrust.dll")]
        static extern unsafe SignatureVerificationResult WinVerifyTrust (
            IntPtr hWnd,
            Guid* pgActionID,
            WINTRUST_DATA* pWVTData);

        public static unsafe SignatureVerificationResult VerifyAuthenticodeTrust (
            ProviderFlags flags,
            string file)
        {
            var fileInfo = new WINTRUST_FILE_INFO ();
            var wintrustData = new WINTRUST_DATA ();
            try {
                fileInfo.cbStruct = Marshal.SizeOf (typeof (WINTRUST_FILE_INFO));
                fileInfo.pcwszFilePath = Marshal.StringToHGlobalAuto (file);

                wintrustData.cbStruct = Marshal.SizeOf (typeof (WINTRUST_DATA));
                wintrustData.dwUIChoice = WTD_UI_NONE;
                wintrustData.dwUnionChoice = WTD_CHOICE_FILE;
                wintrustData.fdwRevocationChecks = WTD_REVOKE_WHOLECHAIN;
                wintrustData.pFile = &fileInfo;
                wintrustData.dwStateAction = WTD_STATEACTION_VERIFY;
                wintrustData.dwProvFlags = flags;
                wintrustData.dwUIContext = WTD_UICONTEXT_INSTALL;

                try {
                    fixed (Guid* pgActionID = &WINTRUST_ACTION_GENERIC_VERIFY_V2)
                        return WinVerifyTrust (IntPtr.Zero, pgActionID, &wintrustData);
                } finally {
                    wintrustData.dwStateAction = WTD_STATEACTION_CLOSE;
                    fixed (Guid* pgActionID = &WINTRUST_ACTION_GENERIC_VERIFY_V2)
                        WinVerifyTrust (IntPtr.Zero, pgActionID, &wintrustData);
                }
            } finally {
                Marshal.FreeHGlobal (fileInfo.pcwszFilePath);
            }
        }

        public static SignatureVerificationResult VerifyAuthenticodeTrust (string file)
            => VerifyAuthenticodeTrust (
                ProviderFlags.RevocationCheckChain |
                ProviderFlags.CacheOnlyUrlRetrieval |
                ProviderFlags.DisableMd2Md4 |
                ProviderFlags.MarkOfTheWeb,
                file);

        /// <summary>
        /// Specifies trust provider settings
        /// </summary>
        [Flags]
        public enum ProviderFlags
        {
            /// <summary>
            /// The trust is verified in the same manner as implemented by Internet Explorer 4.0.
            /// </summary>
            UseIE4Trust = 0x1,

            /// <summary>
            /// The Internet Explorer 4.0 chain functionality is not used.
            /// </summary>
            NoIE4Chain = 0x2,

            /// <summary>
            /// The default verification of the policy provider, such as code signing for Authenticode,
            /// is not performed, and the certificate is assumed valid for all usages.
            /// </summary>
            NoPolicyUsage = 0x4,

            /// <summary>
            /// Revocation checking is not performed.
            /// </summary>
            RevocationCheckNone = 0x10,

            /// <summary>
            /// Revocation checking is performed on the end certificate only.
            /// </summary>
            RevocationCheckEndCert = 0x20,

            /// <summary>
            /// Revocation checking is performed on the entire certificate chain.
            /// </summary>
            RevocationCheckChain = 0x40,

            /// <summary>
            /// Revocation checking is performed on the entire certificate chain,
            /// excluding the root certificate.
            /// </summary>
            RevocationCheckChainExcludeRoot = 0x80,

            /// <summary>
            /// Not supported.
            /// </summary>
            [Obsolete ("Not supported.")]
            Safer = 0x100,

            /// <summary>
            /// Only the hash is verified.
            /// </summary>
            HashOnly = 0x200,

            /// <summary>
            /// The default operating system version checking is performed.
            /// This flag is only used for verifying catalog-signed files.
            /// </summary>
            UseDefaultOSVersionCheck = 0x400,

            /// <summary>
            /// If this flag is not set, all time stamped signatures are considered
            /// valid forever. Setting this flag limits the valid lifetime of the
            /// signature to the lifetime of the signing certificate. This allows
            /// time stamped signatures to expire.
            /// </summary>
            LifetimeSigning = 0x800,

            /// <summary>
            /// Use only the local cache for revocation checks. Prevents revocation
            /// checks over the network. Not supported on Windows XP.
            /// </summary>
            CacheOnlyUrlRetrieval = 0x1000,

            /// <summary>
            /// Disable the use of MD2 and MD4 hashing algorithms. If a file is signed
            /// by using MD2 or MD4 and if this flag is set, an NTE_BAD_ALGID error is
            /// returned. Supported on Windows 7 SP1 and newer.
            /// </summary>
            DisableMd2Md4 = 0x2000,

            /// <summary>
            /// If this flag is specified it is assumed that the file being verified has
            /// been downloaded from the web and has the Mark of the Web attribute.
            /// Policies that are meant to apply to Mark of the Web files will be enforced.
            /// This flag is supported on Windows 8.1 and later operating systems or on
            /// systems that have installed KB2862966.
            /// </summary>
            MarkOfTheWeb = 0x4000
        }
    }
}