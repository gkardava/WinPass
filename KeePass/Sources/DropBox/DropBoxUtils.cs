using System;
using Dropbox.Api;
using KeePass.Utils;

namespace KeePass.Sources.DropBox
{
    internal class DropboxClientWrapper
    {
        internal DropboxClientWrapper(string token)
        {
            Token = token;
            Client = new DropboxClient(token);
        }

        internal DropboxClient Client { get; }
        internal string Token { get; }
    }

    internal static class DropBoxUtils
    {
        public static bool UpdateAuth(ref string token)
        {
            if (token.Contains(":"))
            {
                var auth = new DropboxAppClient(
                    ApiKeys.DROPBOX_KEY,
                    ApiKeys.DROPBOX_SECRET);
                var oldToken = token.Split(new[] { ':' }, 2);
                var auth2 = auth.Auth.TokenFromOauth1Async(oldToken[0], oldToken[1]);
                auth2.RunSynchronously();
                token = auth2.Result.Oauth2Token;
                return true;
            }
            return false;
        }

        public static DropboxClientWrapper Create(string token)
        {
            return new DropboxClientWrapper(token);
        }

        public static string GetUrl(
            this DropboxClientWrapper client,
            string path)
        {
            return string.Format(
                "dropbox://{0}@{1}",
                client.Token, path);
        }

        public static string RenderUrl(string uri)
        {
            var parts = uri.Split(new[] { '@' }, 2);
            return parts.Length == 2 ? parts[1] : parts[0];
        }

        public static async void CallAsyncAndDispose<T>(Func<System.Threading.Tasks.Task<T>> method, Action<T> onSuccess, Action<DropboxException> onError, IDisposable dispose)
        {
            using (dispose)
            {
                await CallAsyncImpl(method, onSuccess, onError);
            }
        }

        public static async void CallAsync<T>(Func<System.Threading.Tasks.Task<T>> method, Action<T> onSuccess, Action<DropboxException> onError)
        {
            await CallAsyncImpl(method, onSuccess, onError);
        }

        public static async System.Threading.Tasks.Task CallAsyncImpl<T>(Func<System.Threading.Tasks.Task<T>> method, Action<T> onSuccess, Action<DropboxException> onError)
        {
            var task = method();
            try
            {
                var result = await task;
                onSuccess(result);
            }
            catch
            {
                if (task.Exception != null)
                {
                    foreach (var ex in task.Exception.InnerExceptions)
                    {
                        if (ex is DropboxException)
                        {
                            onError(ex as DropboxException);
                            return;
                        }
                    }
                    throw new ApplicationException("Unexpected exception. See InnerException for details.", task.Exception);
                }
                throw;
            }
        }
    }
}