using Discord;
using Discord.WebSocket;

namespace LeFunnyAI {
  public partial class DiscordBot {
    public static void Log(string? context, string message) {
      Console.WriteLine(message);
    }

    static bool ConfigValid(string readValue, string? defaultValue = null) {
      return !(readValue == defaultValue || string.IsNullOrWhiteSpace(readValue));
    }

    static string GetDisplayName(IUser user) {
      string dispName = user.GlobalName ?? user.Username;
      if (user is IGuildUser gUser) {
        dispName = gUser.DisplayName; // do they have the vision, if no then gay -> gUser.Nickname ?? dispName;
      }

      return dispName;
    }

    public string DiscordParse(string inp) {
      return DiscordParse(inp, null);
    }

    static int IndexOfAny(string self, IEnumerable<string> strings, int index, StringComparison sc, out string? matched) {
      int i = -1;
      matched = null;

      foreach (string s in strings) {
        int j = self.IndexOf(s, index, sc);
        if (j != -1) {
          if (i == -1 || i > j) {
            i = j;
            matched = s;
          }
        }
      }

      return i;
    }

    // very good implementations of parsing pings, channels, roles, emotes
    public string DiscordParse(string inp, SocketGuild? guild) {
      if (client == null) {
        Log(null, "whar");
        return inp;
      }

      string output = inp;
      // find user pings
      int i = 0;
      while ((i = output.IndexOf("<@", i, StringComparison.Ordinal)) != -1) {
        int oldi = i + 2;
        if (output.Length >= oldi) {
          if (output[oldi] == '!')
            oldi++;
          int newi = output.IndexOf(">", oldi, StringComparison.Ordinal);
          if (newi != -1) {
            string idstr = output[oldi..newi];
            if (ulong.TryParse(idstr, out ulong id)) {
              SocketGuildUser? user = guild?.GetUser(id);
              string append = "@???";
              if (user != null) {
                append = "@" + GetDisplayName(user);
              }
              else {
                SocketUser u = client.GetUser(id);
                if (u != null) {
                  append = "@" + GetDisplayName(u);
                }
              }
              output = output.Remove(i, newi - i + 1).Insert(i, append);
              i += append.Length - 1;
            }
          }
        }
        i++;
      }

      // find channel pings
      i = 0;
      while ((i = output.IndexOf("<#", i, StringComparison.Ordinal)) != -1) {
        int oldi = i + 2;
        if (output.Length >= oldi) {
          int newi = output.IndexOf(">", oldi, StringComparison.Ordinal);
          if (newi != -1) {
            string idstr = output[oldi..newi];
            if (ulong.TryParse(idstr, out ulong id)) {
              SocketGuildChannel? channel = guild?.GetChannel(id);
              output = output.Remove(i, newi - i + 1).Insert(i, "#" + (channel != null ? channel.Name : "deleted-channel"));
            }
          }
        }
        i++;
      }

      // find role pings
      i = 0;
      while ((i = output.IndexOf("<@&", i, StringComparison.Ordinal)) != -1) {
        int oldi = i + 3;
        if (output.Length >= oldi) {
          int newi = output.IndexOf(">", oldi, StringComparison.Ordinal);
          if (newi != -1) {
            string idstr = output[oldi..newi];
            if (ulong.TryParse(idstr, out ulong id)) {
              SocketRole? role = guild?.GetRole(id);
              output = output.Remove(i, newi - i + 1).Insert(i, "@" + (role != null ? role.Name : "deleted-role"));
            }
          }
        }
        i++;
      }

      // find custom emotes
      i = 0;
      string? troll;
      while ((i = IndexOfAny(output, new[]{"<:", "<a:"}, i, StringComparison.Ordinal, out troll)) != -1) {
        int oldi = i + (troll?.Length ?? 2);
        if (output.Length >= oldi) {
          int newi = output.IndexOf(":", oldi, StringComparison.Ordinal);
          if (newi != -1) {
            string emotename = output[oldi..newi];
            oldi = newi + 1;
            newi = output.IndexOf(">", oldi, StringComparison.Ordinal);
            string idstr = output[oldi..newi];
            if (ulong.TryParse(idstr, out ulong id)) {
              foreach (SocketGuild g in client.Guilds) {
                foreach (GuildEmote e in g.Emotes) {
                  if (e.Id == id) {
                    emotename = e.Name;
                    goto end;
                  }
                }
              }
            end:
              output = output.Remove(i, newi - i + 1).Insert(i, $":{emotename}:");
            }
          }
        }
        i++;
      }

      return output;
    }

    public string DiscordUnparse(string inp, Dictionary<string, ulong> users, SocketGuild? guild) {
      if (client == null) {
        Log(null, "whar");
        return inp;
      }

      string output = inp;
      // ping users
      int i = 0;
      while (i < output.Length && (i = output.IndexOf("@", i, StringComparison.Ordinal)) != -1) {
        int oldi = i + 1;
        foreach (KeyValuePair<string, ulong> kvp in users) {
          if (output.Length >= oldi + kvp.Key.Length && output.Substring(oldi, kvp.Key.Length) == kvp.Key) {
            string pong = $"<@{kvp.Value}>";
            output = output.Remove(i, oldi - i + kvp.Key.Length).Insert(i, pong);
            i += pong.Length - 1;
            break;
          }
        }
        i++;
      }

      // ping channels
      if (guild != null) {
        i = 0;
        while (i < output.Length && (i = output.IndexOf("#", i, StringComparison.Ordinal)) != -1) {
          int oldi = i + 1;
          foreach (SocketGuildChannel channel in guild.Channels) {
            if (output.Length >= oldi + channel.Name.Length && output.Substring(oldi, channel.Name.Length) == channel.Name) {
              string pong = $"<#{channel.Id}>";
              output = output.Remove(i, oldi - i + channel.Name.Length).Insert(i, pong);
              i += pong.Length - 1;
              break;
            }
          }
          i++;
        }
      }

      i = 0;
      while (i < output.Length && (i = output.IndexOf(":", i, StringComparison.Ordinal)) != -1) {
        int oldi = i + 1;
        if (output.Length >= oldi) {
          int spacei = output.IndexOf(" ", oldi, StringComparison.Ordinal);
          int newi = output.IndexOf(":", oldi, StringComparison.Ordinal);
          if ((spacei == -1 || spacei > newi) && newi != -1) {
            string emotename = output[oldi..newi];
            string replaceWith = $":{emotename}:";

            foreach (SocketGuild g in client.Guilds) {
              foreach (GuildEmote e in g.Emotes) {
                if (e.Name == emotename) {
                  replaceWith = e.ToString();// $"<:{e.Name}:{e.Id}>";
                  Console.WriteLine("we are replacing with " + replaceWith);
                  goto end;
                }
              }
            }
          end:
            output = output.Remove(i, newi - i + 1).Insert(i, replaceWith);
            i += replaceWith.Length - 1;
          }
        }
        i++;
      }

      return output;
    }
  }
}