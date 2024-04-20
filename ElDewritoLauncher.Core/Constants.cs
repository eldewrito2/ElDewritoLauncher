using System;
using System.IO;

namespace EDLauncher.Core
{
    class Constants
    {
        public const string ApplicationName = "ElDewrito";
        public const string PackageName = "eldewrito";
        public const string PublicKey = "23ED26B21DEA71D94DCA0DA3140C5C00476F1465A5E72A8732F64737EAB65BC0";
        public const string DiscordUrl = "https://discord.gg/0TKY0SDEUHAWL4sG";
        public const string RedditUrl = "https://www.reddit.com/r/HaloOnline";

        public static string GetAppDirectory()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ApplicationName);
        }

        public static string GetLogDirectory()
        {
            return Path.Combine(GetAppDirectory(), "logs");
        }

        public static string GetPackageDirectory()
        {
            return Path.Combine(GetAppDirectory(), "packages");
        }

        public static string GetCacheDirectory()
        {
            return Path.Combine(GetAppDirectory(), "cache");
        }
    }
}
