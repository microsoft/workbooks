//
// MacFileSystem.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

using Foundation;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.IO
{
    sealed class MacFileSystem : DotNetFileSystem
    {
        #region Quarantine

        static readonly NSString NSURLQuarantinePropertiesKey = new NSString ("NSURLQuarantinePropertiesKey");

        // See LSQuarantine.h for more keys
        static readonly NSString LSQuarantineAgentName = new NSString ("LSQuarantineAgentName");
        static readonly NSString LSQuarantineOriginURL = new NSString ("LSQuarantineOriginURL");
        static readonly NSString LSQuarantineTimeStamp = new NSString ("LSQuarantineTimeStamp");

        public override QuarantineInfo GetQuarantineInfo (FilePath path)
        {
            if (path.IsNull)
                return null;

            var url = NSUrl.FromFilename (path);

            NSObject resources;
            if (!url.TryGetResource (NSURLQuarantinePropertiesKey, out resources))
                return null;

            var info = resources as NSDictionary;
            if (info == null)
                return null;

            string agentName = null;
            var timeStamp = DateTime.MinValue;
            Uri originUrl = null;

            NSObject agentNameNso;
            if (info.TryGetValue (LSQuarantineAgentName, out agentNameNso) && agentNameNso is NSString)
                agentName = (NSString)agentNameNso;

            NSObject timeStampNso;
            if (info.TryGetValue (LSQuarantineTimeStamp, out timeStampNso) && timeStampNso is NSDate)
                timeStamp = (DateTime)(NSDate)timeStampNso;

            NSObject originUrlNso;
            if (info.TryGetValue (LSQuarantineOriginURL, out originUrlNso) && originUrlNso is NSUrl)
                originUrl = (NSUrl)originUrlNso;

            return new QuarantineInfo (
                path,
                timeStamp,
                originUrl,
                agentName);
        }

        public override void StripQuarantineInfo (FilePath path)
        {
            if (path.IsNull)
                return;

            var url = NSUrl.FromFilename (path);

            NSError error;
            if (url.SetResource (NSURLQuarantinePropertiesKey, NSNull.Null, out error))
                return;

            if (error != null)
                throw new NSErrorException (error);

            throw new Exception ("NSUrl.SetResource (NSURLQuarantinePropertiesKey, null) returned false");
        }

        #endregion
    }
}