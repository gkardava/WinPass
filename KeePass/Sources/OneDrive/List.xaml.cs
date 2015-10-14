using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Navigation;
using Coding4Fun.Phone.Controls;
using KeePass.Data;
using KeePass.I18n;
using KeePass.IO.Data;
using KeePass.Storage;
using KeePass.Utils;
using Microsoft.Phone.Shell;

namespace KeePass.Sources.OneDrive
{
    public partial class List
    {
        private OneDriveClient _client;
        private string _current;
        private string _folder;
        private bool _isToasted;
        public List()
        {
            InitializeComponent();
            AppButton(0).Text = Strings.Refresh;
        }

        protected override void OnNavigatedTo(
            bool cancelled, NavigationEventArgs e)
        {
            if (cancelled)
                return;

            var pars = NavigationContext
                .QueryString;

            _folder = pars["folder"];

            _client = new OneDriveClient(pars["token"]);

            RefreshList(null);
        }

        private void OnFileDownloaded(MetaListItemInfo item,
            string path, byte[] bytes)
        {
            var dispatcher = Dispatcher;

            try
            {
                using (var buffer = new MemoryStream(bytes))
                {
                    if (string.IsNullOrEmpty(_folder))
                    {
                        if (!DatabaseVerifier.Verify(dispatcher, buffer))
                            return;

                        var name = item.Title.RemoveKdbx();

                        var storage = new DatabaseInfo();
                        storage.SetDatabase(buffer, new DatabaseDetails
                        {
                            Url = path,
                            Name = name,
                            Modified = item.Modified,
                            Type = SourceTypes.Synchronizable,
                            Source = DatabaseUpdater.ONEDRIVE_UPDATER,
                        });
                        dispatcher.BeginInvoke(
                    this.BackToDBs);
                    }
                    else
                    {
                        var hash = KeyFile.GetKey(buffer);
                        if (hash == null)
                        {
                            dispatcher.BeginInvoke(() => MessageBox.Show(
                                Properties.Resources.InvalidKeyFile,
                                Properties.Resources.KeyFileTitle,
                                MessageBoxButton.OK));
                            return;
                        }

                        new DatabaseInfo(_folder)
                            .SetKeyFile(hash);

                        dispatcher.BeginInvoke(
                    this.BackToDBPassword);
                    }
                }


            }
            finally
            {
                dispatcher.BeginInvoke(() =>
                    progBusy.IsBusy = false);
            }
        }

        private void RefreshList(string path)
        {
            progBusy.IsBusy = true;
            _isToasted = false;
            _client.List(path, (parent, items) =>
            {
                try
                {
                    _current = path;

                    var grandParent = parent?.Parent;

                    if (!string.IsNullOrEmpty(grandParent))
                    {
                        var list = new List<ListItemInfo>(items);
                        list.Insert(0, new ParentItem(grandParent));

                        lstItems.SetItems(list);
                    }
                    else
                        lstItems.SetItems(items);
                }
                finally
                {
                    Dispatcher.BeginInvoke(() =>
                        progBusy.IsBusy = false);
                }
            });
        }

        private void cmdRefresh_Click(object sender, EventArgs e)
        {
            RefreshList(_current);
        }

        private void lstItems_SelectionChanged(object sender,
            NavigationListControl.NavigationEventArgs e)
        {
            if (!Network.CheckNetwork())
                return;

            var item = e.Item as MetaListItemInfo;

            if (item != null)
            {
                if (item.IsDir)
                {
                    RefreshList(item.Path);
                    return;
                }

                if (item.Size > 10485760) // 10MB
                {
                    MessageBox.Show(Properties.Resources.FileTooLarge);
                    return;
                }

                progBusy.IsBusy = true;
                _client.Download(item.Path,
                    OnFileDownloaded);

                return;
            }

            var parent = e.Item as ParentItem;
            if (parent != null)
                RefreshList(parent.Path);
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            if (!_isToasted)
            {
                var toast = new ToastPrompt
                {
                    TextWrapping = TextWrapping.NoWrap,
                    TextOrientation = System.Windows.Controls.Orientation.Vertical,
                    Message = "Click egain to reaturn main Window",
                    Title = "back pressed"

                };
                toast.Show();
                e.Cancel = _isToasted = true;
            }

            base.OnBackKeyPress(e);
        }
    }
}