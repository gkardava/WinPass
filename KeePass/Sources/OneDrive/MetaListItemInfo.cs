using System;
using System.Globalization;
using System.Xml.Linq;
using KeePass.Data;
using KeePass.Utils;

namespace KeePass.Sources.OneDrive
{
    internal class MetaListItemInfo : ListItemInfo
    {
        private readonly bool _isDir;
        private readonly string _modified;
        private readonly string _parent;
        private readonly string _path;
        private readonly int _size;

        public bool IsDir
        {
            get { return _isDir; }
        }

        public string Modified
        {
            get { return _modified; }
        }

        public string Parent
        {
            get { return _parent; }
        }

        public string Path
        {
            get { return _path; }
        }

        public int Size
        {
            get { return _size; }
        }

        public MetaListItemInfo(XElement node)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            _path = node.GetValue("id");
            _parent = node.GetValue("parent_id");
            _modified = node.GetValue("updated_time");
            var type = node.GetValue("type");
            if(string.IsNullOrEmpty(type))
            {
                type = "file";
            }
            _isDir = "folder|album".Contains(type); // Show folder icon in case of folder/album
            int.TryParse(node.GetValue("size"), out _size);

            Title = node.GetValue("name");
            Notes = GetRelativeTime(_modified);
            string iconStr = "";
            iconStr = Title.EndsWith(".kdbx",StringComparison.InvariantCultureIgnoreCase) // If its keepass database
                ? "keepasslogo"
                : (_isDir
                    ? "folder"
                    : "entry");

            Icon = ThemeData.GetImage(iconStr);
        }

        public MetaListItemInfo AsParent()
        {
            var clone = (MetaListItemInfo)
                MemberwiseClone();
            clone.Title = "..";

            return clone;
        }

        private static string GetRelativeTime(string time)
        {
            try
            {
                var date = DateTime.ParseExact(time,
                    "yyyy-MM-ddTHH:mm:sszzzz",
                    CultureInfo.InvariantCulture);

                return date.ToRelative();
            }
            catch
            {
                return time;
            }
        }
    }
}