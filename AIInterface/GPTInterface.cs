using Microsoft.ML.Tokenizers;

namespace LeFunnyAI {
  public class GPTInterface {
    protected static readonly Bpe tokenizer = new("vocab.json", "merges.txt");

    protected static string[] FilterChatByTokenCount(string[] chat, int maxLength) {
      List<string> chatList = new(chat);

      while (tokenizer.Tokenize(string.Join("\n", chatList)).Count >= maxLength) {
        chatList.RemoveAt(0);
      }

      return chatList.ToArray();
    }
  }
}