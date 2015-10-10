using System;
using System.Windows;
using System.Windows.Navigation;
using KeePass.I18n;
using KeePass.Storage;
using KeePass.Utils;
using Microsoft.Phone.Controls;
using Windows.Storage.Pickers;

using Windows.Phone.Storage.SharedAccess;
using System.IO.IsolatedStorage;
using Windows.Storage;

using System.Threading.Tasks;
using System.IO;

using Microsoft.Phone.Shell;
using KeePass.Sources;
using Windows.ApplicationModel.Activation;

namespace KeePass.Sources
{
    public partial class Download
    {
        private string _folder;
        public FileOpenPickerContinuationEventArgs FilePickerContinuationArgs { get; set; }

        public Download()
        {
            InitializeComponent();
            AppMenu(0).Text = Strings.Download_Demo;
        }

        protected async override void OnNavigatedTo(
            bool cancelled, NavigationEventArgs e)
        {
            if (cancelled)
                return;

            _folder = NavigationContext.QueryString["folder"];

            if (NavigationContext.QueryString.ContainsKey("fileToken") && !e.IsNavigationInitiator)
            {
                string fileID = NavigationContext.QueryString["fileToken"];
                string incomingFileName = SharedStorageAccessManager.GetSharedFileName(fileID);
                string msg = Strings.Download_OpenConfirm + incomingFileName + "'";
                if (MessageBox.Show(msg, Strings.Download_OpenDB, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    await loadExternalFile(fileID);
                }
            }

            var app = App.Current as App;
            if (app.QueueFileOpenPickerArgs.Count != 0)
            {
                this.ContinueFileOpenPicker(app.QueueFileOpenPickerArgs.Dequeue());
            }
        }

        public async void ContinueFileOpenPicker(FileOpenPickerContinuationEventArgs args)
        {
            if (args.Files != null &&
                args.Files.Count > 0)
            {
                var action = (args.ContinuationData["Action"] as string);
                if (action == "KDBX")
                {
                    StorageFile file = args.Files[0];

                    if (file.Name.EndsWith("kdbx",StringComparison.OrdinalIgnoreCase))
                    {
                        var info = new DatabaseInfo();
                        var test = await file.OpenReadAsync();
                        info.SetDatabase(test.AsStream(), new DatabaseDetails
                        {
                            Source = "FileSystem",
                            Name = file.Name.RemoveKdbx(),
                            Type = SourceTypes.OneTime,
                        });
                        this.NavigateTo<MainPage>();
                    }
                }
                if (action == "KEY")
                {

                }
            }
        }

        private void Navigate<T>()
            where T : PhoneApplicationPage
        {
            this.NavigateTo<T>("folder={0}", _folder);
        }

        private void lnkDemo_Click(object sender, EventArgs e)
        {
            var info = new DatabaseInfo();
            var demoDb = Application.GetResourceStream(
                new Uri("Sources/DemokeepPass.kdbx", UriKind.Relative));

            info.SetDatabase(demoDb.Stream, new DatabaseDetails
            {
                Source = "Demo",
                Name = "Demo Database",
                Type = SourceTypes.OneTime,
            });

            MessageBox.Show(
                Properties.Resources.DemoDbText,
                Properties.Resources.DemoDbTitle,
                MessageBoxButton.OK);

            this.BackToDBs();
        }

        private async Task loadExternalFile(string fileID)
        {
            using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                isoStore.CreateDirectory("temp");
            }
            StorageFolder tempFolder = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFolderAsync("temp");
            // Get the file name.
            string incomingFileName = SharedStorageAccessManager.GetSharedFileName(fileID);
            var file = await SharedStorageAccessManager.CopySharedFileAsync(tempFolder, incomingFileName, NameCollisionOption.ReplaceExisting, fileID);
            var info = new DatabaseInfo();
            var randAccessStream = await file.OpenReadAsync();
            info.SetDatabase(randAccessStream.AsStream(), new DatabaseDetails
            {
                Source = "ExternalApp",
                Name = incomingFileName.RemoveKdbx(),
                Type = SourceTypes.OneTime,
            });

            this.NavigateTo<MainPage>();
        }

        private void lnkDropBox_Click(object sender, RoutedEventArgs e)
        {
            Navigate<DropBox.DropBoxAuth>();
        }

        private void lnkOneDrive_Click(object sender, RoutedEventArgs e)
        {
            Navigate<OneDrive.LiveAuth>();
        }

        private void lnkWebDav_Click(object sender, RoutedEventArgs e)
        {
            Navigate<WebDav.WebDavDownload>();
        }

        private void lnkWeb_Click(object sender, RoutedEventArgs e)
        {
            Navigate<Web.WebDownload>();
        }

        private void lnkLocal_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker fileOpenPicker = new FileOpenPicker();
            fileOpenPicker.ContinuationData["Action"] = "KDBX";
            fileOpenPicker.FileTypeFilter.Add(".kdbx");
            fileOpenPicker.PickSingleFileAndContinue();
        }
    }
}
