using System;
using System.Collections.Generic;

namespace EDLauncher.Core.Install
{
    public record ReleaseInfo(string Version, string PackageUri)
    {
        public static ReleaseInfo Decode(object value)
        {
            var dict = value as IDictionary<string, object>;
            if (dict == null)
                throw new FormatException("Expected dictionary value");

            string version = (string)dict["v"];

            string uri;
            if (dict.ContainsKey("ih"))
            {
                var infohash = (string)dict["ih"];
                uri = $"magnet:?xt=urn:btih:{infohash}&dn=eldewrito";
            }
            else
            {
                uri = (string)dict["l"];
            }

            return new ReleaseInfo(version, uri);
        }

        public static object Encode(ReleaseInfo value)
        {
            var dict = new Dictionary<string, object>();
            dict["v"] = value.Version;
            dict["l"] = value.PackageUri;
            return dict;
        }

        public static string GetChannel(string version)
        {
            // This will do for now to avoid breaking old updaters when we change the format
            if (version.Contains("debug"))
                return "debug";
            else if (version.Contains("release"))
                return "release";

            return "";
        }
    }
}