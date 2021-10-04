using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using Newtonsoft.Json;

namespace CallCentre
{
    public class HttpWrapper
    {
        private readonly string _accessToken;

        public HttpWrapper(string accessToken)
        {
            _accessToken = accessToken;
        }

        public T Invoke<T>(string method, string uri, string body)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(uri),
                Timeout = new TimeSpan(0, 0, 90)
            };

            var version = Assembly.GetEntryAssembly()?.GetName().Version;
            var name = Assembly.GetEntryAssembly()?.GetName().Name;


            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
            httpClient.DefaultRequestHeaders.Add("User-Agent", $"{name} {version}");
            httpClient.DefaultRequestHeaders.Add("MAC", Helpers.GetMacAddress());

            HttpResponseMessage response;
            var httpMethod = new HttpMethod(method);

            switch (httpMethod.ToString().ToUpper())
            {
                case "GET":
                case "HEAD":
                    response = httpClient.GetAsync(uri).Result;
                    break;
                case "POST":
                {
                    HttpContent httpContent = new StringContent(body);
                    httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    response = httpClient.PostAsync(uri, httpContent).Result;
                }
                    break;
                case "PUT":
                {
                    HttpContent httpContent = new StringContent(body);
                    httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    response = httpClient.PutAsync(uri, httpContent).Result;
                }
                    break;
                case "DELETE":
                    response = httpClient.DeleteAsync(uri).Result;
                    break;
                default:
                    throw new NotImplementedException();
            }

            response.EnsureSuccessStatusCode();
            var content = response.Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject<T>(content);
        }
    }
}