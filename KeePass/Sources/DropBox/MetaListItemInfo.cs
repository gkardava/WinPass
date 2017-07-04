using System;
using System.Globalization;
using Dropbox.Api.Files;
using KeePass.Data;
using KeePass.Utils;

namespace KeePass.Sources.DropBox
{
    internal class MetaListItemInfo : ListItemInfo, IListItem
    {
        private readonly bool _isDir;
        private readonly DateTime _modified;
        private readonly string _path;
        private readonly long _size;

        public bool IsDir
        {
            get { return _isDir; }
        }

        public DateTime Modified
        {
            get { return _modified; }
        }

        public string Path
        {
            get { return _path; }
        }

        public long Size
        {
            get { return _size; }
        }

        public MetaListItemInfo(Metadata data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            _path = data.PathDisplay; // PathLower?
            _size = data.IsFile ? (long)data.AsFile.Size : 0;
            _isDir = data.IsFolder;
            _modified = data.IsFile ? data.AsFile.ClientModified : DateTime.Now;

            Title = data.Name;
            Notes = GetRelativeTime(data);
            Icon = ThemeData.GetImage(
                data.IsFolder ? "folder" : "entry");
        }

        public MetaListItemInfo(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            _path = path;
            _isDir = true;
            _modified = DateTime.Now;

            Title = "Parent Folder";
            Icon = ThemeData.GetImage("parent");
        }

        private static string GetRelativeTime(Metadata data)
        {
            return data.IsFile
                ? data.AsFile.ClientModified.ToRelative()
                : string.Empty;
        }
    }
}