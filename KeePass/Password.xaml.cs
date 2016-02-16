using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using Windows.Storage.Pickers;
using KeePass.I18n;
using KeePass.Sources;
using KeePass.Storage;
using KeePass.Utils;
using Microsoft.Phone.Shell;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace KeePass
{
    public partial class Password
    {
        private readonly ApplicationBarIconButton _cmdOpen;
        private readonly BackgroundWorker _wkOpen;

        private string _folder;
        private bool _hasKeyFile;

        public Password()
        {
            InitializeComponent();

            _cmdOpen = AppButton(0);
            _cmdOpen.Text = Strings.Password_Open;
            AppButton(1).Text = Strings.Clear;

            _wkOpen = new BackgroundWorker();
            _wkOpen.DoWork += _wkOpen_DoWork;
            _wkOpen.RunWorkerCompleted += _wkOpen_RunWorkerCompleted;


            imgWarning.Source = ThemeData.GetImageSource("information");
            imgWarning.Visibility = GlobalPassHandler.Instance.HasGlobalPass
                ? Visibility.Collapsed : Visibility.Visible;
        }

        protected override void OnNavigatedTo(
            bool cancelled, NavigationEventArgs e)
        {
            if (cancelled)
                return;

            _folder = NavigationContext.QueryString["db"];
            _hasKeyFile = new DatabaseInfo(_folder).HasKeyFile;

            UpdatePasswordStatus();
        }

        private void OpenDatabase()
        {
            progBusy.IsBusy = true;
            progBusy.Focus();

            var savePass = chkStore
                .IsChecked == true;

            _wkOpen.RunWorkerAsync(new OpenArgs
            {
                Folder = _folder,
                Dispatcher = Dispatcher,
                SavePassword = savePass,
                Password = txtPassword.Password,
            });
        }

        private void UpdatePasswordStatus()
        {
            var hasPassword = _hasKeyFile ||
                txtPassword.Password.Length > 0;

            _cmdOpen.IsEnabled = hasPassword;
        }

        private static void _wkOpen_DoWork(
            object sender, DoWorkEventArgs e)
        {
            var args = (OpenArgs)e.Argument;
            var database = new DatabaseInfo(args.Folder);

            e.Result = database.Open(args.Dispatcher,
                args.Password, args.SavePassword);
        }

        private void _wkOpen_RunWorkerCompleted(
            object sender, RunWorkerCompletedEventArgs e)
        {
            progBusy.IsBusy = false;

            if (e.Error != null)
            {
                var sendMail = MessageBox.Show(
                    Properties.Resources.ParseError,
                    Properties.Resources.PasswordTitle,
                    MessageBoxButton.OKCancel);

                if (sendMail == MessageBoxResult.OK)
                    ErrorReport.Report(e.Error);

                return;
            }

            switch ((OpenDbResults)e.Result)
            {
                case OpenDbResults.Success:
                    txtPassword.Password = string.Empty;

                    string fromTile;
                    if (!NavigationContext.QueryString
                        .TryGetValue("fromTile", out fromTile))
                    {
                        this.NavigateTo<GroupDetails>();
                    }
                    else
                    {
                        this.NavigateTo<GroupDetails>(
                            "fromTile={0}", fromTile);
                    }

                    break;

                case OpenDbResults.IncorrectPassword:
                    MessageBox.Show(Properties.Resources.IncorrectPassword,
                        Properties.Resources.PasswordTitle,
                        MessageBoxButton.OK);
                    break;

                case OpenDbResults.CorruptedFile:
                    MessageBox.Show(Properties.Resources.CorruptedFile,
                        Properties.Resources.PasswordTitle,
                        MessageBoxButton.OK);
                    break;
            }
        }

        private void cmdClear_Click(object sender, EventArgs e)
        {
            txtPassword.Password = string.Empty;
        }

        private void cmdOpen_Click(object sender, EventArgs e)
        {
            OpenDatabase();
        }

        private void imgWarning_ManipulationStarted(
            object sender, ManipulationStartedEventArgs e)
        {
            e.Complete();
            e.Handled = true;

            MessageBox.Show(Properties.Resources.WarningStorePassword,
                (string)chkStore.Content, MessageBoxButton.OK);
        }
        
        private void txtPassword_Loaded(
            object sender, RoutedEventArgs e)
        {

            txtPassword.Focus();
            txtPasswordtext_KeyUp(null, null);
        }
        

        private class OpenArgs
        {
            public Dispatcher Dispatcher { get; set; }
            public string Folder { get; set; }
            public string Password { get; set; }
            public bool SavePassword { get; set; }
        }
        private void lnkLocal_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker fileOpenPicker = new FileOpenPicker();
            fileOpenPicker.ContinuationData["Action"] = "KEY";
            fileOpenPicker.FileTypeFilter.Add(".kdbx");
            fileOpenPicker.PickSingleFileAndContinue();
        }

        private void buttonLoadMasterKey_Tap(object sender, GestureEventArgs e)
        {
            this.NavigateTo<Download>("type={0}&folder={1}", "key", _folder);
        }

        private void txtPasswordtext_KeyUp(object sender, KeyEventArgs e)
        {
            txtPassword.Password = txtPasswordtext.Text = (sender is TextBox ? txtPasswordtext.Text : txtPassword.Password);

            UpdatePasswordStatus();

            if (!_cmdOpen.IsEnabled)
                return;

            if (e!=null && e.IsEnter())
                OpenDatabase();
        }

        private void Image_Tap(object sender, GestureEventArgs e)
        {
            if (txtPassword.Visibility == Visibility.Collapsed)
            {
                changeVisibiliteText(txtPasswordtext, txtPassword);
                eyeImage.Source = new BitmapImage(new Uri("/Images/eyeOpen.png", UriKind.Relative));
            }
            else {
                changeVisibiliteText(txtPassword, txtPasswordtext);
                eyeImage.Source = new BitmapImage(new Uri("/Images/eyeClose.png",UriKind.Relative));
                txtPasswordtext.SelectionStart = txtPasswordtext.Text.Length;
            }
        }
        private void changeVisibiliteText(Control from, Control to)
        {
            to.Visibility = Visibility.Visible;
            to.Focus();
            from.Visibility = Visibility.Collapsed;
        }
        
    }
}
