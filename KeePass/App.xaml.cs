using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using KeePass.Storage;
using KeePass.Utils;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using KeePass.Sources;
using Windows.ApplicationModel.Activation;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;
using KeePass.IO.Exceptions;

namespace KeePass
{
    public partial class App
    {
        private bool _initialized;

        public new static App Current
        {
            get { return (App)Application.Current; }
        }
        public PhoneApplicationFrame RootFrame { get; private set; }
        public Queue<FileOpenPickerContinuationEventArgs> QueueFileOpenPickerArgs { get; set; }

        public App()
        {
            UnhandledException += Application_UnhandledException;
            QueueFileOpenPickerArgs = new Queue<FileOpenPickerContinuationEventArgs>();
            if (Debugger.IsAttached)
                Host.Settings.EnableFrameRateCounter = true;

            InitializeComponent();

            InitializePhoneApplication();

            // Show graphics profiling information while debugging.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Display the current frame rate counters.
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Display the metro grid helper.
                MetroGridHelper.IsVisible = true;

                // Enable non-production analysis visualization mode, 
                // which shows areas of a page that are handed off to GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Disable the application idle detection by setting the UserIdleDetectionMode property of the
                // application's PhoneApplicationService object to Disabled.
                // Caution:- Use this under debug mode only. Application that disable user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }
        }

        private void Application_ContractActivated(object sender, IActivatedEventArgs e)
        {
            var filePickerContinuationArgs = e as FileOpenPickerContinuationEventArgs;
            if (filePickerContinuationArgs != null)
            {
                QueueFileOpenPickerArgs.Enqueue(filePickerContinuationArgs);
            }
        }

        private void InitializePhoneApplication()
        {
            if (_initialized)
                return;

            RootFrame = new Delay.HybridOrientationChangesFrame
            {
                Duration = TimeSpan.FromSeconds(0.6)
            };
            if (AppSettings.Instance.Language == null)
                AppSettings.Instance.Language = Thread.CurrentThread.CurrentCulture.Name;
            CultureInfo newCulture = new CultureInfo(AppSettings.Instance.Language);
            Thread.CurrentThread.CurrentCulture = newCulture;
            Thread.CurrentThread.CurrentUICulture = newCulture;

            RootFrame.Navigated += CompleteInitializePhoneApplication;
            RootFrame.UriMapper = new AssociationUriMapper();
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            PhoneApplicationService.Current.ContractActivated += Application_ContractActivated;

            Cache.Initialize();
            _initialized = true;
        }



        private void Application_Activated(
            object sender, ActivatedEventArgs e)
        {
            if (e.IsApplicationInstancePreserved)
            {
                var global = GlobalPassHandler.Instance;
                global.Reset();

                if (global.ShouldPromptGlobalPass)
                {
                    var root = RootFrame;
                    root.Dispatcher.BeginInvoke(() =>
                        root.Navigate(Navigation.GetPathTo
                            <GlobalPassVerify>()));
                }

                return;
            }

            ThemeData.Initialize();
            Cache.RestoreCache(RootFrame.Dispatcher);
        }

        private void Application_Closing(
            object sender, ClosingEventArgs e)
        { }

        private void Application_Deactivated(
            object sender, DeactivatedEventArgs e)
        { }

        private void Application_Launching(
            object sender, LaunchingEventArgs e)
        {
            ThemeData.Initialize();
        }

        private void Application_UnhandledException(
            object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject;

            e.Handled = true;

            RootFrame.Dispatcher.BeginInvoke(() =>
            {
                if (!Debugger.IsAttached)
                {
                    if (ex.GetType() == typeof(DateTimeFormatException))
                    {
                        MessageBox.Show(
                        Properties.Resources.CorruptedFile,
                        Properties.Resources.UnhandledExTitle, MessageBoxButton.OK);
                        return;
                    }
                    var response = MessageBox.Show(
                        Properties.Resources.UnhandledExPrompt,
                        Properties.Resources.UnhandledExTitle,
                        MessageBoxButton.OKCancel);

                    if (response == MessageBoxResult.OK)
                        ErrorReport.Report(ex);
                }
                else
                    Debugger.Break();
            });
        }

        private void CompleteInitializePhoneApplication(
            object sender, NavigationEventArgs e)
        {
            RootVisual = RootFrame;
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        private static void RootFrame_NavigationFailed(
            object sender, NavigationFailedEventArgs e)
        {
            if (Debugger.IsAttached)
                Debugger.Break();
        }
    }
}