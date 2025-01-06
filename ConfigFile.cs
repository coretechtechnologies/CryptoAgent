namespace LeFunnyAI {
    /// Really simple config file implementation but does make things more readable than previously lmao
    public static class ConfigFile {
        public static Dictionary<string, string> Read(string path) {
            Dictionary<string, string> values = new();

            StreamReader sr = new(path);
            while (!sr.EndOfStream) {
                string ln = (sr.ReadLine() ?? "").Trim();
                int split = ln.IndexOf("=");
                if (split != -1) {
                    string key = ln[..split].ToLowerInvariant();
                    string value = ln[(split + 1)..];
                    while (value.EndsWith("\\")) {
                        value = value.Remove(value.Length - 1) + "\n" + (sr.ReadLine() ?? "").Trim();
                    }
                    values[key] = value;
                }
            }
            sr.Close();

            return values;
        }
    }
}
