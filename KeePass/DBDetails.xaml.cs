using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Shell;

using KeePass.I18n;
using KeePass.Storage;
using KeePass.Utils;
using KeePass.Sources;

using System.Globalization;

namespace KeePass
{
    public partial class DBDetails
    {
        private DatabaseInfo _database;
        private string _originalName;
        private string _originalURL;
        private string _originalUser;
        private string _originalPW;
        private string _originalDomain;

        ApplicationBarIconButton btnSave;
        ApplicationBarIconButton btnClose;
        ApplicationBarIconButton btnDelete;

        ApplicationBarMenuItem mnuDlKeyFile;
        ApplicationBarMenuItem mnuDelKeyFile;
        ApplicationBarMenuItem mnuClearPW;

        public DBDetails()
        {
            InitializeComponent();

            btnSave = ApplicationBar.Buttons[0] as ApplicationBarIconButton;
            btnClose = ApplicationBar.Buttons[1] as ApplicationBarIconButton;
            btnDelete = ApplicationBar.Buttons[2] as ApplicationBarIconButton;
            btnSave.Text = Strings.EntryDetails_SaveEntry;
            btnClose.Text = Strings.EntryDetails_ResetEntry;
            btnDelete.Text = Strings.GroupDetails_Delete;

            mnuClearPW = ApplicationBar.MenuItems[0] as ApplicationBarMenuItem;
            mnuDlKeyFile = ApplicationBar.MenuItems[1] as ApplicationBarMenuItem;
            mnuDelKeyFile = ApplicationBar.MenuItems[2] as ApplicationBarMenuItem;
            mnuDlKeyFile.Text = Strings.MainPage_DownloadKeyfile;
            mnuDelKeyFile.Text = Strings.MainPage_ClearKeyfile;
            mnuClearPW.Text = Strings.MainPage_ClearPassword;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _database = new DatabaseInfo(NavigationContext.QueryString["db"]);
            _database.LoadDetails();

            _originalName = _database.Details.Name;

            DateTime convertedDate;
            convertedDate = DateTime.Now;

            txtName.Text = _originalName;
            var urlstr = _database.Details.Url;

            var locCH = "";
            if (_database.Details.HasLocalChanges)
                locCH = "\n " + Strings.DBDetail_LocalCH;

            mnuDelKeyFile.IsEnabled = _database.HasKeyFile;
            mnuClearPW.IsEnabled = _database.HasPassword;

            switch (_database.Details.Source)
            {
                case "Demo":
                    lblSource.Text = _database.Details.Source + locCH;

                    txtURL.Visibility = Visibility.Collapsed;
                    txtURLUser.Visibility = Visibility.Collapsed;
                    txtURLPW.Visibility = Visibility.Collapsed;
                    txtDomain.Visibility = Visibility.Collapsed;
                    break;
                case "Web":
                    urlstr = _database.Details.Url;
                    lblSource.Text = _database.Details.Source + ", " + Strings.DBDetail_Updated + locCH;
                    string[] urlarr = urlstr.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    string[] domain = urlarr[0].Split(new string[] { "\":\"" }, StringSplitOptions.RemoveEmptyEntries);
                    string[] pwd = urlarr[1].Split(new string[] { "\":\"" }, StringSplitOptions.RemoveEmptyEntries);
                    string[] url = urlarr[2].Split(new string[] { "\":\"" }, StringSplitOptions.RemoveEmptyEntries);
                    string[] user = urlarr[3].Split(new string[] { "\":\"" }, StringSplitOptions.RemoveEmptyEntries);


                    txtURL.Text = url[1].Replace("\"", string.Empty);
                    _originalURL = txtURL.Text;

                    if (user.Length > 1)
                    {
                        txtURLUser.Text = user[1].Replace("\"", string.Empty).Replace("}", string.Empty);
                        _originalUser = txtURLUser.Text;
                    }
                    else
                    {
                        txtURLUser.Visibility = Visibility.Collapsed;
                    }

                    if (pwd.Length > 1)
                    {
                        txtURLPW.Password = pwd[1].Replace("\"", string.Empty);
                        _originalPW = txtURLPW.Password;
                    }
                    else
                    {
                        txtURLPW.Visibility = Visibility.Collapsed;
                    }

                    if (domain.Length > 1)
                    {
                        txtDomain.Text = domain[1].Replace("\"", string.Empty);
                        _originalDomain = txtDomain.Text;
                    }
                    else
                    {
                        txtDomain.Visibility = Visibility.Collapsed;
                    }
                    break;
                case "DropBox":
                    convertedDate = DateTime.Parse(_database.Details.Modified);
                    lblSource.Text = _database.Details.Source + ", " + convertedDate.ToLocalTime() + locCH;
                    txtURL.Text = _database.Details.Url;
                    _originalURL = _database.Details.Url;

                    txtURLUser.Visibility = Visibility.Collapsed;
                    txtURLPW.Visibility = Visibility.Collapsed;
                    txtDomain.Visibility = Visibility.Collapsed;
                    break;
                case "OneDrive":
                    convertedDate = DateTime.Parse(_database.Details.Modified);
                    lblSource.Text = _database.Details.Source + ", " + convertedDate.ToLocalTime() + locCH;

                    txtURL.Visibility = Visibility.Collapsed;
                    txtURLUser.Visibility = Visibility.Collapsed;
                    txtURLPW.Visibility = Visibility.Collapsed;
                    txtDomain.Visibility = Visibility.Collapsed;
                    break;
                case "WebDAV":
                    convertedDate = DateTime.Parse(_database.Details.Modified);
                    //lblModified.Text = _database.Details.Modified;
                    lblSource.Text = _database.Details.Source + ", " + convertedDate.ToLocalTime() + locCH;
                    txtName.Text = _originalName;
                    string[] urlarr2 = urlstr.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    {
                        txtURL.Text = urlarr2[0];
                        _originalURL = txtURL.Text;

                        txtURLUser.Text = urlarr2[1];
                        _originalUser = txtURLUser.Text;

                        txtURLPW.Password = urlarr2[2];
                        _originalPW = txtURLPW.Password;
                    }
                    txtDomain.Visibility = Visibility.Collapsed;
                    break;
                default:
                    lblSource.Text = Strings.Download_LocalFile + locCH;

                    txtURL.Visibility = Visibility.Collapsed;
                    txtURLUser.Visibility = Visibility.Collapsed;
                    txtURLPW.Visibility = Visibility.Collapsed;
                    txtDomain.Visibility = Visibility.Collapsed;
                    return;
            }
        }

        private void cmdSave_Click(object sender, EventArgs e)
        {
            if (txtName.Text != _originalName)
            {
                PerformRenameLocal();
            }
            var url = "";

            switch (_database.Details.Source)
            {
                case "Web":
                    url = "{\"Domain\":\"" + txtDomain.Text + "\",\"Password\":\"" + txtURLPW.Password + "\",\"Url\":\"" + txtURL.Text + "\",\"User\":\"" + txtURLUser.Text + "\"}";
                    break;
                case "DropBox":
                    url = txtURL.Text;
                    break;
                case "WebDAV":
                    url = txtURL.Text + "\n" + txtURLUser.Text + "\n" + txtURLPW.Password;
                    break;
                default:
                    return;
            }

            _database.Details.Url = url;
            _database.SaveDetails();
            NavigationService.GoBack();
        }

        private void cmdClear_Click(object sender, EventArgs e)
        {
            PerformClear();
        }

        private void PerformRenameLocal()
        {
            _database.Details.Name = txtName.Text;
            _database.SaveDetails();

            TilesManager.Renamed(_database);
            NavigationService.GoBack();
        }

        private void PerformClear()
        {
            txtName.Text = _originalName;
        }

        private void lklName_ch(object sender, TextChangedEventArgs e)
        {
            if (txtName.Text != _originalName)
            {
                btnSave.IsEnabled = true;
                btnClose.IsEnabled = true;
            }
            else
            {
                btnSave.IsEnabled = false;
                btnClose.IsEnabled = false;
            }
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            var msg = string.Format(
                Properties.Resources.ConfirmDeleteDb,
                _database.Details.Name);

            var confirm = MessageBox.Show(msg,
                Properties.Resources.DeleteDbTitle,
                MessageBoxButton.OKCancel) == MessageBoxResult.OK;

            if (!confirm)
                return;

            _database.Delete();
            TilesManager.Deleted(_database);

            this.NavigateTo<MainPage>();
        }

        private void mnuKeyFile_Click(object sender, EventArgs e)
        {
            this.NavigateTo<Download>(
                "type=key&folder={0}", _database.Folder);
        }

        private void mnuClearKeyFile_Click(object sender, EventArgs e)
        {
            _database.SetKeyFile(null);
        }

        private void url_ch(object sender, TextChangedEventArgs e)
        {
            if (txtURL.Text != _originalURL)
            {
                btnSave.IsEnabled = true;
                btnClose.IsEnabled = true;
            }
            else
            {
                btnSave.IsEnabled = false;
                btnClose.IsEnabled = false;
            }
        }

        private void user_ch(object sender, TextChangedEventArgs e)
        {
            if (txtURLUser.Text != _originalUser)
            {
                btnSave.IsEnabled = true;
                btnClose.IsEnabled = true;
            }
            else
            {
                btnSave.IsEnabled = false;
                btnClose.IsEnabled = false;
            }
        }

        private void pw_ch(object sender, RoutedEventArgs e)
        {
            if (txtURLPW.Password != _originalPW)
            {
                btnSave.IsEnabled = true;
                btnClose.IsEnabled = true;
            }
            else
            {
                btnSave.IsEnabled = false;
                btnClose.IsEnabled = false;
            }
        }

        private void domain_ch(object sender, TextChangedEventArgs e)
        {
            if (txtDomain.Text != _originalDomain)
            {
                btnSave.IsEnabled = true;
                btnClose.IsEnabled = true;
            }
            else
            {
                btnSave.IsEnabled = false;
                btnClose.IsEnabled = false;
            }
        }

        private void mnuClear_Click(object sender, EventArgs e)
        {
            //var item = (MenuItem)sender;
            //var database = (DatabaseInfo)item.Tag;

            _database.ClearPassword();
            _database.SaveDetails();

            //var listItem = _items.First(x => x.Info == database);

            //listItem.HasPassword = false;
            //listItem.PasswordIcon = null;
        }
    }
}