using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BoughtItems
{
    internal class HttpSingleton
    {
        public HttpSingleton()
        {

        }

        private static HttpClient _client;

        public static HttpClient Client
        {
            get
            {
                if (_client == null)
                {
                    HttpClientHandler handler = new()
                    {
                        CookieContainer = Cookies,
                        UseCookies = false,
                        SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
                    };
                    _client = new HttpClient(handler)
                    {
                        //Timeout = TimeSpan.FromSeconds(30),
                    };
                }
                return _client;
            }
        }

        private static CookieContainer _cookies;

        public static CookieContainer Cookies
        {
            get
            {
                _cookies ??= new CookieContainer();
                return _cookies;
            }
        }

        public static void SetCustomCookie(string domain, string cookiesString)
        {
            var list = cookiesString.Split(';').Select(i => i.Split('='));
            Dictionary<string, string> dictCookies = new();
            foreach (var pair in list)
            {
                string key = pair[0].Trim();
                if (key.Length > 0)
                {
                    string value = pair.Length > 1 ? pair[1].Trim() : "";
                    if (value.Length > 0)
                    {
                        dictCookies[key] = value;
                    }
                }
            }
            List<string> listBaseUrls = new();
            if (!domain.StartsWith("http"))
            {
                //add both http and https
                listBaseUrls.Add("http://" + domain);
                listBaseUrls.Add("https://" + domain);
            }
            else
            {
                listBaseUrls.Add(domain);
            }
            foreach (var url in listBaseUrls)
            {
                var uriCookie = new Uri(url);
                foreach (var pair in dictCookies)
                {
                    Cookies.Add(uriCookie, new System.Net.Cookie(pair.Key, pair.Value));
                }
            }
        }

        public static void SetUserAgent(string userAgent)
        {
            Client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
        }
    }
}
