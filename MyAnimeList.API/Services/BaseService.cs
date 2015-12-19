using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using HtmlAgilityPack;
using HttpClient = System.Net.Http.HttpClient;
using HttpMethod = System.Net.Http.HttpMethod;
using HttpRequestMessage = System.Net.Http.HttpRequestMessage;
using HttpResponseMessage = Windows.Web.Http.HttpResponseMessage;
using HttpStatusCode = System.Net.HttpStatusCode;

namespace MyAnimeList.API.Services
{
    public class BaseService
    {
        protected static Uri BaseUri = new Uri("http://myanimelist.net");

        public HttpResponseHeaders LastHeaders { get; set; }

        public string LastResponseData { get; set; }

        public string UserAgent { get; set; }

        public CookieContainer CookieContainer { get; set; }

        public HttpStatusCode LastHttpStatusCode { get; set; }


        protected BaseService(string userAgent)
        {
            UserAgent = userAgent;
        }

        private HttpClientHandler GetHttpClientHandler()
        {
            return new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
            };
        }

        private HttpBaseProtocolFilter GetHttpBaseProtocolFilter()
        {
            return new HttpBaseProtocolFilter();
        }

        private HttpClient GetHttpClient(ICredentials credentials = null, HttpClientHandler handler = null, int? timeOut = null)
        {
            if (handler == null)
            {
                handler = GetHttpClientHandler();
            }

            if (credentials != null)
                handler.Credentials = credentials;

            if (CookieContainer != null)
                handler.CookieContainer = CookieContainer;

            var restClient = new HttpClient(handler);

            restClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");

            if (timeOut.HasValue)
            {
                restClient.Timeout = TimeSpan.FromSeconds(timeOut.Value);
            }

            restClient.BaseAddress = BaseUri;

            restClient.DefaultRequestHeaders.TryAddWithoutValidation("user-agent", UserAgent);

            return restClient;
        }

        private Windows.Web.Http.HttpClient GetHttpClient(PasswordCredential credentials = null, HttpBaseProtocolFilter handler = null, int? timeOut = null)
        {
            if (handler == null)
            {
                handler = GetHttpBaseProtocolFilter();
            }

            if (credentials != null)
                handler.ServerCredential = credentials;

            //if (CookieContainer != null)
            //    handler.CookieContainer = CookieContainer;

            var restClient = new Windows.Web.Http.HttpClient(handler);

            restClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");


            restClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);

            return restClient;
        }

        protected async Task<string> GetAsync(string path, PasswordCredential credentials = null)
        {
            var restClient = GetHttpClient(credentials);

            if (path.Contains("?"))
            {
                path = path + "&cache=" + Guid.NewGuid();
            }
            else
                path = path + "?cache=" + Guid.NewGuid();

            HttpResponseMessage response;

            try
            {
                response = await restClient.GetAsync((new Uri(BaseUri + path)));
            }
            catch (HttpRequestException ex)
            {
                throw new ServiceException(ex.Message, ex);
            }

            LastResponseData = await response.Content.ReadAsStringAsync();

            return LastResponseData;
        }

        //protected async Task<string> GetAsync(string path, Dictionary<string, string> parameters = null, ICredentials credentials = null)
        //{
        //    var restClient = GetHttpClient(credentials);

        //    if (path.Contains("?"))
        //    {
        //        path = path + "&cache=" + Guid.NewGuid();
        //    }
        //    else
        //        path = path + "?cache=" + Guid.NewGuid();

        //    var request = new HttpRequestMessage(HttpMethod.Get, path);

        //    HttpResponseMessage response;

        //    if (parameters != null)
        //    {
        //        request.Content = new FormUrlEncodedContent(parameters);
        //    }

        //    try
        //    {
        //        response = await restClient.SendAsync(request);
        //    }
        //    catch (HttpRequestException ex)
        //    {
        //        throw new ServiceException(ex.Message, ex);
        //    }

        //    LastHttpStatusCode = response.StatusCode;

        //    HttpRequestHelper.HandleHttpCodes(response.StatusCode);

        //    LastHeaders = response.Headers;

        //    LastResponseData = await response.Content.ReadAsStringAsync();

        //    return LastResponseData;
        //}

        protected async Task<string> DeleteAsync(string path, PasswordCredential credentials = null)
        {
            var restClient = GetHttpClient(credentials);

            if (path.Contains("?"))
            {
                path = path + "&cache=" + Guid.NewGuid();
            }
            else
                path = path + "?cache=" + Guid.NewGuid();

            HttpResponseMessage response;

            try
            {
                response = await restClient.DeleteAsync(new Uri(BaseUri + path));
            }
            catch (HttpRequestException ex)
            {
                throw new ServiceException(ex.Message, ex);
            }

            LastResponseData = await response.Content.ReadAsStringAsync();

            return LastResponseData;
        }

        protected async Task<string> PostAsync(string path, Dictionary<string, string> parameters = null, PasswordCredential credentials = null)
        {
            var restClient = GetHttpClient(credentials);

            if (path.Contains("?"))
            {
                path = path + "&cache=" + Guid.NewGuid();
            }
            else
                path = path + "?cache=" + Guid.NewGuid();

            var request = new HttpRequestMessage(HttpMethod.Post, path);
            HttpResponseMessage response;

            if (parameters != null)
            {
                request.Content = new FormUrlEncodedContent(parameters);
            }

            try
            {
                response = await restClient.PostAsync(new Uri(BaseUri + path), new HttpFormUrlEncodedContent(parameters));
            }
            catch (HttpRequestException ex)
            {
                throw new ServiceException(ex.Message, ex);
            }


            LastResponseData = await response.Content.ReadAsStringAsync();

            return LastResponseData;
        }


        protected string TryGetHeaderValue(HttpResponseHeaders headers, string key)
        {
            string result = null;

            if (headers == null || String.IsNullOrEmpty(key))
            {
                return null;
            }

            foreach (var header in headers)
            {
                if (header.Key.ToLowerInvariant() == key)
                {
                    if (key == "set-cookie")
                    {
                        result = string.Join(",", header.Value);

                    }
                    else
                    {
                        var headerEnumerator = header.Value.GetEnumerator();


                        headerEnumerator.MoveNext();

                        result = headerEnumerator.Current;
                    }
                    break;
                }
            }

            return result;
        }


        private static List<string> ConvertCookieHeaderToArrayList(string strCookHeader)
        {
            strCookHeader = strCookHeader.Replace("\r", "");
            strCookHeader = strCookHeader.Replace("\n", "");
            string[] strCookTemp = strCookHeader.Split(',');
            var al = new List<string>();
            int i = 0;
            int n = strCookTemp.Length;
            while (i < n)
            {
                if (strCookTemp[i].IndexOf("expires=", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    al.Add(strCookTemp[i] + "," + strCookTemp[i + 1]);
                    i = i + 1;
                }
                else
                {
                    al.Add(strCookTemp[i]);
                }
                i = i + 1;
            }
            return al;
        }

        private static CookieCollection ConvertCookieArraysToCookieCollection(List<string> al)
        {
            CookieCollection cc = new CookieCollection();
            int alcount = al.Count;
            for (int i = 0; i < alcount; i++)
            {
                string strEachCook = al[i];
                string[] strEachCookParts = strEachCook.Split(';');
                int intEachCookPartsCount = strEachCookParts.Length;

                Cookie cookTemp = new Cookie();
                for (int j = 0; j < intEachCookPartsCount; j++)
                {
                    if (j == 0)
                    {
                        string strCNameAndCValue = strEachCookParts[j];
                        if (strCNameAndCValue != string.Empty)
                        {
                            int firstEqual = strCNameAndCValue.IndexOf("=", StringComparison.Ordinal);
                            string firstName = strCNameAndCValue.Substring(0, firstEqual);
                            string allValue = strCNameAndCValue.Substring(firstEqual + 1, strCNameAndCValue.Length - (firstEqual + 1));
                            cookTemp.Name = firstName.Replace(" ", "");
                            allValue = WebUtility.UrlEncode(allValue);
                            cookTemp.Value = allValue;
                        }
                        continue;
                    }
                    if (strEachCookParts[j].IndexOf("path", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var strPNameAndPValue = strEachCookParts[j];
                        if (strPNameAndPValue != string.Empty)
                        {
                            var nameValuePairTemp = strPNameAndPValue.Split('=');
                            cookTemp.Path = nameValuePairTemp[1] != string.Empty ? nameValuePairTemp[1] : "/";
                        }
                    }
                }
                if (cookTemp.Path == string.Empty)
                {
                    cookTemp.Path = "/";
                }
         
                cc.Add(cookTemp);
            }
            return cc;
        }

        protected async Task InitializeCookie(string login, string password)
        {
            var stringCookie = SettingsHelper.Get<string>("Cookie", null);

            if (string.IsNullOrEmpty(stringCookie))
            {

                var handler = GetHttpClientHandler();
                handler.AllowAutoRedirect = false;
                CookieCollection cookieCollection = new CookieCollection();
                handler.CookieContainer = new CookieContainer();
                handler.CookieContainer.Add(new Uri(@"http://myanimelist.net"), cookieCollection);
                var restClient = GetHttpClient(null, handler);

                restClient.BaseAddress = new Uri(@"http://myanimelist.net/panel.php?cache=" + Guid.NewGuid());

                var request = new HttpRequestMessage(HttpMethod.Get, "");

                var response = await restClient.SendAsync(request);

                //need to check how the cookie is structured in my old wrapper.
                var cookieHeader = response.Headers.GetValues("Set-Cookie");

                var content = await response.Content.ReadAsStringAsync();

                var document = new HtmlDocument();

                document.LoadHtml(content);

                var csrfTokenNode =
                    document.DocumentNode.Descendants("meta")
                        .FirstOrDefault(meta => meta.GetAttributeValue("name", string.Empty) == "csrf_token");
                var csrfToken = csrfTokenNode.Attributes["content"].Value;

                var cc = new CookieCollection();
                if (cookieHeader.FirstOrDefault() != string.Empty)
                {
                    var al = ConvertCookieHeaderToArrayList(string.Join(",", cookieHeader));
                    cc = ConvertCookieArraysToCookieCollection(al);
                }


                var cookieContainer = new CookieContainer();

                cookieContainer.Add(new Uri("http://myanimelist.net/login.php"), cc);
                
                var handler2 = new HttpClientHandler() { UseCookies = false, AllowAutoRedirect = false };
                var client = new HttpClient(handler2) { BaseAddress = new Uri("http://myanimelist.net") };

                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
                var message = new HttpRequestMessage(HttpMethod.Post, "/login.php");

                message.Headers.Add("Cookie", string.Join(",", cookieHeader));

                message.Headers.Add("Connection", "keep-alive");
                message.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/40.0.2214.93 Safari/537.36");
                message.Headers.Add("Accept-Encoding", "gzip, deflate");
                message.Content = new StringContent(string.Format(@"user_name={0}&password={1}&submit=Login&csrf_token={2}", login,
                    password, csrfToken), Encoding.UTF8, "application/x-www-form-urlencoded");

                var httpResponseMessage = await client.SendAsync(message);

                if (httpResponseMessage.StatusCode != HttpStatusCode.Found)
                    throw new ServiceException("Not Authenticated");

                var responseCookies = string.Join(",", httpResponseMessage.Headers.GetValues("Set-Cookie"));

                SettingsHelper.Set("Cookie", responseCookies);
            }

            stringCookie = SettingsHelper.Get<string>("Cookie", null);
            if (stringCookie != null)
            {

                stringCookie = stringCookie.Replace("HttpOnly,", "");
                stringCookie = stringCookie.Replace("httponly,", "");
                var parts = stringCookie.Split(';')
                 .Where(i => i.Contains("=")) // filter out empty values
                 .Select(i => i.Trim().Split('=')) // trim to remove leading blank
                 .Select(i => new { Name = i.First(), Value = i.Last() });

                CookieContainer = new CookieContainer();

                foreach (var val in parts)
                {
                    CookieContainer.Add(BaseUri, new Cookie(val.Name, WebUtility.UrlEncode(val.Value)));

                }

            }
            else
                throw new ServiceException("Not Authenticated");


        }
    }
}