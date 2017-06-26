using System;
using System.IO;
using Dropbox.Api;
using Dropbox.Api.Files;
using KeePass.IO.Utils;
using KeePass.Storage;

namespace KeePass.Sources.DropBox
{
    internal class DropBoxAdapter : ServiceAdapterBase
    {
        private DropboxClientWrapper _client;
        private SyncInfo _info;

        public override void Conflict(ListItem item,
            Action<ListItem, string, string> uploaded)
        {
            var path = GetNonConflictPath();

            UploadFileAsync(path, x => uploaded(
                Translate(x), _client.GetUrl(path),
                new Uri(path).LocalPath));
        }

        public override void Download(ListItem item,
            Action<ListItem, byte[]> downloaded)
        {
            DropBoxUtils.CallAsync(
                () => _client.Client.Files.DownloadAsync(DropBoxUtils.RenderUrl(_info.Path)),
                async t => downloaded(item, await t.GetContentAsByteArrayAsync()),
                OnError);
        }

        public override SyncInfo Initialize(DatabaseInfo info)
        {
            if (info == null)
                throw new ArgumentNullException("info");

            var details = info.Details;
            var url = new Uri(details.Url);
            var userInfo = url.UserInfo;
            if (DropBoxUtils.UpdateAuth(ref userInfo))
            {
                var urlBld = new UriBuilder(url);
                urlBld.UserName = userInfo;
                urlBld.Password = null;
                details.Url = urlBld.Uri.ToString();
            }
            _client = CreateClient(userInfo);

            _info = new SyncInfo
            {
                Path = url.LocalPath,
                Modified = details.Modified,
                HasLocalChanges = details.HasLocalChanges,
            };

            info.OpenDatabaseFile(x =>
            {
                using (var buffer = new MemoryStream())
                {
                    BufferEx.CopyStream(x, buffer);
                    _info.Database = buffer.ToArray();
                }
            });

            return _info;
        }

        public override void List(Action<ListItem> ready)
        {
            DropBoxUtils.CallAsync(
                () => _client.Client.Files.GetMetadataAsync(DropBoxUtils.RenderUrl(_info.Path)),
                meta => ready(Translate(meta)),
                OnError);
        }

        public override void Upload(ListItem item,
            Action<ListItem> uploaded)
        {
            UploadFileAsync(_info.Path,
                meta => uploaded(Translate(meta)));
        }

        private static DropboxClientWrapper CreateClient(string userInfo)
        {
            return DropBoxUtils.Create(userInfo);
        }

        private string GetNonConflictPath()
        {
            var path = _info.Path;
            var dir = Path.GetDirectoryName(path);
            var extension = Path.GetExtension(path);
            var fileName = Path.GetFileNameWithoutExtension(path);

            fileName = string.Concat(fileName,
                " (WinPass' conflicted copy ",
                DateTime.Today.ToString("yyyy-MM-dd"),
                ")", extension);

            return Path.Combine(dir, fileName)
                .Replace('\\', '/');
        }

        private static ListItem Translate(Metadata meta)
        {
            return new ListItem
            {
                Tag = meta,
                Timestamp = meta.IsFile ? meta.AsFile.ClientModified.ToString("r") : string.Empty,
            };
        }

        private void UploadFileAsync(string path,
            Action<Metadata> completed)
        {
            var orgPath = path;

            using (var stream = new MemoryStream(_info.Database))
            {
                DropBoxUtils.CallAsyncAndDispose(
                    () => _client.Client.Files.UploadAsync(DropBoxUtils.RenderUrl(path.Replace('\\', '/')), body: stream),
                    x => DropBoxUtils.CallAsync(
                        () => _client.Client.Files.GetMetadataAsync(DropBoxUtils.RenderUrl(orgPath)),
                        completed,
                        OnError),
                    OnError,
                    stream);
            }
        }
    }
}