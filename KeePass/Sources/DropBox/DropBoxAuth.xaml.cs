using System;
using System.Net;
using System.Windows;
using Dropbox.Api;
using KeePass.Utils;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace KeePass.Sources.DropBox
{
    public partial class DropBoxAuth
    {
        private const string CALL_BACK = "https://github.com/gkardava/WinPass/blob/master/README.md"; //TODO remove url
        // CALL_BACK is the url registered in the App Console. According to DropBox it is common to use a localhost URI.

        private readonly ProgressIndicator _indicator;
        private readonly string _oauth2State = Guid.NewGuid().ToString("N");

        public DropBoxAuth()
        {
            InitializeComponent();

            _indicator = AddIndicator();
        }

        private void CheckToken(Uri uri)
        {
            try
            {
                var result = DropboxOAuth2Helper.ParseTokenFragment(uri);
                if (result.State != _oauth2State)
                {
                    ShowError();
                }
                else
                {
                    var folder = NavigationContext
                        .QueryString["folder"];

                    this.NavigateTo<List>(
                        "token={0}&folder={1}",
                        result.AccessToken, folder);
                }
            }
            catch(ArgumentException)
            {
                ShowError();
            }
        }

        private void ShowError()
        {
            Dispatcher.BeginInvoke(() =>
                MessageBox.Show(
                    DropBoxResources.GetTokenError,
                    "DropBox",
                    MessageBoxButton.OK));
        }

        private void browser_LoadCompleted(object sender,
            System.Windows.Navigation.NavigationEventArgs e)
        {
            _indicator.IsVisible = false;
        }

        private void browser_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Network.CheckNetwork())
                return;

            var authorizeUri = DropboxOAuth2Helper.GetAuthorizeUri(OAuthResponseType.Token, ApiKeys.DROPBOX_KEY, CALL_BACK, state: _oauth2State);
            Dispatcher.BeginInvoke(() => browser.Navigate(authorizeUri));
        }

        private void browser_Navigating(object sender, NavigatingEventArgs e)
        {
            _indicator.IsVisible = !e.Cancel;

            if (e.Uri.ToString().StartsWith(CALL_BACK))
            {
                e.Cancel = true;
                CheckToken(e.Uri);
            }
        }
    }
}