using TL;

// Async method lacks 'await' operators and will run synchronously
// Using a sync method requires us to return though, and requires a code change to allow awaiting so...
#pragma warning disable CS1998

namespace LeFunnyAI {
  public partial class TelegramBot {

    public static void Log(string? context, string message) {
      Console.WriteLine(message);
    }

    static bool ConfigValid(string readValue, string? defaultValue = null) {
        return !(readValue == defaultValue || string.IsNullOrWhiteSpace(readValue));
    }

    static string GetDisplayName(User user) {
      return $"{user.first_name} {user.last_name}".Trim();
    }
  }
}