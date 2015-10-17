using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using KeePass.Data;
using KeePass.I18n;
using KeePass.Sources;
using KeePass.Storage;
using KeePass.Utils;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Tasks;

namespace KeePass
{
    public partial class MainPage
    {
        private readonly ObservableCollection<DatabaseItem> _items;
        private readonly ApplicationBarIconButton _RefreshButton;
        string syncdb = string.Empty;

        private static bool _fAppLoaded;

        private bool _moved;

        public MainPage()
        {
            InitializeComponent();

            AppButton(0).Text = Strings.MainPage_AddNew;
            AppButton(1).Text = Strings.MainPage_SyncAll;
            AppButton(2).Text = Strings.MainPage_Settings;
            AppButton(3).Text = Strings.About_Review;

            _items = new ObservableCollection<DatabaseItem>();
            lstDatabases.ItemsSource = _items;

            _RefreshButton = AppButton(1);
        }

        protected override void OnNavigatedTo(
            bool cancelled, NavigationEventArgs e)
        {
            _moved = false;

            if (cancelled)
            {
                _moved = true;
                return;
            }

            SourceCapabilityUpdater.Update();

            Cache.Clear();

            if (NavigationContext.QueryString.TryGetValue("sync", out syncdb))
            {
                _fAppLoaded = true;
                syncdb = NavigationContext.QueryString["db"];
                NavigationContext.QueryString.Remove("sync");
            }
            string param;
            if (NavigationContext.QueryString.TryGetValue("languageChange", out param))
            {
                bool langChange;
                if (bool.TryParse(param, out langChange))
                {
                    if (langChange)
                    {
                        while (NavigationService.CanGoBack)
                        {
                            NavigationService.RemoveBackEntry();
                        }
                    }
                    NavigationContext.QueryString.Remove("languageChange");
                }
            }

            var checkTileOpen = e.NavigationMode !=
                NavigationMode.Back;
            RefreshDbList(checkTileOpen, syncdb);
        }

        private void DatabaseUpdated(DatabaseInfo info,
            SyncResults result, string error)
        {
            var listItem = _items.FirstOrDefault(
                x => x.Info == info);
            if (listItem == null)
                return;

            var dispatcher = Dispatcher;
            dispatcher.BeginInvoke(() =>
                listItem.IsUpdating = false);

            switch (result)
            {
                case SyncResults.NoChange:
                case SyncResults.Downloaded:
                    dispatcher.BeginInvoke(() =>
                        UpdateItem(listItem, "updated"));
                    break;

                case SyncResults.Uploaded:

                    dispatcher.BeginInvoke(() =>
                        UpdateItem(listItem, "uploaded"));
                    break;

                case SyncResults.Conflict:
                    dispatcher.BeginInvoke(() =>
                    {
                        UpdateItem(listItem, "uploaded");

                        MessageBox.Show(error,
                            Properties.Resources.ConflictTitle,
                            MessageBoxButton.OK);
                    });
                    break;

                case SyncResults.Failed:
                    var msg = string.Format(
                        Properties.Resources.UpdateFailure,
                        info.Details.Name, error);

                    dispatcher.BeginInvoke(() =>
                    {
                        listItem.UpdatedIcon = null;

                        MessageBox.Show(msg,
                            Properties.Resources.UpdateTitle,
                            MessageBoxButton.OK);
                    });
                    break;
            }
        }

        private void ListDatabases(string tile, string syncdb)
        {
            var dispatcher = Dispatcher;
            var databases = DatabaseInfo.GetAll();

            var open = tile == null ? null
                : databases.FirstOrDefault(
                    x => x.Folder == tile);

            if (open != null)
            {
                dispatcher.BeginInvoke(
                    () => Open(open, true));

                return;
            }

            foreach (var db in databases)
                db.LoadDetails();

            var items = databases
                .Where(x => x.Details != null)
                .Select(x => new DatabaseItem(x))
                .OrderBy(x => x.Name)
                .ToList();

            foreach (var item in items)
            {
                var local = item;
                dispatcher.BeginInvoke(() =>
                {
                    if (_items.Contains(local)) return;
                    UpdateItem(local, null);
                    _items.Add(local);
                });
            }

            var hasUpdatables = items
                .Any(x => x.CanUpdate);

            dispatcher.BeginInvoke(() =>
                _RefreshButton.IsEnabled = hasUpdatables);

            if (syncdb != null)
            {
                foreach (var db in items)
                {
                    var test = (DatabaseInfo)db.Info;
                    if (test.Folder == syncdb)
                        db.IsUpdating = true;
                }
                var udbi = databases.FirstOrDefault(x => x.Folder == syncdb);
                var udb = new DatabaseItem(udbi);
                Update(udb);
            }

            var UpdateAbleDBs = items
                .Where(x => x.CanUpdate);

            // AutoUpdate
            if (!_fAppLoaded)
            {
                _fAppLoaded = true;
                dispatcher.BeginInvoke(() =>
                {
                    if (hasUpdatables)
                    {
                        _RefreshButton.IsEnabled = true;

                        if (AppSettings.Instance.AutoUpdate)
                        {
                            if (AppSettings.Instance.AutoUpdateWLAN)
                            {
                                if (DeviceNetworkInformation.IsWiFiEnabled)
                                {
                                    foreach (var uDB in UpdateAbleDBs)
                                        Update(uDB);
                                }
                            }
                            else
                            {
                                foreach (var uDB in UpdateAbleDBs)
                                    Update(uDB);
                            }
                        }
                    }
                    else
                        _RefreshButton.IsEnabled = false;
                });
            }
        }

        private void Open(DatabaseInfo database,
            bool fromTile)
        {
            if (!fromTile)
            {
                if (!database.HasPassword)
                {
                    this.NavigateTo<Password>(
                        "db={0}", database.Folder);
                }
                else
                {
                    database.Open(Dispatcher);
                    this.NavigateTo<GroupDetails>();
                }
            }
            else
            {
                if (!database.HasPassword)
                {
                    this.NavigateTo<Password>(
                        "db={0}&fromTile=1", database.Folder);
                }
                else
                {
                    database.Open(Dispatcher);
                    this.NavigateTo<GroupDetails>("fromTile=1");
                }
            }
        }

        private void RefreshDbList(bool checkTileOpen, string syncdb)
        {
            _items.Clear();

            string tile;
            if (!checkTileOpen || !NavigationContext
                .QueryString.TryGetValue("tile", out tile))
            {
                tile = null;
            }

            ThreadPool.QueueUserWorkItem(
                _ => ListDatabases(tile, syncdb));
        }

        private void Update(DatabaseItem item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            if (!Network.CheckNetwork())
                return;

            item.IsUpdating = true;
            var database = (DatabaseInfo)item.Info;

            database.Update(_ => item.IsUpdating,
                DatabaseUpdated);
        }

        private static void UpdateItem(
            DatabaseItem item, string icon)
        {
            var info = (DatabaseInfo)item.Info;

            if (info.HasPassword)
            {
                item.PasswordIcon = ThemeData
                    .GetImageSource("unlock");
            }
            else
                item.PasswordIcon = null;

            if (!string.IsNullOrEmpty(icon))
            {
                item.UpdatedIcon = ThemeData
                    .GetImageSource(icon);
            }
            else
                item.UpdatedIcon = null;
        }

        private void lstDatabases_Navigation(object sender,
            NavigationListControl.NavigationEventArgs e)
        {
            var item = e.Item as DatabaseItem;
            if (item == null)
                return;

            if (item.IsUpdating)
                item.IsUpdating = false;
            else
                Open((DatabaseInfo)item.Info, false);
        }

        private void mnuAbout_Click(object sender, EventArgs e)
        {
            this.NavigateTo<Settings>("page=1");
        }

        private void mnuClearKeyFile_Click(
            object sender, RoutedEventArgs e)
        {
            var item = (MenuItem)sender;
            var database = (DatabaseInfo)item.Tag;

            database.SetKeyFile(null);
            var listItem = _items.First(
                x => x.Info == database);
            listItem.HasKeyFile = false;
        }

        private void mnuClear_Click(object sender, RoutedEventArgs e)
        {
            var item = (MenuItem)sender;
            var database = (DatabaseInfo)item.Tag;

            database.ClearPassword();
            var listItem = _items.First(
                x => x.Info == database);

            listItem.HasPassword = false;
            listItem.PasswordIcon = null;
        }

        private void mnuDelete_Click(object sender, RoutedEventArgs e)
        {
            var item = (MenuItem)sender;
            var database = (DatabaseInfo)item.Tag;

            var msg = string.Format(
                Properties.Resources.ConfirmDeleteDb,
                database.Details.Name);

            var confirm = MessageBox.Show(msg,
                Properties.Resources.DeleteDbTitle,
                MessageBoxButton.OKCancel) == MessageBoxResult.OK;

            if (!confirm)
                return;

            database.Delete();
            TilesManager.Deleted(database);

            RefreshDbList(false, null);
        }

        private void mnuKeyFile_Click(object sender, RoutedEventArgs e)
        {
            var item = (MenuItem)sender;
            var database = (DatabaseInfo)item.Tag;
            this.NavigateTo<Download>(
                "folder={0}", database.Folder);
        }

        private void mnuNew_Click(object sender, EventArgs e)
        {
            this.NavigateTo<Download>("folder=");
        }

        private void mnuPin_Click(object sender, RoutedEventArgs e)
        {//-- test it 
            var item = (MenuItem)sender;
            var database = (DatabaseInfo)item.Tag;

            if (TilesManager.Pin(database))
                return;

            MessageBox.Show(
                Properties.Resources.AlreadyPinned,
                Properties.Resources.PinDatabase,
                MessageBoxButton.OK);
        }

        private void mnuRename_Click(object sender, RoutedEventArgs e)
        {
            var item = (MenuItem)sender;
            var database = (DatabaseInfo)item.Tag;
            this.NavigateTo<Rename>("db={0}", database.Folder);
        }

        private void mnuSettings_Click(object sender, EventArgs e)
        {
            this.NavigateTo<Settings>("page=0");
        }

        private void mnuRate_Click(object sender, EventArgs e)
        {
            new MarketplaceReviewTask().Show();
        }

        private void mnuUpdateAll_Click(object sender, EventArgs e)
        {
            var updatables = _items
                .Where(x => x.CanUpdate);

            foreach (var item in updatables)
                Update(item);
        }

        private void mnuUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (!Network.CheckNetwork())
                return;

            var item = (MenuItem)sender;
            var database = (DatabaseInfo)item.Tag;

            var listItem = _items.First
                (x => x.Info == database);

            Update(listItem);
        }

        private void mnuDispDBInfo(object sender, RoutedEventArgs e)
        {
            var item = (MenuItem)sender;
            var database = (DatabaseInfo)item.Tag;
            this.NavigateTo<DBDetails>("db={0}", database.Folder);
        }
    }
}
