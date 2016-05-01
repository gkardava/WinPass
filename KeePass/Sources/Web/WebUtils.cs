using System;
using System.Net;
using Newtonsoft.Json;
using System.Windows;
using KeePass.I18n;

namespace KeePass.Sources.Web
{
    internal static class WebUtils
    {
        public static NetworkCredential CreateCredentials(
            string user, string password, string domain)
        {
            if (string.IsNullOrEmpty(user) &&
                string.IsNullOrEmpty(password))
                return null;

            var credentials = new NetworkCredential(user, password);

            if (!string.IsNullOrEmpty(domain))
                credentials.Domain = domain;

            return credentials;
        }

        public static WebUrlInfo Deserialize(string info)
        {
            return JsonConvert.DeserializeObject<WebUrlInfo>(info);
        }

        public static void Download(string url, ICredentials credentials,
            Action<HttpWebRequest, Func<HttpWebResponse>> report)
        {
            var request = WebRequest.CreateHttp(url);
            request.UserAgent = "WinPass";
            if (credentials != null)
                request.Credentials = credentials;
            request.BeginGetResponse(
                new AsyncCallback((result) =>
                {
                    report(request, () =>
                    {
                        var httpRes = result.AsyncState as HttpWebRequest;
                        return httpRes.EndGetResponse(result) as HttpWebResponse;
                    });
                }),
                request);
        }

        public static string Serialize(WebRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            var info = new WebUrlInfo
            {
                Url = request.RequestUri.ToString()
            };

            var credentials = request.Credentials
                as NetworkCredential;

            if (credentials != null)
            {
                info.User = credentials.UserName;
                info.Domain = credentials.Domain;
                info.Password = credentials.Password;
            }

            return JsonConvert.SerializeObject(info);
        }
    }
}