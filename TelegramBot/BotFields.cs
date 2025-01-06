using TL;
using WTelegram;

namespace LeFunnyAI {
  public partial class TelegramBot {
    static readonly Dictionary<string, string> globalValues = new();
    readonly Dictionary<string, string> localValues = new();

    string? _GetConfigString(string key) {
      if (localValues.TryGetValue(key, out string? o)) {
        return o;
      }

      if (globalValues.TryGetValue(key, out o)) {
        return o;
      }

      return null;
    }

    public string GetConfigString(string key) {
      return _GetConfigString(key) ?? throw new Exception($"Missing string configuration value for {key}!");
    }

    public double GetConfigFloat(string key) {
      string v = _GetConfigString(key) ?? throw new Exception($"Missing number configuration value for {key}!");
      
      if (double.TryParse(v, out double o)) {
        return o;
      }

      throw new Exception($"{key}'s value {v} is not a valid number!");
    }

    public double? GetConfigFloatOptional(string key) {
      string? v = _GetConfigString(key);
      if (v == null) return null;
      
      if (double.TryParse(v, out double o)) {
        return o;
      }

      throw new Exception($"Optional property {key}'s value exists but {v} is not a valid number!");
    }

    public long GetConfigInt(string key) {
      string v = _GetConfigString(key) ?? throw new Exception($"Missing number configuration value for {key}!");
      
      if (long.TryParse(v, out long o)) {
        return o;
      }

      throw new Exception($"{key}'s value {v} is not a valid number!");
    }

    public long? GetConfigIntOptional(string key) {
      string? v = _GetConfigString(key);
      if (v == null) return null;
      
      if (long.TryParse(v, out long o)) {
        return o;
      }

      throw new Exception($"Optional property {key}'s value exists but {v} is not a valid number!");
    }

    double Yappiness => GetConfigFloat("yappiness");
    long ChatPeerId => GetConfigInt("chatpeerid");
    
    static string globalPrompt = "";
    string individualPrompt = "";

    Client? client = null;
    UpdateManager? updateManager = null;
    User? currentUser = null;

    Dictionary<IPeerInfo, ChannelCache> channelCache = new();
    ChannelCache GetChannelCache(IPeerInfo peer) {
      if (channelCache.TryGetValue(peer, out ChannelCache? cache)) return cache;

      cache = new(peer);
      channelCache.Add(peer, cache);
      return cache;
    }

    DateTime nextResponse = DateTime.UtcNow;

    bool PostedByMe(Message msg) {
      return msg.from_id == null || msg.from_id.ID == client?.UserId;
    }

    bool CheckChannelId(long? peerID) {
      // TODO: this isn't working for some reason
      return ChatPeerId <= 0 || ChatPeerId == peerID;
    }
  }
}
