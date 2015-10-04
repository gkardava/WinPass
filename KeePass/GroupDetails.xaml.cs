using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Navigation;
using Coding4Fun.Phone.Controls;
using KeePass.Data;
using KeePass.IO.Data;
using KeePass.IO.Write;
using KeePass.Storage;
using KeePass.Utils;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using KeePass.I18n;

namespace KeePass
{
    public partial class GroupDetails
    {
        private Group _group;
        private IList<string> _ids;
        private DateTime _lastModified;

        ApplicationBarMenuItem mnuPin;
        ApplicationBarIconButton mnuSync;
        public GroupDetails()
        {
            InitializeComponent();

            _ids = new List<string>();

            AppMenu(0).Text = Strings.App_Databases;
            AppMenu(1).Text = Strings.GroupDetails_ClearHistory;
            AppMenu(2).Text = Strings.MainPage_Pin;
            AppMenu(3).Text = Strings.MainPage_DBInfo;
            AppMenu(4).Text = Strings.MainPage_Settings;

            AppButton(0).Text = Strings.GroupDetails_NewEntry;
            AppButton(1).Text = Strings.GroupDetails_NewGroup;
            AppButton(2).Text = Strings.Refresh;
            AppButton(3).Text = Strings.GroupDetails_Search;
            mnuSync = AppButton(2);
            mnuPin = ApplicationBar.MenuItems[2] as ApplicationBarMenuItem;
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            var fromTile = NavigationContext
                .QueryString
                .ContainsKey("fromTile");

            if (fromTile)
                this.ClearBackStack();
        }

        protected override void OnNavigatedTo(
            bool cancelled, NavigationEventArgs e)
        {
            if (cancelled)
                return;

            var database = Cache.Database;

            DateTime convertedDate;
            if ((Cache.DbInfo != null) && (Cache.DbInfo.Details.Modified != null))
            {
                convertedDate = DateTime.Parse(Cache.DbInfo.Details.Modified);
                ApplicationTitle.Text = "WinPass - " + Cache.DbInfo.Details.Name + " (" + convertedDate + ")";
            }

            if (database == null)
            {
                this.BackToDBs();
                return;
            }

            //if (Cache.DbInfo != null && Cache.DbInfo.Details.Type.ToString() == "OneTime")
            //    mnuSync.IsEnabled = false;
            _group = GetGroup(database);
            lstHistory.ItemsSource = null;
            pivotGroup.Header = _group.Name;

            ThreadPool.QueueUserWorkItem(_ =>
                ListItems(_group, database.RecycleBin));

            ThreadPool.QueueUserWorkItem(_ =>
                ListHistory(database));
            ItemRemovedAction();
        }

        void ItemRemovedAction()
        {
            if (Cache.LastMovedItems.Count < 1)
                return;
            var obj = Cache.LastMovedItems.Dequeue();
            if (obj != null)
            {
                if (obj is Group)
                    lstGroup.RemoveItem(new GroupItem(obj as Group, Dispatcher));
                if (obj is Entry)
                    lstGroup.RemoveItem(new GroupItem(obj as Entry, Dispatcher));
            }
        }

        private static bool ConfirmDelete(
            bool pernament, string type, string name)
        {
            var message = !pernament
                ? Properties.Resources.DeletePrompt
                : Properties.Resources.DeletePernamentPrompt;

            message = string.Format(
                message, type, name);

            var confirm = MessageBox.Show(message,
                Properties.Resources.DeleteTitle,
                MessageBoxButton.OKCancel);

            return confirm == MessageBoxResult.OK;
        }

        private bool Delete(Entry entry)
        {
            var database = Cache.Database;
            var pernament = IsPernamentDelete();

            if (!ConfirmDelete(pernament,
                Properties.Resources.Entry,
                entry.Title))
            {
                return false;
            }

            if (!pernament)
            {
                MoveToRecycleBin((writer, recycleBin) =>
                {
                    entry.Group.Entries
                        .Remove(entry);
                    recycleBin.Add(entry);

                    writer.Location(entry);
                });
            }
            else
            {
                Save(x =>
                {
                    x.Delete(entry);
                    database.Remove(entry);
                });
            }

            return true;

        }

        private bool Delete(Group group)
        {
            var database = Cache.Database;
            var pernament = IsPernamentDelete();

            if (!ConfirmDelete(pernament,
                Properties.Resources.Group,
                group.Name))
            {
                return false;
            }

            if (!pernament)
            {
                MoveToRecycleBin((writer, recycleBin) =>
                {
                    group.Remove();
                    recycleBin.Add(group);
                    if (recycleBin.ID == group.ID)
                    {
                        database.RecycleBin = null;
                    }
                    writer.Location(group);
                });
            }
            else
            {
                Save(x =>
                {
                    x.Delete(group);
                    database.Remove(group);
                });
            }
            return true;
        }

        private Group GetGroup(Database database)
        {
            string groupId;
            var queries = NavigationContext.QueryString;

            if (queries.TryGetValue("id", out groupId))
                return database.GetGroup(groupId);

            //_cmdHome.IsEnabled = false;
            return database.Root;
        }

        private bool IsPernamentDelete()
        {
            var database = Cache.Database;

            if (!database.RecycleBinEnabled)
                return true;

            var recycleBin = database.RecycleBin;
            if (recycleBin != null && recycleBin == _group)
                return true;

            return false;
        }

        private bool IsSameData(ICollection<Group> groups,
            ICollection<Entry> entries)
        {
            var lastModified = groups
                .Select(x => x.LastModified)
                .Union(entries.Select(x => x.LastModified))
                .OrderByDescending(x => x)
                .FirstOrDefault();

            var ids = groups
                .Select(x => x.ID)
                .Union(entries.Select(x => x.ID))
                .ToList();

            var sameIds = ids.Intersect(_ids)
                .Count() == ids.Count;

            if (sameIds && lastModified == _lastModified)
                return true;

            _ids = ids;
            _lastModified = lastModified;

            return false;
        }

        private void ListHistory(Database database)
        {
            var dispatcher = Dispatcher;

            var recents = Cache.GetRecents()
                .Select(database.GetEntry)
                .Where(x => x != null)
                .Select(x => new GroupItem(x, dispatcher))
                .ToList();

            lstHistory.SetItems(recents);
        }


        private void ListItems(Group group, Group recycleBin)
        {
            var dispatcher = Dispatcher;
            var groups = group.Groups.ToList();

            if (recycleBin != null)
            {
                var settings = AppSettings.Instance;

                if (settings.HideRecycleBin)
                    groups.Remove(recycleBin);
            }

            if (IsSameData(groups, group.Entries))
                return;

            var items = new List<GroupItem>();
            items.AddRange(groups
                .OrderBy(x => x.Name)
                .Select(x => new GroupItem(x, dispatcher)));
            items.AddRange(group.Entries
                .OrderBy(x => x.Title)
                .Select(x => new GroupItem(x, dispatcher)));

            lstGroup.SetItems(items);
        }

        private void MoveToRecycleBin(
            Action<DatabaseWriter, Group> action)
        {
            Save(x =>
            {
                var database = Cache.Database;

                var recycleBin = database.RecycleBin;

                if (recycleBin == null)
                {

                    recycleBin = database.AddNew(database.Root,
                    Properties.Resources.RecycleBin);

                    recycleBin.Icon = new IconData
                    {
                        Standard = 43,
                    };
                    database.RecycleBin = recycleBin;
                    x.New(recycleBin);
                }

                action(x, recycleBin);

            });
        }

        private void Save(Action<DatabaseWriter> save)
        {
            IsEnabled = false;

            var info = Cache.DbInfo;
            var database = Cache.Database;
            var writer = new DatabaseWriter();

            info.OpenDatabaseFile(x => writer
                .LoadExisting(x, info.Data.MasterKey));

            save(writer);
            info.SetDatabase(x => writer.CreateRecycleBin(
                x, database.RecycleBin));

            IsEnabled = true;
            ThreadPool.QueueUserWorkItem(_ => ListItems(
                _group, Cache.Database.RecycleBin));

            Cache.UpdateRecents();
            lstHistory.ItemsSource = null;

            ThreadPool.QueueUserWorkItem(_ =>
                ListHistory(database));

            Dispatcher.BeginInvoke(() =>
                info.NotifyIfNotSyncable());
        }

        private void cmdHome_Click(object sender, EventArgs e)
        {
            this.BackToRoot();
        }

        private void cmdNewEntry_Click(object sender, EventArgs e)
        {
            string groupId;
            var queries = NavigationContext.QueryString;

            if (!queries.TryGetValue("id", out groupId) ||
                groupId == null)
            {
                groupId = string.Empty;
            }

            this.NavigateTo<EntryDetails>(
                "group={0}", groupId);
        }

        private void cmdNewGroup_Click(object sender, EventArgs e)
        {
            var dlgNewGroup = new InputPrompt
            {
                Message = Properties.Resources.PromptName,
                Title = Properties.Resources.NewGroupTitle,
            };
            dlgNewGroup.Completed += dlgNewGroup_Completed;

            dlgNewGroup.Show();
        }

        private void dlgNewGroup_Completed(object sender,
            PopUpEventArgs<string, PopUpResult> e)
        {
            if (e.PopUpResult != PopUpResult.Ok)
                return;

            if (string.IsNullOrEmpty(e.Result))
                return;

            Save(x =>
            {
                var database = Cache.Database;

                var group = database
                    .AddNew(_group, e.Result);

                x.New(group);
            });
        }

        private void dlgRename_Completed(object sender,
            PopUpEventArgs<string, PopUpResult> e)
        {
            if (e.PopUpResult != PopUpResult.Ok)
                return;

            if (string.IsNullOrEmpty(e.Result))
                return;

            var dlgRename = (InputPrompt)sender;
            var group = (Group)dlgRename.Tag;

            Save(x =>
            {
                group.Name = e.Result;
                x.Details(group);
            });
        }

        private void lstGroup_Navigation(object sender,
            NavigationListControl.NavigationEventArgs e)
        {
            var item = e.Item as GroupItem;
            if (item == null)
                return;

            NavigationService.Navigate(item.TargetUri);
        }

        private void lstHistory_SelectionChanged(object sender,
            NavigationListControl.NavigationEventArgs e)
        {
            var item = e.Item as GroupItem;
            if (item == null)
                return;

            NavigationService.Navigate(item.TargetUri);
        }

        private void mnuAbout_Click(object sender, EventArgs e)
        {
            this.NavigateTo<Settings>("page=1");
        }

        private void mnuDelete_Click(object sender, RoutedEventArgs e)
        {
            var mnuDelete = (MenuItem)sender;
            var entry = mnuDelete.Tag as Entry;

            if (entry != null)
            {
                if (Delete(entry))
                    lstGroup.RemoveItem(new GroupItem(entry, Dispatcher));
                return;
            }
            var group = (Group)mnuDelete.Tag;
            if (Delete(group))
                lstGroup.RemoveItem(new GroupItem(group, Dispatcher));
        }

        private void mnuHistory_Click(object sender, EventArgs e)
        {
            Cache.ClearRecents();
            lstHistory.ItemsSource = null;
        }

        private void mnuMove_Click(object sender, RoutedEventArgs e)
        {
            var mnuMove = (MenuItem)sender;

            var entry = mnuMove.Tag as Entry;
            if (entry != null)
            {
                this.NavigateTo<MoveTarget>(
                    "entry={0}", entry.ID);

                return;
            }

            var group = (Group)mnuMove.Tag;
            this.NavigateTo<MoveTarget>(
                "group={0}", group.ID);
        }

        private void mnuRename_Click(object sender, RoutedEventArgs e)
        {
            var mnuRename = (MenuItem)sender;
            var group = (Group)mnuRename.Tag;

            var dlgRename = new InputPrompt
            {
                Tag = group,
                Value = group.Name,
                Title = Properties.Resources.RenameTitle,
                Message = Properties.Resources.PromptName,
            };

            dlgRename.Completed += dlgRename_Completed;

            dlgRename.Show();
        }

        private void mnuRoot_Click(object sender, EventArgs e)
        {
            this.BackToDBs();
        }

        private void mnuSearch_Click(object sender, EventArgs e)
        {
            this.NavigateTo<Search>();
        }

        private void mnuSettings_Click(object sender, EventArgs e)
        {
            this.NavigateTo<Settings>("page=0");
        }

        private void mnuDispDBInfo(object sender, EventArgs e)
        {
            var dbfolder = Cache.DbInfo.Folder;
            this.NavigateTo<DBDetails>("db={0}", dbfolder);
        }

        private void mnuSync_Click(object sender, EventArgs e)
        {
            var dbfolder = Cache.DbInfo.Folder;
            this.NavigateTo<MainPage>("db={0}&sync=1", dbfolder);
        }

        private void mnuPin_Click(object sender, EventArgs e)
        {
            //var item = (MenuItem)sender;

            //var database = (DatabaseInfo)item.Tag;

            if (TilesManager.Pin(Cache.DbInfo))
                return;

            MessageBox.Show(
                Properties.Resources.AlreadyPinned,
                Properties.Resources.PinDatabase,
                MessageBoxButton.OK);
        }
    }
}
