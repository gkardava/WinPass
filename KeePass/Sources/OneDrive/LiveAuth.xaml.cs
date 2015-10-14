using System;
using System.Windows.Navigation;
using KeePass.Utils;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace KeePass.Sources.OneDrive
{
    public partial class LiveAuth
    {
        private readonly ProgressIndicator _indicator;

        public LiveAuth()
        {
            InitializeComponent();
            _indicator = AddIndicator();
        }
        private string _folder;
        protected override void OnNavigatedTo(bool cancelled, NavigationEventArgs e)
        {
            _folder = NavigationContext
                   .QueryString["folder"];
            base.OnNavigatedTo(cancelled, e);
        }

        private void CheckToken(Uri uri)
        {
            const string prefix = "?code=";

            var query = uri.Query;
            if (!query.StartsWith(prefix,
                StringComparison.InvariantCultureIgnoreCase))
                return;

            var code = query.Substring(prefix.Length);
            OneDriveClient.GetToken(code, token =>
            {

                this.NavigateTo<List>(
                    "token={0}&folder={1}",
                    token, _folder);
            });
        }

        private void ShowLogin()
        {
            var theme = ThemeData.IsDarkTheme
                ? "Dark" : "Light";

            var url = string.Format(
                OneDrive.Resources.AuthUrl,
                ApiKeys.ONEDRIVE_CLIENT_ID,
                ApiKeys.ONEDRIVE_REDIRECT, theme);

            browser.Navigate(new Uri(url));
        }

        private void browser_LoadCompleted(object sender,
            System.Windows.Navigation.NavigationEventArgs e)
        {
            _indicator.IsVisible = false;
        }

        private void browser_Loaded(object sender,
            System.Windows.RoutedEventArgs e)
        {
            ShowLogin();
        }

        private void browser_Navigating(
            object sender, NavigatingEventArgs e)
        {
            _indicator.IsVisible = true;

            try
            {
                var uri = e.Uri;
                if (uri.ToString().StartsWith(
                    ApiKeys.ONEDRIVE_REDIRECT))
                {
                    CheckToken(uri);
                    return;
                }

                if (uri.Host.Contains("live.com"))
                    return;

                e.Cancel = true;
                ShowLogin();
            }
            finally
            {
                _indicator.IsVisible = !e.Cancel;
            }
        }
    }
}