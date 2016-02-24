using System;
using System.Text;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Media;
using Coding4Fun.Phone.Controls;
using KeePass.Controls;
using KeePass.Data;
using KeePass.I18n;
using KeePass.IO.Data;
using KeePass.IO.Write;
using KeePass.Storage;
using KeePass.Utils;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Devices;
using Environment = System.Environment;

namespace KeePass
{
    public partial class EntryDetails
    {
        private readonly ApplicationBarIconButton _cmdReset;
        private readonly ApplicationBarIconButton _cmdSave;

        private ObservableCollection<FieldBinding> _fields;

        private EntryBinding _binding;

        private Entry _entry;

        public EntryDetails()
        {
            InitializeComponent();

            _cmdSave = AppButton(2);
            _cmdReset = AppButton(3);

            AppMenu(0).Text = Strings.EntryDetails_GeneratePassword;
            AppMenu(1).Text = Strings.App_Databases;
            AppMenu(2).Text = Strings.EntryDetails_ShowAllMasked;
            AppMenu(3).Text = Strings.MainPage_Settings;
            AppMenu(4).Text = Strings.App_About;

            AppButton(0).Text = Strings.App_UNCopy;
            AppButton(1).Text = Strings.App_PWCopy;

            _cmdSave.Text = Strings.EntryDetails_SaveEntry;
            _cmdReset.Text = Strings.EntryDetails_ResetEntry;
        }

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplicationBarIconButton UN_Button = new ApplicationBarIconButton();
            UN_Button.IconUri = new Uri("/Images/username.png", UriKind.Relative);
            UN_Button.Text = Strings.App_UNCopy;
            UN_Button.Click += cmdUNCopy_Click;

            ApplicationBarIconButton NF_Button = new ApplicationBarIconButton();
            NF_Button.IconUri = new Uri("/Images/new.png", UriKind.Relative);
            NF_Button.Text = Strings.EntryFields_Add;
            NF_Button.Click += cmdAdd_Click;

            ApplicationBarIconButton AC_Button = new ApplicationBarIconButton();
            AC_Button.IconUri = new Uri("/Images/refresh.png", UriKind.Relative);
            AC_Button.Text = Strings.EntryNotes_ClearAll;
            AC_Button.Click += cmdSync_Click;

            ApplicationBarIconButton Reset_Button = new ApplicationBarIconButton();
            Reset_Button.IconUri = new Uri("/Images/refresh.png", UriKind.Relative);
            Reset_Button.Text = Strings.EntryNotes_ClearAll;
            Reset_Button.Click += cmdReset_Click;

            switch (((Pivot)sender).SelectedIndex)
            {
                case 0:
                    ApplicationBar.Buttons.Remove(ApplicationBar.Buttons[0] as ApplicationBarIconButton);
                    ApplicationBar.Buttons.Insert(0, UN_Button);
                    AppButton(1).IsEnabled = true;
                    break;
                case 1:
                    ApplicationBar.Buttons.Remove(ApplicationBar.Buttons[0] as ApplicationBarIconButton);
                    ApplicationBar.Buttons.Insert(0, NF_Button);

                    ApplicationBar.Buttons.Remove(ApplicationBar.Buttons[3] as ApplicationBarIconButton);
                    ApplicationBar.Buttons.Insert(3, Reset_Button);
                    Reset_Button.IsEnabled = false;

                    AppButton(1).IsEnabled = false;
                    break;
                case 2:
                    ApplicationBar.Buttons.Remove(ApplicationBar.Buttons[3] as ApplicationBarIconButton);
                    ApplicationBar.Buttons.Insert(3, AC_Button);

                    ApplicationBar.Buttons.Remove(ApplicationBar.Buttons[0] as ApplicationBarIconButton);
                    ApplicationBar.Buttons.Insert(0, UN_Button);
                    AppButton(1).IsEnabled = false;
                    AppButton(0).IsEnabled = false;
                    break;
            }
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            if (!e.Cancel && !ConfirmNavigateAway())
                e.Cancel = true;
        }

        protected override void OnNavigatedTo(
            bool cancelled, NavigationEventArgs e)
        {
            if (cancelled)
                return;

            if (_binding != null)
            {
                _binding.Save();

                UpdateNotes();

                if (!String.IsNullOrEmpty(_binding.Password))
                {
                    txtPassword.Text = _binding.Password;
                }

                return;
            }

            DateTime convertedDate;
            if ((Cache.DbInfo != null) && (Cache.DbInfo.Details.Modified != null))
            {
                convertedDate = DateTime.Parse(Cache.DbInfo.Details.Modified);
                ApplicationTitle.Text = "WinPass - " + Cache.DbInfo.Details.Name + " (" + convertedDate + ")";
            }

            var database = Cache.Database;
            if (database == null)
            {
                this.BackToDBs();
                return;
            }

            string id;
            var queries = NavigationContext.QueryString;
            _fields = new ObservableCollection<FieldBinding>();
            Entry entry;
            if (queries.TryGetValue("id", out id))
            {
                entry = database.GetEntry(id);

                ThreadPool.QueueUserWorkItem(
                    _ => Cache.AddRecent(id));

                // Notes
                if (!String.IsNullOrEmpty(entry.Notes))
                    txtNotes.Text = entry.Notes;

                // Fields
                _fields = new ObservableCollection
                    <FieldBinding>(entry.CustomFields
                        .Select(x => new FieldBinding(x)));


            }
            else
            {
                var config = database.Configuration;

                entry = new Entry
                {
                    Password = Generator
                        .CharacterSets.NewEntry(),
                    UserName = config.DefaultUserName,
                    Protections =
                        {
                            Title = config.ProtectTitle,
                            UserName = config.ProtectUserName,
                            Password = config.ProtectPassword,
                        }
                };

                txtTitle.Loaded += (sender, e1) =>
                    txtTitle.Focus();
            }
            lstFields.ItemsSource = _fields;
            DisplayEntry(entry);
        }

        private bool ConfirmNavigateAway()
        {
            if (!_binding.HasChanges)
                return true;

            var confirm = MessageBox.Show(
                Properties.Resources.UnsavedChange,
                Properties.Resources.UnsavedChangeTitle,
                MessageBoxButton.OKCancel);

            if (confirm != MessageBoxResult.OK)
                return false;

            if (!_entry.IsNew())
            {
                DataContext = null;
                _entry.Reset();
            }

            return true;
        }

        private void DisplayEntry(Entry entry)
        {
            _entry = entry;

            var config = entry.Protections;
            txtTitle.IsProtected = config.Title;
            txtPassword.IsProtected = config.Password;
            txtUsername.IsProtected = config.UserName;

            _binding = new EntryBinding(entry);
            _binding.HasChangesChanged += _binding_HasChangesChanged;
            _binding.HasChanges = entry.IsNew();

            CurrentEntry.Entry = _binding;
            _binding.HasChanges = entry.IsNew();

            UpdateNotes();
            DataContext = _binding;
        }

        private string GetUrl()
        {
            return _entry.GetNavigateUrl(txtUrl.Text);
        }
        private FieldBinding GetAnyDuplicateField()
        {
            return _fields.GroupBy(p => p.Name).SelectMany(f => f.Skip(1)).FirstOrDefault();
        }
        private bool IsReservedName()
        {
            return _fields.Any(p => !Entry.IsSafeNameForField(p.Name));
        }
        private void OpenUrl(bool useIntegreatedBrowser)
        {
            var url = GetUrl();
            if (string.IsNullOrEmpty(url))
                return;

            if (useIntegreatedBrowser)
            {
                this.NavigateTo<WebView>(
                    "url={0}", url);

                return;
            }
            Uri uri;
            if (Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                new WebBrowserTask
                {
                    Uri = uri,
                }.Show();
            }
            else
            {
                MessageBox.Show(Strings.Error_BadURL);
                NavigationService.GoBack();
            }
        }

        private void Save()
        {
            progBusy.IsBusy = true;
            string errorMessage = string.Empty;
            string groupId;
            if (!NavigationContext.QueryString
                .TryGetValue("group", out groupId))
            {
                groupId = null;
            }

            if (IsReservedName())
            {
                errorMessage = Strings.EntryDetails_NameReserved;
            }

            var duplicate = GetAnyDuplicateField();
            if (duplicate != null)
                errorMessage = string.Format(Strings.EntryDetails_NameDuplicate, duplicate.Name);

            if (!string.IsNullOrEmpty(errorMessage))
            {
                MessageBox.Show(errorMessage, "Warning", MessageBoxButton.OK);
                progBusy.IsBusy = false;
                return;
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                var info = Cache.DbInfo;
                var database = Cache.Database;
                var writer = new DatabaseWriter();

                info.OpenDatabaseFile(x => writer
                    .LoadExisting(x, info.Data.MasterKey));

                foreach (var fild in _fields)
                {
                    _entry.Add(new Field() { Name = fild.Name, Value = fild.Value, Protected = fild.Protected });
                }
                if (_entry.ID != null)
                {
                    _binding.Save();
                    writer.Details(_entry);
                }
                else
                {
                    database.AddNew(
                        _entry, groupId);

                    writer.New(_entry);
                }

                info.SetDatabase(x => writer.CreateRecycleBin(
                    x, database.RecycleBin));

                Dispatcher.BeginInvoke(() =>
                {
                    UpdateNotes();
                    progBusy.IsBusy = false;
                    _binding.HasChanges = false;

                    if (!info.NotifyIfNotSyncable())
                    {
                        new ToastPrompt
                        {
                            Title = Properties.Resources.SavedTitle,
                            Message = Properties.Resources.SavedCaption,
                            TextOrientation = System.Windows.Controls
                                .Orientation.Vertical,
                        }.Show();
                    }
                });

                ThreadPool.QueueUserWorkItem(
                    __ => Cache.AddRecent(_entry.ID));
            });
        }

        private void UpdateNotes()
        {
            var notes = _binding.Notes;

            if (!string.IsNullOrEmpty(notes))
            {
                notes = notes
                    .Replace(Environment.NewLine, " ")
                    .TrimStart();

                if (notes.Length > 60)
                {
                    notes = notes
                        .Substring(0, 55)
                        .TrimEnd() + "...";
                }
            }
            else
            {
                notes = Properties
                    .Resources.AddNotes;
            }

            //lnkNotes.Content = notes;
        }

        private void _binding_HasChangesChanged(
            object sender, EventArgs e)
        {
            var hasChanges = _binding.HasChanges;

            _cmdSave.IsEnabled = hasChanges;
            _cmdReset.IsEnabled = hasChanges;
        }

        private void cmdHome_Click(object sender, EventArgs e)
        {
            if (ConfirmNavigateAway())
                this.BackToRoot();
        }

        private void cmdSync_Click(object sender, EventArgs e)
        {
            var dbfolder = Cache.DbInfo.Folder;
            this.NavigateTo<MainPage>("db={0}&sync=1", dbfolder);
        }

        private void cmdPassGen_Click(object sender, EventArgs e)
        {
            this.NavigateTo<PassGen>();
        }

        private void cmdReset_Click(object sender, EventArgs e)
        {
            _binding.Reset();

            DataContext = null;
            DataContext = _binding;

            UpdateNotes();
        }

        private void cmdSave_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void lnkUrl_Click(object sender, RoutedEventArgs e)
        {
            var settings = AppSettings.Instance;
            OpenUrl(settings.UseIntBrowser);
        }

        private void mnuShowMasked_Click(object sender, EventArgs e)
        {
            // Get all the items that have isProtected property and remove the protection
            var protectedControls = this.GetLogicalChildrenByType<ProtectedTextBox>(false);
            if (protectedControls != null)
                foreach (var protectedControl in protectedControls)
                {
                    protectedControl.IsProtected = false;
                }
        }

        private void mnuAbout_Click(object sender, EventArgs e)
        {
            this.NavigateTo<Settings>("page=1");
        }

        private void mnuBrowser_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(false);
        }

        private void mnuIntegrated_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(true);
        }

        private void mnuRoot_Click(object sender, EventArgs e)
        {
            if (ConfirmNavigateAway())
                this.BackToDBs();
        }

        private void txtUrl_Changed(object sender, TextChangedEventArgs e)
        {
            var url = GetUrl();
            lnkUrl.Content = url;

            lnkUrl.IsEnabled = UrlUtils
                .IsValidUrl(url);
        }

        private void txt_GotFocus(object sender, RoutedEventArgs e)
        {
            var txt = sender as TextBox;

            if (txt != null)
            {
                txt.SelectAll();
                return;
            }

            var protect = sender as ProtectedTextBox;
            if (protect != null)
            {
                protect.SelectAll();
            }
        }

        private void UNCopy_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(txtUsername.Text);
            VibrateController.Default.Start(TimeSpan.FromMilliseconds(30));
        }

        private void PWCopy_Click(object sender, EventArgs e)
        {
            Encoding utf8 = Encoding.UTF8;
            var pwlength = txtPassword.Text.Length + 1;
            byte[] bytes = utf8.GetBytes(txtPassword.Text + "\0");
            string content = utf8.GetString(bytes, 0, bytes.Length);
            Clipboard.SetText(content);
            VibrateController.Default.Start(TimeSpan.FromMilliseconds(30));
        }

        private void txtName_Changed(object sender, TextChangedEventArgs e)
        {
            var txtName = (PhoneTextBox)sender;
            var field = (FieldBinding)txtName.Tag;

            if (txtName.Text != field.Name)
                _binding.HasChanges = true;
        }

        private void txtName_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!e.IsEnter())
                return;

            var txtName = (PhoneTextBox)sender;
            var field = (FieldBinding)txtName.Tag;
            field.IsEditing = false;
        }

        private void txtName_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var txtName = (PhoneTextBox)sender;
            if (txtName.Visibility != Visibility.Visible)
                return;

            txtName.Focus();
            txtName.SelectAll();
        }

        private void txtName_ActionIconTapped(object sender, EventArgs e)
        {
            var element = (FrameworkElement)sender;
            while (element != null && !(element is PhoneTextBox))
            {
                element = VisualTreeHelper
                    .GetParent(element) as FrameworkElement;
            }

            if (element == null)
                return;

            var txtName = (PhoneTextBox)element;
            var field = (FieldBinding)txtName.Tag;
            field.IsEditing = false;
        }

        private void cmdRename_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var cmdRename = (RoundButton)sender;
            var field = (FieldBinding)cmdRename.Tag;
            field.IsEditing = true;
        }

        private void txtField_GotFocus(object sender, RoutedEventArgs e)
        {
            var txtField = (ProtectedTextBox)sender;
            txtField.SelectAll();
        }

        private void txtNotes_Changed(object sender, TextChangedEventArgs e)
        {
            if (txtNotes.Text == _binding.Notes)
                return;

            _binding.HasChanges = true;
            _entry.Notes = txtNotes.Text;
        }

        private void cmdAdd_Click(object sender, EventArgs e)
        {
            _binding.HasChanges = true;

            var field = _binding.AddField();
            var binding = new FieldBinding(field);
            _fields.Add(binding);
            lstFields.UpdateLayout();
            lstFields.ScrollIntoView(binding);

            Dispatcher.BeginInvoke(() =>
                binding.IsEditing = true);
        }

        private void cmdUNCopy_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(txtUsername.Text);
            VibrateController.Default.Start(TimeSpan.FromMilliseconds(30));
        }

        private void mnuSettings_Click(object sender, EventArgs e)
        {
            this.NavigateTo<Settings>("page=0");
        }

        private void txtField_TextChanged(object arg1, string arg2)
        {
            var txtField = (ProtectedTextBox)arg1;
            var field = (FieldBinding)txtField.Tag;

            if (field == null || field.Value == txtField.Text)
                return;

            _binding.HasChanges = true;
            field.Value = txtField.Text;
        }

        //private void TextBlock_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        //{
        //    Clipboard.SetText(((TextBlock)sender).Text);
        //    VibrateController.Default.Start(TimeSpan.FromMilliseconds(30));
        //}
    }
}
