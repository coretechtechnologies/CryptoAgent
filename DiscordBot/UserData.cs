namespace LeFunnyAI {
    public partial class DiscordBot {
        public class UserData {
            // Account details
            public readonly string token = "";

            // AI details
            public readonly string prompt = "";
            public readonly Dictionary<string, string> otherValues = new();

            public UserData(Dictionary<string, string> values) {
                foreach (KeyValuePair<string, string> kvp in values) {
                    switch (kvp.Key) {
                        case "token":
                            token = kvp.Value;
                            break;
                        case "prompt":
                            prompt = kvp.Value;
                            break;
                        default:
                            otherValues.Add(kvp.Key, kvp.Value);
                            break;
                    }
                }
            }
        }
    }
}