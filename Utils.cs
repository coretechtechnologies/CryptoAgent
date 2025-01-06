namespace LeFunnyAI {
  public static class Utils {
    public static double MessageRelevance(int position, string str) {
      return Math.Sqrt(str.Length) / position;
    }

    public static string BasicFormOfString(string str) {
      return str.Trim();/*
      string output = "";
      foreach (char chr in str) {
        if (char.IsLetterOrDigit(chr)) {
          output += chr;
        }
      }
      return output;*/
    }

    public static bool IsGoingToSleep(string str) {
      string strBasicForm = BasicFormOfString(str.ToLowerInvariant());
      if (strBasicForm.Contains("no")) {
        return false;
      }

      if (strBasicForm.Contains("goodnight") || strBasicForm.Contains("gnight") || strBasicForm.Contains("ightnight") || strBasicForm == "night") {
        return true;
      }

      return false;
      /*string strLower = str.ToLowerInvariant();
      int intentIndex = Math.Max(strLower.IndexOf("i will", StringComparison.Ordinal), Math.Max(strLower.IndexOf("i'll", StringComparison.Ordinal), strBasicForm.IndexOf("mgoing", StringComparison.Ordinal)));
      int sleepWordIndex = Math.Max(strLower.IndexOf("sleep", StringComparison.Ordinal), strLower.IndexOf("bed", StringComparison.Ordinal));

      if (intentIndex == -1 || sleepWordIndex == -1) {
        return false;
      }
      return sleepWordIndex > intentIndex;*/
    }

    public static string FormatFilesize(int size) {
      string[] units = { "B", "kB", "MB", "GB", "TB", "PB" };
      int unit = 0;

      while (size >= 1024) {
        unit++;
        size /= 1024;
      }

      return $"{size} {units[unit]}";
    }
  }
}
