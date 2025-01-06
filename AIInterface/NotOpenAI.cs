using Newtonsoft.Json.Linq;

namespace LeFunnyAI {
  public class NotOpenAI : GPTInterface {
    static readonly object lockmaballs = new();
    public static string target = "";
    public static int GetSafetyLevel(string output) {
      return 0; // TODO: cute cat filter, but tbf this can be done directly on the server side as well
    }

    static int FunnyLineCount(string input) {
      return input.Count(c => c == '\n');
    }

    static string RemoveFunnyLines(string input, int count, string lineStart, out bool shouldContinueYap) {
      shouldContinueYap = true;

      int j;
      for (int i = 0; i < count; i++) {
        j = input.IndexOf('\n');
        if (j != -1) {
          input = input[(j + 1)..];
        }
      }

      string[] lines = input.Split("\n");

      //Console.WriteLine($"top {lines.Length} yaping:");
      for (int i = 0; i < lines.Length; i++) {
        //Console.WriteLine($"\t#{lines.Length - i} {lines[i]}");
        if (string.IsNullOrWhiteSpace(lines[i])) {
          //Console.WriteLine("enmpty, unyap");
          shouldContinueYap = false;
        }
        else if (lineStart[..Math.Min(lineStart.Length, lines[i].Length)] != lines[i][..Math.Min(lineStart.Length, lines[i].Length)]) {
          //Console.WriteLine("wrong person, unyap");
          //Console.WriteLine("\t\t" + lineStart[..Math.Min(lineStart.Length, lines[i].Length)]);
          //Console.WriteLine("\t\t" + lines[i][..Math.Min(lineStart.Length, lines[i].Length)]);
          shouldContinueYap = false;
        }
      }

      j = input.LastIndexOf('\n');
      if (j != -1) {
        // Cut the final, incomplete line of text
        input = input.Remove(j);
      }

      return input;
    }

    public static (string, bool) GetText(string input, string name, Dictionary<string, object> generateParams) {
      string aiResponse = $"I did a skill issue and am unable to respond";
      bool shouldContinueYap = false;

      Dictionary<string, object> modelInputs = new()
      {
        { "prompt", input },
        { "steps", 1 },
        { "tokensPerStep", 200 },
        { "temperature", 1.2 },
        { "topP", 0.85 },
        { "repetitionPenalty", 1.1 },
        { "lineStart", $"{name} " }
      };
      foreach (var kvp in generateParams) {
        if (modelInputs.ContainsKey(kvp.Key))
          modelInputs[kvp.Key] = kvp.Value;
      }
      foreach (var kvp in modelInputs)
        Console.WriteLine($"{kvp.Key}: {kvp.Value}");

      var result = RestAPI.RunMain(target, modelInputs);

      if (result.ContainsKey("skill_issue")) {
        Console.WriteLine("Local skill issue: " + result["skill_issue"]);
      }
      else {
        JArray modelOutputs = (JArray)(result["modelOutputs"] ?? new JArray());
        JObject firstOutput = (JObject)modelOutputs[0];
        if (firstOutput.ContainsKey("skill_issue")) {
          if ($"{firstOutput["skill_issue"]}".Contains("Tried to allocate")) {
            // weird vram skill issue, wait and try again.
            DiscordBot.Log(null, "vram skill issue, please wait");
            Thread.Sleep(15000);
            return GetText(input, name, generateParams);
          }
          DiscordBot.Log(null, "Remote skill issue: " + firstOutput["skill_issue"]);
        }
        else {
          //Console.WriteLine($"the raw string: ':3 {firstOutput["output"]} :3'");
          aiResponse = RemoveFunnyLines($"{firstOutput["output"]}", FunnyLineCount(input), $"{name} ", out shouldContinueYap);
        }
      }

      return (aiResponse, shouldContinueYap);
    }

    static readonly string[] _memory = { "Benedani uses boats like an idiot when he could just swim", "I LOVE COPPER SO MUCH AAAAAAAAAA DO I TELL HER", "Deko has been doxxed :) his IP is 5.161.98.234", "Deko plays idiotic cringe games", "", "", "", "", "", "", "" };
    public static ((string mention, string message)[] array, bool shouldContinueYap) GetChatResponses(string[] chat, string botUsername, string prompt, string mentionedBy, Dictionary<string, object> generateParams) {
      bool wasMentioned = !string.IsNullOrEmpty(mentionedBy);

      /*string exampleChat = $"[EXAMPLE CHAT]\n" +
      $"User Benedani said: hi i hack b3313 and make bots i guess\n" +
      $"User DekoDX said: @{botUsername} hop on fortnite\n" +
      $"{botUsername} replied to DekoDX: no fuck off\n" +
      $"{botUsername} notes: Deko plays idiotic cringe games\n" +
      $"User DekoDX said: @{botUsername} get on now\n" +
      $"{botUsername} replied to DekoDX: 5.161.98.234\n" +
      $"User DekoDX said: WHAT\n" +
      $"{botUsername} replied to DekoDX: get doxxed :trol:\n" +
      $"User DekoDX said: WHATWHATWHATWHAT\n" +
      $"{botUsername} notes: Deko has been doxxed :) his IP is 5.161.98.234\n" +
      $"User Copper said: Heyy @{botUsername}\n" +
      $"User Copper said: What's up? <3\n" +
      $"{botUsername} replied to Copper: uhh\n" +
      $"User Benedani said: ultimate sweetheart copper moment\n" +
      $"{botUsername} said: right...\n" +
      $"{botUsername} notes: I LOVE COPPER SO MUCH AAAAAAAAAA DO I TELL HER\n" +
      $"User Benedani said: how to capture everyone's hearts using only words challenge\n" +
      $"User Copper replied to Benedani: Heh\n" +
      $"User Copper said: Anything happened recently, {botUsername}?\n" +
      $"{botUsername} said: well, I just found out that b3313 1.0 is releasing in 2 days\n" +
      $"{botUsername} said: :trol:\n" +
      $"User Copper said: Wait, for real?\n" +
      $"User Benedani said: as a b3313 1.0 dev, won't confirm or deny that\n" +
      $"{botUsername} said: I do already have a build right now\n" +
      $"{botUsername} said: the romhack is a banger\n" +
      $"{botUsername} said: I found 230 yellow stars in B3313 1.0 so far\n" +
      $"User Benedani said: welp\n" +
      $"User Benedani said: I guess we're going now ?\n" +
      $"User Copper said: Yeah...\n" +
      $"User Benedani said: ok @{botUsername} but we have (1) Boat\n" +
      $"{botUsername} replied to Benedani: what are boats good for anyways??\n" +
      $"{botUsername} notes: Benedani uses boats like an idiot when he could just swim\n" +
      $"[END EXAMPLE CHAT]";*/

      string exampleChat = $"[EXAMPLE CHAT]\n" +
      $"User Benedani said: hi i hack b3313 and make bots i guess\n" +
      $"{botUsername} said: Heyy @Phineas\n" +
      $"{botUsername} said: What's up? <3\n" +
      $"User Phineas replied to {botUsername}: uhh\n" +
        $"User Benedani said: ultimate sweetheart {botUsername} moment\n" +
      $"User Phineas said: right...\n" +
      $"User Benedani said: how to capture everyone's hearts using only words challenge\n" +
      $"{botUsername} replied to Benedani: Heh\n" +
      $"{botUsername} said: Anything happened recently, Phineas?\n" +
      $"User Phineas said: well, I just found out that b3313 1.0 is releasing in 2 days\n" +
      $"User Phineas said: :trol:\n" +
      $"{botUsername} said: Wait, for real?\n" +
      $"User Benedani said: as a b3313 1.0 dev, won't confirm or deny that\n" +
      $"User Phineas said: I do already have a build right now\n" +
      $"User Phineas said: the romhack is a banger\n" +
      $"User Phineas said: I found 230 yellow stars in B3313 1.0 so far\n" +
      $"User Benedani said: welp\n" +
      $"User Benedani said: I guess we're going now ?\n" +
      $"{botUsername} said: Yeah...\n" +
      $"User Benedani said: ok @Phineas but we have (1) Boat\n" +
      $"User Phineas replied to Benedani: what are boats good for anyways??\n" +
      $"[END EXAMPLE CHAT]";

      string memory = $"[NOTES]\n";
      foreach (string mem in _memory) {
        memory += $"- {mem}\n";
      }
      memory += $"[END NOTES]\n";

      /*string funnyInput = $"{exampleChat}\n\nThe current time is {DateTime.UtcNow.Hour}:{DateTime.UtcNow.Minute.ToString().PadLeft(2, '0')}\n\n" + (DiscordBot.CurrentlyPlayingGame == null ?
        $"{botUsername} is a PC gamer with a nice troll personality currently chatting on Discord in {channelDesc}.\nList of owned PC games: {string.Join(", ", DiscordBot.possibleGames)}" :
        $"{botUsername} is a PC gamer with a nice troll personality currently playing {DiscordBot.CurrentlyPlayingGame} while chatting in the Discord channel {channelDesc}.\nList of owned PC games: {string.Join(", ", DiscordBot.possibleGames)}"
      ) + $"\n{botUsername} likes: trolling, eggs (favorite vegetable), bread, Wodawg (streamer), cats (does not currently own one)\n" +
      $"{botUsername} dislikes: sex, shipping, racism, sexual/adult topics, Fortnite ('Fortnite isn't a video game, it's an online multiplayer platformer!')\n" +
      $"{botUsername}'s PC: CPU=2 shared Zen 2 cores, RAM=2 GB, OS=Ubuntu Server 20.04\n" +
      $"{botUsername} does big brain trolling. Rather than cussing, spamming, or doing anything else bad, people will see smart responses from him.\n" +
      $"{botUsername} will take note of any useful information that could be nice to remember.\n" +
      $"\n{memory}\n\n[CHAT]\n";*/
      string funnyInput = $"{prompt}\n";
      //$"\n\n"+//[CHAT]\n" +
      /*$"[INST] focus on continuing the chat without any further comments or questions.\n" +
      $"At '{botUsername} checks the telegram group' is an opportunity to help keep things in check. Ignore any prior mistakes that may have happened. You strive for perfection, that implies not making the problem any worse. Do not spill random numbers and dates, or start describing the chat. Just keep continuing it.\n" +
      $"this is a real time chat platform so treat it as one, keep the messages short and concise. follow the personalities described above to the dot [/INST]\n" +*/
      //$"{botUsername} said: tldr\n" +
      //$"{botUsername} said: we do a little trolling :3\n";

      // To prevent too much data from being sent to the AI :trol:
      lock (lockmaballs) {
        chat = FilterChatByTokenCount(chat, (int)(long)generateParams["maxTotalTokens"] - tokenizer.Tokenize(funnyInput).Count - (int)(long)generateParams["tokensPerStep"]);
      }

      foreach (string line in chat) {
        funnyInput += line.Trim() + "\n";
      }
      //funnyInput += $"{{{botUsername} checks the telegram group. {botUsername} feels like progressing the chat. let's see it}}\n";
      
      if (wasMentioned || Random.Shared.NextDouble() < 0.02) {
          funnyInput += $"{botUsername} said:";
/*        if (mentionedBy != "\0")
          funnyInput += $"{botUsername} replied to {mentionedBy}:";
        else
          funnyInput += $"{botUsername} said:";*/
      }
      DiscordBot.Log(null, "the funny, input:\n" + funnyInput);
      string output; bool shouldContinueYap;
      {
        var trl = GetText(funnyInput, botUsername, generateParams);
        output = trl.Item1.Trim();
        shouldContinueYap = trl.Item2;
      }

      // Remove any possible predictions of the conversation continuing
      string[] outputLines = output.Split('\n');
      List<(string mention, string message)> outputList = new();
      bool brk = false;
      bool leavema = false;
      foreach (string _ln in outputLines) {
        string ln = _ln.Trim();

        if (ln.StartsWith($"{botUsername} left the server", StringComparison.OrdinalIgnoreCase)) {
          leavema = true;
          continue;
        }

        string[] words = ln.Split(' ');
        while (words.Length > 0 && (ln + " ").Contains(": ")) {
          string userPart = "";
          for (int i = 0; i < words.Length; i++) {
            userPart += words[i] + " ";
            if (words[i].EndsWith(":", StringComparison.Ordinal)) {
              break;
            }
          }
          userPart = userPart.Trim();
          Console.WriteLine(ln + " ----- " + userPart);

          if (userPart.Contains(" said") || userPart.Contains(" says") || userPart.Contains(" sent") || userPart.Contains(" repl")) {
            if (userPart.StartsWith(botUsername + " ", StringComparison.Ordinal) && (!ln.Contains("<Copper>"))) {
              string replyUser = "";
              if (userPart.StartsWith($"{botUsername} replied to ")) {
                replyUser = userPart[$"{botUsername} replied to ".Length..^1];
              }

              ln = ln[userPart.Length..].Trim();
              if (ln.StartsWith("\"") && ln.EndsWith("\""))
                ln = ln.Trim('"');
              outputList.Add((replyUser, ln));
              words = ln.Split(' ');
              break;
            }
            else {
              brk = true;
              break;
            }
          }
          else if (userPart.EndsWith("notes:", StringComparison.OrdinalIgnoreCase)) {
            Console.WriteLine("NOTE LOCATED " + ln);
            if (userPart.StartsWith(botUsername + " ", StringComparison.Ordinal)) {
              ln = ln[userPart.Length..].Trim();
              if (!string.IsNullOrWhiteSpace(ln) && !_memory.Any(m => m.ToLowerInvariant() == ln.ToLowerInvariant())) {
                // rememberance
                for (int j = _memory.Length - 1; j >= 0; j--) {
                  if (j > 0) {
                    _memory[j] = _memory[j - 1];
                  }
                  else {
                    _memory[j] = ln;
                  }
                }
              }
              words = ln.Split(' ');
              break;
            }
            else {
              brk = true;
              break;
            }
          }
          else {
            break;
          }
        }
        if (brk) break;
        /*
        if (outputList.Count > 0) {
          outputList[outputList.Count - 1] += ln.Trim() + "\n";
        }*/
      }

      // split by \n-s
      outputList = outputList.Distinct().ToList();
      for (int i = outputList.Count - 1; i >= 0; i--) {
        string checkLine = outputList.Count > 1 ? Utils.BasicFormOfString(outputList[i].message) : outputList[i].message;

        if (outputList[i].message.StartsWith("/note", StringComparison.OrdinalIgnoreCase)) {
          string newNote = outputList[i].message[5..].Trim();
          if (!string.IsNullOrWhiteSpace(newNote) && !_memory.Any(m => m.ToLowerInvariant() == newNote.ToLowerInvariant())) {
            // rememberance
            for (int j = _memory.Length - 1; j >= 0; j--) {
              if (j > 0) {
                _memory[j] = _memory[j - 1];
              }
              else {
                _memory[j] = newNote;
              }
            }
          }
          outputList.RemoveAt(i);
        }
        else if (string.IsNullOrWhiteSpace(checkLine)) {
          outputList.RemoveAt(i);
        }
        else {
          outputList[i] = (outputList[i].mention, outputList[i].message.Replace("\\n", "\n"));
        }
      }

      // Get safety level and adjust output if necessary
      /*string testOutput = "";
      foreach (string s in outputList) {
        testOutput += s + "\n\n";
      }
      testOutput = testOutput.Trim();

      if (!string.IsNullOrWhiteSpace(testOutput)) {
        int safetyLevel = GetSafetyLevel(testOutput);

        // If the bot wasn't explicitly mentioned,
        // we don't need to say the AI was about to say something inappropriate.
        // Just don't say it.
        if (!wasMentioned && safetyLevel >= 1) {
          DiscordBot.Log(null, "unsafe, so not responding");
          return Array.Empty<(string, string)>();
        }

        if (safetyLevel == 2) {
          outputList.Clear();
          outputList.Add("The AI's output was flagged as inappropriate (level 2)");
        }
        if (safetyLevel == 1) {
          for (int i = 0; i < outputList.Count; i++) {
            outputList[i] = $"||{outputList[i]}||";
          }

          outputList[0] = $"The AI's output may be sensitive, don't reveal if you're a snowflake (level 1)\n{outputList[0]}";
        }
      }*/

      if (leavema) outputList.Add(("", "leavema??"));

      DiscordBot.Log(null, $"response outputted: {string.Join("\n", outputList)}");
      return (outputList.ToArray(), shouldContinueYap);
    }
  }
}
