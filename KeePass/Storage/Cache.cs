using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using KeePass.IO.Data;
using Microsoft.Phone.Wallet;

namespace KeePass.Storage
{
    internal static class Cache
    {
        private const string KEY_DATABASE = "Database";
        public const string KEY_IMAGESTYLE_IN_Classic = "ImageStyle";
        private const string ClassicImagePath = "/Images/KeePass/Classic/{0:00}.png";
        private const string ModernImagePath = "/Images/KeePass/Modern/{1}/{0:00}.png";
        private static readonly IsolatedStorageSettings _appSettings;
        private static readonly object _lckStandards;
        private static readonly Dictionary<int, ImageSource> _standards;
        private static readonly string _theamPrefix;
        private static string _imagePath;
        private static DatabaseInfo _info;
        private static Queue<object> _lastMovedItems { set; get; }

        public static Queue<object> LastMovedItems
        {
            get
            {
                if (_lastMovedItems == null)
                    _lastMovedItems = new Queue<object>();
                return _lastMovedItems;
            }
        }

        public static bool InClassicStyle()
        {
            return _imagePath == ClassicImagePath;
        }
        public static void InvertStyle()
        {

            _imagePath = InClassicStyle() ? ModernImagePath : ClassicImagePath;
            _appSettings[KEY_IMAGESTYLE_IN_Classic] = InClassicStyle();
            ResetCache();
        }
        /// <summary>
        /// Gets the database.
        /// </summary>
        /// <value>
        /// The database.
        /// </value>
        public static Database Database { get; private set; }

        /// <summary>
        /// Gets the detailed information for <see cref="Database"/>.
        /// </summary>
        public static DatabaseInfo DbInfo
        {
            get { return _info; }
        }

        static Cache()
        {

            _lckStandards = new object();
            _appSettings = IsolatedStorageSettings
                .ApplicationSettings;
            _standards = new Dictionary<int, ImageSource>();

            var v = (Visibility)Application.Current
                   .Resources["PhoneLightThemeVisibility"];
            _theamPrefix = v != Visibility.Visible ? "dark" : "light";

        }
        public static void Initialize()
        {
            if (!_appSettings.Contains(KEY_IMAGESTYLE_IN_Classic))
            {
                _appSettings[KEY_IMAGESTYLE_IN_Classic] = false;// default style now moddern
            }
            var res = (_appSettings[KEY_IMAGESTYLE_IN_Classic] as bool?);
            if (res.HasValue)
                ResetCache();

        }
        /// <summary>
        /// Adds the specified entry id to the recently viewed entries list.
        /// </summary>
        /// <param name="entryId">The entry id.</param>
        public static void AddRecent(string entryId)
        {
            var recents = _info.Details.Recents;

            recents.Remove(entryId);
            recents.Insert(0, entryId);

            if (recents.Count == 10)
                recents.RemoveAt(9);

            _info.SaveDetails();
        }

        public static void CacheDb(DatabaseInfo info,
            string name, Database database)
        {
            _info = info;
            _standards.Clear();
            Database = database;

            _appSettings[KEY_DATABASE] = name;
            _appSettings.Save();
        }

        public static void Clear()
        {
            Database = null;
            _standards.Clear();

            _appSettings.Remove(KEY_DATABASE);
            _appSettings.Save();
        }

        /// <summary>
        /// Clears the recently viewed entries list.
        /// </summary>
        public static void ClearRecents()
        {
            _info.Details.Recents.Clear();
            _info.SaveDetails();
        }

        /// <summary>
        /// Gets the overlay icon.
        /// </summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <param name="icon">The icon information.</param>
        /// <returns>
        /// The overlay icon.
        /// </returns>
        public static ImageSource GetOverlay(
            Dispatcher dispatcher, IconData icon)
        {
            if (icon == null)
                throw new ArgumentNullException("icon");
            ImageSource source;

            if (!string.IsNullOrEmpty(icon.Custom) &&
                Database.Icons.TryGetValue(icon.Custom, out source))
                return source;

            var id = icon.Standard;
            if (!_standards.TryGetValue(id, out source))
            {

                lock (_lckStandards)
                {
                    if (!_standards.TryGetValue(id, out source))
                    {
                        var wait = new ManualResetEvent(false);


                        dispatcher.BeginInvoke(() =>
                        {
                            //  "/Images/KeePass/classic/{0:00}.png", id);
                            var uri = string.Format(_imagePath, id, _theamPrefix);
                            source = new BitmapImage(new Uri(
                                uri, UriKind.Relative));
                            _standards.Add(id, source);

                            wait.Set();
                        });

                        wait.WaitOne();
                    }
                }
            }

            return source;
        }

        public static void ResetCache()
        {
            var item = (_appSettings[KEY_IMAGESTYLE_IN_Classic] as bool?) ?? false;
            _imagePath = item ? ClassicImagePath : ModernImagePath;
            if (_standards != null)
                _standards.Clear();
        }

        /// <summary>
        /// Gets the recently viewed entries.
        /// </summary>
        /// <returns></returns>
        public static string[] GetRecents()
        {
            return _info.Details
                .Recents.ToArray();
        }

        public static void RestoreCache(Dispatcher dispatcher)
        {
            if (dispatcher == null)
                throw new ArgumentNullException("dispatcher");

            string name;
            if (!_appSettings.TryGetValue(KEY_DATABASE, out name) ||
                string.IsNullOrEmpty(name))
                return;

            var info = new DatabaseInfo(name);
            if (!info.HasPassword)
                return;

            info.Open(dispatcher);
        }

        /// <summary>
        /// Updates the recents.
        /// </summary>
        public static void UpdateRecents()
        {
            var database = Database;
            var recents = _info.Details.Recents;

            var removed = recents
                .Where(x => database.GetEntry(x) == null)
                .ToArray();

            foreach (var entry in removed)
                recents.Remove(entry);

            _info.SaveDetails();
        }
    }
}