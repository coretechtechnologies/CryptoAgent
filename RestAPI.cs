using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LeFunnyAI {
  // implementation of a 'banana style' rest API but selfhosted
  // banana namespace was basically nuked because banana serverless is no more :troleft:

  public static partial class RestAPI {
    static readonly JsonSerializerSettings _jsonSettings = new() {
      TypeNameHandling = TypeNameHandling.All,
      TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full
    };

    public static int Timeout { get; set; } = 150000;

    // THE MAIN FUNCTIONS
    // ___________________________________


    public static JObject RunMain(string endpoint, object modelInputs) {
      Console.WriteLine("start RunMain");
      try {
        JObject result = _StartAPI(endpoint, modelInputs);
        JObject dictOut;

        if (result.ContainsKey("output")) {
          dictOut = new JObject {
            { "id", "id" },
            { "message", "message" },
            { "created", "created" },
            { "apiVersion", "apiVersion" },
            { "modelOutputs", new JArray { result } }
          };

          return dictOut;
        }

        dictOut = new JObject {
          { "skill_issue", result["skill_issue"] }
        };
        return dictOut;
      }
      catch (Exception e) {
        JObject dictOut = new()
        {
          { "skill_issue", e.ToString() }
        };
        return dictOut;
      }
    }


    // THE API CALLING FUNCTIONS
    // ________________________

    // Takes in start params and returns the full server json response
    static JObject _StartAPI(string endpoint, object modelInputs) {
      Console.WriteLine("start _StartAPI");
      string urlStart = endpoint;

      var httpRequest = (HttpWebRequest)WebRequest.Create(urlStart);
      httpRequest.Method = "POST";

      httpRequest.Accept = "application/json";
      httpRequest.ContentType = "application/json";

      string payload = JsonConvert.SerializeObject(modelInputs, new JsonDictionaryConverter());

      using (var streamWriter = new StreamWriter(httpRequest.GetRequestStream())) {
        streamWriter.Write(payload);
      }

      var httpResponse = (HttpWebResponse)httpRequest.GetResponse();

      if (httpResponse.StatusCode != HttpStatusCode.OK) {
        throw new Exception($"server error: status code {(int)httpResponse.StatusCode}");
      }

      JObject obj;
      try {
        using var streamReader = new StreamReader(httpResponse.GetResponseStream());
        string result = streamReader.ReadToEnd();
        obj = JObject.Parse(result);
      }
      catch {
        throw new Exception("server error: returned invalid json");
      }

      return obj;
    }
  }
}
