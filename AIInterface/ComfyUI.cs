using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json.Linq;

namespace LeFunnyAI {
    public static class ComfyUI {
        const string SERVER_ADDRESS = "127.0.0.1:8188"; // TODO: config

        public static void GenerateImageByPrompt(string prompt, string outputPath, bool savePreviews = false) {
            (ClientWebSocket ws, string clientId) = OpenWebsocketConnection(SERVER_ADDRESS);
            string promptId = QueuePrompt(prompt, clientId, SERVER_ADDRESS);
            TrackProgress(ws, promptId);
            List<JObject> images = GetImages(promptId, SERVER_ADDRESS, savePreviews);
            //Console.WriteLine("writing file");
            File.WriteAllBytes(outputPath, (byte[])images[0]["image_data"]);
            //Console.WriteLine("closing ws");
            ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "meow :3", new CancellationToken()).GetAwaiter().GetResult();
            //Console.WriteLine("gootbye");
        }


        static (ClientWebSocket ws, string clientId) OpenWebsocketConnection(string serverAddress) {
            byte[] buf = new byte[16];
            Random.Shared.NextBytes(buf);
            string clientId = string.Join("", buf.Select(s => s.ToString("x2")));
            
            ClientWebSocket ws = new();
            ws.ConnectAsync(new Uri($"ws://{serverAddress}/ws?clientId={clientId}"), new CancellationToken()).GetAwaiter().GetResult();
            return (ws, clientId);
        }

        static JObject GetHistory(string promptId, string serverAddress) {
            return APIHelper.GetJSON($"http://{serverAddress}/history/{promptId}");
        }

        static string QueuePrompt(string prompt, string clientId, string serverAddress) {
            //Console.WriteLine("QueuePrompt()");
            JObject p = new() {
                {"prompt", JObject.Parse(prompt) },
                {"client_id", clientId }
            };

            JObject obj = APIHelper.PostJSON($"http://{serverAddress}/prompt", p);
            if (obj.GetValue("prompt_id") != null) {
                return obj.GetValue("prompt_id").ToString();
            }
            throw new Exception($"QueuePrompt: server did not return prompt_id {obj}");
        }

        static void TrackProgress(ClientWebSocket ws, string promptId) {
            //Console.WriteLine("tracking progress of " + promptId);
            byte[] buf = new byte[16*1024*1024];
            bool working = true;

            while (working) {
                var o = ws.ReceiveAsync(buf, new CancellationToken()).GetAwaiter().GetResult();
                
                if (o.MessageType == WebSocketMessageType.Text) {
                    JObject message = JObject.Parse(Encoding.UTF8.GetString(buf, 0, o.Count));
                    //Console.WriteLine("received " + message);
                    JObject message_data = message.GetValue("data") as JObject;

                    //Console.WriteLine("h " + (message.GetValue("type")?.ToString() ?? ""));
                    switch (message.GetValue("type")?.ToString() ?? "") {
                        case "executing":
                            //Console.WriteLine("we executing bro s " + message_data + " '" + message_data["node"].ToString() + "' '" + message_data["prompt_id"].ToString() + "'");
                            if (message_data["node"].ToString() == "" && message_data["prompt_id"].ToString() == promptId) {
                                return;
                            }
                            break;
                    }
                }
            }
        }

        static byte[] GetImage(string filename, string subfolder, string folderType, string serverAddress) {
            return APIHelper.GetBytes($"http://{serverAddress}/view?filename={Uri.EscapeDataString(filename)}&subfolder={Uri.EscapeDataString(subfolder)}&type={Uri.EscapeDataString(folderType)}");
        }

        static List<JObject> GetImages(string promptId, string serverAddress, bool allowPreview = false) {
            //Console.WriteLine("getting images of " + promptId);
            List<JObject> outputImages = new();

            JObject history = GetHistory(promptId, serverAddress)[promptId] as JObject;

            foreach ((string nodeId, JToken _nodeOutput) in history["outputs"] as JObject) {
                JObject outputData = new();
                
                if (_nodeOutput is JObject nodeOutput && nodeOutput.TryGetValue("images", out JToken? _images)) {
                    if (_images is JArray images) {
                        foreach (JObject image in images) {
                            switch (image["type"].ToString()) {
                                case "temp":
                                    if (allowPreview) {
                                        byte[] previewData = GetImage(image["filename"].ToString(), image["subfolder"].ToString(), image["type"].ToString(), serverAddress);
                                        outputData["image_data"] = previewData;
                                    }
                                    break;
                                case "output":
                                    byte[] imageData = GetImage(image["filename"].ToString(), image["subfolder"].ToString(), image["type"].ToString(), serverAddress);
                                    outputData["image_data"] = imageData;
                                    break;
                            }
                    
                            outputData["file_name"] = image["filename"];
                            outputData["type"] = image["type"];
                            outputImages.Add(outputData);
                        }
                    }
                }
            }

            return outputImages;
        }
    }
}