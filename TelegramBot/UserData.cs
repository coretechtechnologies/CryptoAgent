namespace LeFunnyAI {
    public partial class TelegramBot {
        public class UserData {
            // Account details
            public readonly string phoneNumber = "";
            public readonly string registerFirstName = "";
            public readonly string registerLastName = "";

            // AI details
            public readonly string prompt = "";
            public readonly Dictionary<string, string> otherValues = new();

            public UserData(Dictionary<string, string> values) {
                foreach (KeyValuePair<string, string> kvp in values) {
                    switch (kvp.Key) {
                        case "phone":
                            phoneNumber = kvp.Value;
                            break;
                        case "firstname":
                            registerFirstName = kvp.Value;
                            break;
                        case "lastname":
                            registerLastName = kvp.Value;
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