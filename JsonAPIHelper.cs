
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

namespace LeFunnyAI {
    public static class APIHelper {
        static readonly HttpClient client = new();


        public static byte[] GetBytes(string endpoint) {
            lock (client) {
                var httpResponse = client.GetAsync(endpoint).GetAwaiter().GetResult();

                if (httpResponse.StatusCode != HttpStatusCode.OK) {
                    throw new Exception($"{endpoint} returned status code {(int)httpResponse.StatusCode}");
                }

                return httpResponse.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            }
        }

        public static JObject GetJSON(string endpoint) {
            lock (client) {
                var httpResponse = client.GetAsync(endpoint).GetAwaiter().GetResult();
                string? content = httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (httpResponse.StatusCode != HttpStatusCode.OK) {
                    throw new Exception($"{endpoint} returned status code {(int)httpResponse.StatusCode} body {content}");
                }

                JObject obj;
                try {
                    string? result = content.ToString();
                    obj = JObject.Parse(result ?? "");
                }
                catch {
                    throw new Exception($"{endpoint} returned invalid json {content}");
                }

                return obj;
            }
        }


        public static JObject PostJSON(string endpoint, Dictionary<string, object> data) {
            return PostJSON(endpoint, JObject.FromObject(data));
        }

        public static JObject PostJSON(string endpoint, JObject data) {
            lock (client) {
                //Console.WriteLine($"POST {endpoint} {data}");
                string jsonString = data.ToString(Newtonsoft.Json.Formatting.None);
                var httpResponse = client.PostAsync(endpoint, new StringContent(jsonString, Encoding.UTF8, "application/json")).GetAwaiter().GetResult();
                string? content = httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (httpResponse.StatusCode != HttpStatusCode.OK) {
                    throw new Exception($"{endpoint} returned status code {(int)httpResponse.StatusCode} body {content}");
                }

                JObject obj;
                try {
                    string? result = content.ToString();
                    obj = JObject.Parse(result ?? "");
                }
                catch {
                    throw new Exception($"{endpoint} returned invalid json {content}");
                }

                return obj;
            }
        }
    }
}