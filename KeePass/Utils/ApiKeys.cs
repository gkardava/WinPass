using System;

namespace KeePass.Utils
{

#warning remove git_mode :)
#if git_mode
     internal static partial class ApiKeys
    {
#warning WinPass needs API Keys to use web services
        public const string DROPBOX_KEY = "DROPBOX_KEY";
        public const string DROPBOX_SECRET = "DROPBOX_SECRET";
        public const string ONEDRIVE_SECRET = "ONEDRIVE_SECRET";
        public const string ONEDRIVE_CLIENT_ID = "ONEDRIVE_CLIENT_ID";
        public const string ONEDRIVE_REDIRECT = "https://login.live.com/oauth20_desktop.srf";
    }
#endif
}