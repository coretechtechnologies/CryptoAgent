using Discord;
using Discord.WebSocket;
using System.Text.RegularExpressions;

// Async method lacks 'await' operators and will run synchronously
// Using a sync method requires us to return though, and requires a code change to allow awaiting so...
#pragma warning disable CS1998

namespace LeFunnyAI {
    public partial class DiscordBot {
        const string CONFIG_ROOT = "data/dc";

        public static DiscordBot[]? _Connect() {
            MediaReader.Init();
            
            static void WriteDefaultConfigFile(string path) {
                StreamWriter sw = new(path);
                if (path == $"{CONFIG_ROOT}/tokens.txt") {
                    sw.WriteLine("aiserver=http://127.0.0.1:8000/");
                    sw.WriteLine("chatpeerid=");
                    sw.WriteLine("globalprompt=Discord chat log.\\\n\tTopic: financial trolling");
                    sw.WriteLine("");
                    sw.WriteLine("# Values here can be overridden on a per-user basis");
                    sw.WriteLine("yappiness=0.01");
                    sw.WriteLine("maxtokens=4096");
                    sw.WriteLine("outtokens=200");
                    sw.WriteLine("#maxctxmessages=50");
                    sw.WriteLine("temperature=1.2");
                    sw.WriteLine("topp=0.85");
                    sw.WriteLine("repetitionpenalty=1.1");
                }
                else if (path.StartsWith($"{CONFIG_ROOT}/users/")) {
                    sw.WriteLine("token=");
                    sw.WriteLine("prompt=MeowMrrp is a gamer.\\\nHe is quite excited for the new crypto coin known as MeowCoin which they are discussing in the channel.\\\nMeow.");
                }
                else {
                    throw new Exception($"No default config file for path {path}!");
                }
                sw.Close();
            }

            List<UserData> spawnUsers = new();

            if (File.Exists($"{CONFIG_ROOT}/tokens.txt")) {
                Dictionary<string, string> values = ConfigFile.Read($"{CONFIG_ROOT}/tokens.txt");

                foreach (KeyValuePair<string, string> kvp in values) {
                    switch (kvp.Key) {
                        case "aiserver":
                            NotOpenAI.target = kvp.Value;
                            break;
                        case "globalprompt":
                            globalPrompt = kvp.Value;
                            break;
                        default:
                            globalValues.Add(kvp.Key, kvp.Value);
                            break;
                    }
                }
            }
            else {
                Directory.CreateDirectory($"{CONFIG_ROOT}/users/");
                WriteDefaultConfigFile($"{CONFIG_ROOT}/tokens.txt");
                WriteDefaultConfigFile($"{CONFIG_ROOT}/users/ExampleUser.txt");
                Console.WriteLine($"[Telegram Bot Error] Fill out the data in the {CONFIG_ROOT}/ directory and restart the application.");
                return null;
            }

            if (!ConfigValid(globalPrompt)) {
                Console.WriteLine($"[Telegram Bot Error] Tokens were not configured! Please fill out {CONFIG_ROOT}/tokens.txt");
                return null;
            }

            foreach (string file in Directory.GetFiles($"{CONFIG_ROOT}/users/")) {
                if (Path.GetFileName(file).StartsWith(".")) continue;

                Dictionary<string, string> values = ConfigFile.Read(file);

                UserData newUser = new(values);
                if (!ConfigValid(newUser.token)) {
                    Console.WriteLine($"[Telegram Bot Error] User wasn't fully configured! Please fill out {file}");
                    return null;
                }

                spawnUsers.Add(newUser);
            }

            if (spawnUsers.Count == 0) {
                Directory.CreateDirectory($"{CONFIG_ROOT}/users/");
                WriteDefaultConfigFile($"{CONFIG_ROOT}/users/ExampleUser.txt");
                Console.WriteLine($"Users were not configured! Please create users in {CONFIG_ROOT}/users/");
                return null;
            }

            DiscordBot[] bots = new DiscordBot[spawnUsers.Count];
            for (int i = 0; i < spawnUsers.Count; i++) {
                bots[i] = new(spawnUsers[i]);
                bots[i].Connect().GetAwaiter();
            }

            return bots;
        }

        DiscordBot(UserData userData) {
            this.userData = userData;

            individualPrompt = userData.prompt;
            foreach (var kvp in userData.otherValues)
                localValues.Add(kvp.Key, kvp.Value);
            // verify values
            GetConfigInt("chatpeerid");
            GetConfigFloat("yappiness");
            GetConfigInt("maxtokens");
            GetConfigInt("outtokens");
            GetConfigInt("maxctxmessages");
            GetConfigFloat("temperature");
            GetConfigFloat("topp");
            GetConfigFloat("repetitionpenalty");
        }

        public async Task Connect() {
            try {
                if (client != null) {
                    // this apparently can error and cause reconnect spam so we trycatch
                    // the Discord.Net vison is impeccable
                    try {
                        await client.StopAsync();
                    }
                    catch { }
                    try {
                        client.Dispose();
                    }
                    catch { }
                    client = null;
                }

                Log(null, "");
                Log(null, "   --- START LOG ---");
                Log(null, "Discord.NET version: " + DiscordConfig.Version);
                client = new DiscordSocketClient(
                    new DiscordSocketConfig {
                        AlwaysDownloadUsers = true,
                        LogLevel = LogSeverity.Info,
                        MessageCacheSize = 500,
                        DefaultRetryMode = RetryMode.AlwaysRetry,
                        HandlerTimeout = null, // TODO: set to 100 when message handling gets async
                        LargeThreshold = 250,
                        GatewayIntents = GatewayIntents.All & ~GatewayIntents.GuildPresences & ~GatewayIntents.GuildInvites & ~GatewayIntents.GuildScheduledEvents,
                    });

                //client.Ready += OnInit;
                client.Connected += OnInit;
                client.Disconnected += OnDisconnect;
                client.LoggedOut += OnLoggedOut;
                client.Log += OnLog;

                client.MessageReceived += OnMessageReceived;
                client.MessageUpdated += OnMessageUpdated;
                client.MessageDeleted += OnMessageDeleted;
                client.MessagesBulkDeleted += OnMessagesBulkDeleted;

                await client.LoginAsync(TokenType.User, userData.token);
                await client.StartAsync();
            }
            catch (ObjectDisposedException e) {
                Log(null, $"Idiot semaphore exception:\n{e}\nLiterally and physically rebooting...");
                Environment.Exit(69);
            }
            catch (Exception e) {
                Log(null, $"Connection error:\n{e}\nRetrying in 10 seconds...");
                Thread.Sleep(10000);
                Connect().GetAwaiter();
            }
        }

        public async Task OnInit() {
            if (client == null) {
                Log(null, "whar");
                return;
            }

            try {
                Log(null, "Init start");
                //await client.SetStatusAsync(DateTime.UtcNow.Subtract(sleepTime).TotalHours < 7.0 ? UserStatus.DoNotDisturb : UserStatus.Online);
                //await client.SetGameAsync(CurrentlyPlayingGame ?? "", null, ActivityType.Playing);
                Log(null, "trolled");
            }
            catch (Exception e) {
                Log(null, "Init exception:\n" + e);
            }
        }


        async Task OnDisconnect(Exception e) {
            Log(null, $"The bot disconnected ({e.Message})");
            Connect().GetAwaiter();
        }

        async Task OnLoggedOut() {
            Log(null, "Logged out...? What tf :troll:");
            Connect().GetAwaiter();
        }


        async Task OnLog(LogMessage arg) {
            if (arg.Source == "Rest") return; // ram leak pretty much when this was a gui :troll:

            Log(null, $"[{arg.Severity} - {arg.Source}] {arg.Message}");
        }

        DateTime sleepTime = DateTime.MinValue;

        public readonly List<string> possibleGames = new() { "Minecraft", "Terraria", "Brawlforming", "Bloons TD 6", "Rocket League", "Katamari Damacy REROLL", "We Love Katamari REROLL + Royal Reverie", "Muck", "Crab Game", "Super Mario 64", "B3313 1.0.6 hotfix 9", "B3313 0.1", "Motos Factory", "Trollface Quest 13", "Super Mario 74: Extreme Edition" };
        public string? CurrentlyPlayingGame { get; private set; }
        DateTime nextGame = DateTime.MinValue;
        DateTime nextRandomResponse = DateTime.MinValue;
        DateTime lastResponseTime = DateTime.MinValue;
        SocketMessage? lastMessage = null;
        float engagement = 0.0f;

        // tick that runs once about every 1 seconds
        public void UpdateGame() {
            double pow = DateTime.UtcNow.Subtract(lastResponseTime).TotalMinutes;
            if (Random.Shared.NextDouble() < Math.Pow(Yappiness, pow)) {
                if (lastMessage != null) {
                    Log(null, "suddenly, he appear.");
                    RespondTo(lastMessage, true).GetAwaiter().GetResult();
                }
            }
            /*
            Random r = new();
            engagement = Math.Max(engagement - 0.001f, 0.0f);

            if (DateTime.UtcNow.Subtract(sleepTime).TotalHours < 7.0) {
                // honk mimimimimimi
                if (CurrentlyPlayingGame != null) {
                    CurrentlyPlayingGame = null;
                    Log(null, "Sleep. No longer gaming");
                    client?.SetGameAsync("", null, ActivityType.Playing).GetAwaiter().GetResult();
                }
            }
            else {
                if (DateTime.UtcNow > nextGame) {
                    if (CurrentlyPlayingGame == null) {
                        if (r.NextDouble() > engagement) {
                            CurrentlyPlayingGame = possibleGames[r.Next(possibleGames.Count)];
                            Log(null, "New game: " + CurrentlyPlayingGame);

                            client?.SetGameAsync(CurrentlyPlayingGame, null, ActivityType.Playing).GetAwaiter().GetResult();
                        }
                    }
                    else {
                        // 1/4 chance to just continue gaming
                        if (r.Next(4) != 0) {
                            CurrentlyPlayingGame = null;
                            Log(null, "No longer gaming");
                            if (nextRandomResponse > DateTime.UtcNow.AddMinutes(2)) {
                                nextRandomResponse = DateTime.UtcNow.AddSeconds(30 + r.Next(90));
                            }

                            client?.SetGameAsync("", null, ActivityType.Playing).GetAwaiter().GetResult();
                        }
                    }

                    //nextGame = DateTime.UtcNow.AddSeconds(r.Next(20));
                    nextGame = DateTime.UtcNow.AddMinutes((r.Next(20) + 6) * (CurrentlyPlayingGame == null ? 1 : 10));
                }

                if (DateTime.UtcNow > nextRandomResponse) {
                    if (CurrentlyPlayingGame == null) {
                        if (lastMessage != null) {
                            RespondTo(lastMessage, true);
                        }
                    }

                    nextRandomResponse = DateTime.UtcNow.AddMinutes(3 + r.Next(31));
                }
            }*/
        }

        readonly Dictionary<ulong, object> serverLocks = new();
        int queuedRequests = 0;
        readonly Dictionary<ulong, string> messageOverrides = new();
        async Task RespondTo(SocketMessage msg, bool forceResponse) {
            if (client == null) {
                Log(null, "whar");
                return;
            }

            Log(null, "selfcheck");
            bool selfResponse = PostedByMe(msg);
            if (selfResponse && Random.Shared.NextDouble() > Yappiness) return;
            Log(null, ":D");
            
            try {
                try {
                    string serverName = "";
                    ulong serverId = 0;
                    string botUsername = client.CurrentUser.GlobalName;
                    string topic = "";
                    ChannelCache cache = GetChannelCache(msg.Channel.Id);
                    SocketGuild? guild = null;
                    if (msg.Channel is SocketTextChannel tc) {
                        guild = tc.Guild;
                        botUsername = GetDisplayName(tc.Guild.GetUser(client.CurrentUser.Id));
                        serverName = tc.Guild.Name;
                        serverId = tc.Guild.Id;
                        topic = string.IsNullOrEmpty(tc.Topic) ? "" : $" (channel topic: {tc.Topic})";
                    }

                    object? lockma;
                    lock (serverLocks) {
                        if (!serverLocks.TryGetValue(serverId, out lockma)) {
                            lockma = new object();
                            serverLocks.Add(serverId, new object());
                        }
                    }

                    if (!Monitor.TryEnter(lockma)) {
                        return;
                    }
                    Monitor.Exit(lockma);

                    if (queuedRequests > 0 && !forceResponse) {
                        Log(null, "my man theres already stuff queued we shouldnt spam");
                        return;
                    }
                    queuedRequests++;

                    Log(null, "lockma?");
                    lock (lockma) {
                        queuedRequests--;

                        bool theYapper = true;
                        while (theYapper) {
                            Log(null, "is the yapper");
                            theYapper = false; // reset by default

                            // Time to respond
                            Log(null, "boutta generate a response");
                            Thread.Sleep(Random.Shared.Next(900) + 500);

                            Log(null, "ok the scan is complete");
                            List<IMessage> rawMessages = new(cache.GetAllMessages(client, msg.Channel).GetAwaiter().GetResult());
                            long? maxctxmessages = GetConfigIntOptional("maxctxmessages");
                            if (maxctxmessages != null)
                                if (rawMessages.Count > maxctxmessages)
                                    rawMessages.RemoveRange(0, rawMessages.Count - (int)maxctxmessages);

                            // Get the last 100 messages for context
                            if (!rawMessages.Any(m => m.Id == msg.Id)) {
                                rawMessages.Add(msg);
                            }
                            List<IMessage> allMessagesOrdered = rawMessages.OrderBy(m => m.Id).ToList();
                            Dictionary<ulong, int> sentTimes = new();
                            // If any messages are spammed, then we collapse with the tag "(sent x times)"
                            for (int i = 0; i < allMessagesOrdered.Count - 1; i++) {
                                IMessage compareTo = allMessagesOrdered[i];

                                if (allMessagesOrdered[i].Embeds.Count > 0) {
                                    continue;
                                }

                                int count = 1;
                                int j = i + 1;
                                while (j < allMessagesOrdered.Count) {
                                    if (allMessagesOrdered[j].Content.ToLowerInvariant() != compareTo.Content.ToLowerInvariant() || allMessagesOrdered[j].Author.Id != compareTo.Author.Id) {
                                        break;
                                    }
                                    allMessagesOrdered.RemoveAt(j);
                                    count++;
                                }

                                if (count > 1) {
                                    sentTimes.Add(allMessagesOrdered[i].Id, count);
                                }
                            }

                            List<IMessage> messages = new();
                            // Keep the last 25 messages for sure
                            for (int i = allMessagesOrdered.Count - 1, j = 0; i >= 0 && j < 25; i--, j++) {
                                messages.Add(allMessagesOrdered[i]);
                                allMessagesOrdered.RemoveAt(i);
                            }

                            try {
                                // Completely nuke non-unique messages
                                allMessagesOrdered = allMessagesOrdered.GroupBy(m => m.Content.ToLowerInvariant()).Select(m => m.First()).OrderBy(m => m.Id).ToList();
                                // Remove the least 10% "relevant" messages. (8 messages)
                                double relevancyFloor = allMessagesOrdered.ConvertAll(m => Utils.MessageRelevance(allMessagesOrdered.Count - allMessagesOrdered.IndexOf(m), m.Content)).OrderBy(d => d)
                                                .ToArray()[(int)(allMessagesOrdered.Count * 0.1)];
                                allMessagesOrdered = allMessagesOrdered.Where(m => Utils.MessageRelevance(allMessagesOrdered.Count - allMessagesOrdered.IndexOf(m), m.Content) > relevancyFloor).OrderBy(m => m.Id).ToList();
                            }
                            catch { }

                            // Add 20 more messages
                            for (int i = allMessagesOrdered.Count - 1, j = 0; i >= 0 && j < 20; i--, j++) {
                                messages.Add(allMessagesOrdered[i]);
                                allMessagesOrdered.RemoveAt(i);
                            }

                            // Remove messages from the bot, to attempt to keep more of what others said (which is more important to the bot)
                            allMessagesOrdered = allMessagesOrdered.Where(m => m.Author.Id != client.CurrentUser.Id).ToList();

                            // Add 40 more messages
                            for (int i = allMessagesOrdered.Count - 1, j = 0; i >= 0 && j < 40; i--, j++) {
                                messages.Add(allMessagesOrdered[i]);
                                allMessagesOrdered.RemoveAt(i);
                            }

                            messages = messages.OrderBy(m => m.Id).ToList();

                            // The bot should attempt to generate text less often if it hasn't been active in the channel ("not paying attention"). Reduces cost :D
                            int countMessagesFromBot = messages.Count(m => m.Author.Id == client.CurrentUser.Id);
                            double baseChance = countMessagesFromBot * 0.15;
                            if (baseChance > 1.0) {
                                baseChance = 1.0 - (baseChance - 1.0) / 2.0;
                            }
                            double talkChance = Math.Min(1.0, 0.003313 + baseChance +
                            (messages.Count > 1 && messages[messages.Count - 2].Author.Id == client.CurrentUser.Id ? 0.4 : 0.0));
                            if (forceResponse) {
                                talkChance = 1.0;
                            }
                            else {
                                if (CurrentlyPlayingGame != null) {
                                    double reduce = 1.0 - 0.7 * Math.Min(DateTime.UtcNow.Subtract(lastResponseTime).TotalMinutes, 1.0);
                                    talkChance *= reduce;
                                }
                                // TODO: remove this 0.2 multiplication if repetition penalty becomes real
                                talkChance *= 0.2;
                            }

                            double sleepMultiplier = Math.Min(DateTime.UtcNow.Subtract(sleepTime).TotalHours / 8.0 + 0.0125, 1.0);
                            talkChance *= sleepMultiplier * sleepMultiplier;

                            /*if (new Random().NextDouble() > Math.Sqrt(Math.Max(talkChance, 0.0))) {
                                nextResponse = DateTime.UtcNow.AddSeconds(1 + new Random().NextDouble() * 5 * sleepMultiplier);
                                Log(null, "The bot failed RNG check. No talking for now");
                                return;
                            }*/
                            try {
                                sleepTime -= TimeSpan.FromMinutes(69);
                                //client.SetStatusAsync(UserStatus.Online).GetAwaiter().GetResult();
                            }
                            catch {
                                sleepTime = DateTime.MinValue + TimeSpan.FromMinutes(6969);
                            }

                            string[] chat = new string[messages.Count];
                            string previousDate = "";
                            Dictionary<string, ulong> users = new();
                            for (int i = 0; i < chat.Length; i++) {
                                string authorDisplayName = GetDisplayName(messages[i].Author);
                                if (authorDisplayName == null)
                                    Log(null, "????????????????????????????????????????????????? " + messages[i].Author.Id);

                                if (!users.ContainsKey(authorDisplayName)) {
                                    users.Add(authorDisplayName, messages[i].Author.Id);
                                }
                                /*if (messages[i].Author.Id != client.CurrentUser.Id) {
                                    authorDisplayName = $"{(messages[i].Author.IsBot ? "Bot" : "User")} {authorDisplayName}";
                                }*/

                                string said = "said:";
                                if (messages[i].Attachments.Count > 0) {
                                    string? monoextension = null;

                                    foreach (IAttachment attachment in messages[i].Attachments) {
                                        try {
                                            string extension = Path.GetExtension(attachment.Filename).ToLowerInvariant();

                                            if (monoextension == "") {
                                                monoextension = extension;
                                            }
                                            else if (extension != monoextension) {
                                                monoextension = null;
                                            }
                                        }
                                        catch { }
                                    }

                                    if (messages[i].Attachments.Count == 1) {
                                        IAttachment attachment = messages[i].Attachments.First();
                                        said = $"sent {attachment.Filename} ({Utils.FormatFilesize(messages[i].Attachments.First().Size)}):";
                                    }
                                    else {
                                        // multiple files
                                        if (monoextension == null) {
                                            said = $"sent {messages[i].Attachments.Count} files:";
                                        }
                                        else {
                                            said = $"sent {messages[i].Attachments.Count} {monoextension.ToUpperInvariant()}s:";
                                        }
                                    }
                                }

                                if (messages[i].Reference != null) {
                                    said = "replied:";
                                    if (messages[i].Reference.MessageId.IsSpecified) {
                                        IMessage? repliedTo = null;

                                        foreach (IMessage message in rawMessages) {
                                            if (message.Id == messages[i].Reference.MessageId.Value) {
                                                repliedTo = message;
                                                break;
                                            }
                                        }

                                        repliedTo ??= msg.Channel.GetCachedMessage(messages[i].Reference.MessageId.Value);
                                        if (repliedTo != null) {
                                            string replyDisplayName = GetDisplayName(repliedTo.Author);

                                            /*if (repliedTo.Author.Id != client.CurrentUser.Id) {
                                                replyDisplayName = $"{(repliedTo.Author.IsBot ? "Bot" : "User")} {replyDisplayName}";
                                            }*/

                                            said = $"replied to {replyDisplayName}:";
                                        }
                                    }
                                }

                                string currentDate = messages[i].CreatedAt.UtcDateTime.ToString("yyyy/MM/dd");
                                //said = said.TrimEnd(':') + " at " + (messages[i].CreatedAt.UtcDateTime.Hour + ":" + messages[i].CreatedAt.UtcDateTime.Minute.ToString().PadLeft(2, '0')) + ":";

                                if (sentTimes.ContainsKey(messages[i].Id)) {
                                    said = $"{said.TrimEnd(':')} ({sentTimes[messages[i].Id]}x):";
                                }

                                string embeds = "";

                                foreach (IAttachment attachment in messages[i].Attachments) {
                                    string embedStr = $"[{attachment.Filename}";
                                    bool append = false;
                                    try {
                                        string extension = Path.GetExtension(attachment.Filename).ToLowerInvariant();

                                        Console.WriteLine($"Attachment with extension {extension}");
                                        switch (extension.ToLowerInvariant()) {
                                            case ".png":
                                            case ".jpg":
                                            case ".jpeg":
                                            case ".gif":
                                            string meta = MediaReader.GetImageMeta(attachment);
                                            if (!string.IsNullOrWhiteSpace(meta)) {
                                                embedStr += $"\nText in image:\n{meta}";
                                                append = true;
                                            }
                                            break;
                                        }
                                        embedStr += "\n]";
                                    }
                                    catch (Exception e) {
                                        Console.WriteLine("Attachment fetch issue");
                                        Console.WriteLine(e);
                                    }

                                    if (append) {
                                    embeds += $"\n{embedStr}";
                                    }
                                }
                                
                                foreach (IEmbed embed in messages[i].Embeds) {
                                    string embedStr = "";
                                    if (embed.Author != null) {
                                        embedStr += $"Author: {embed.Author.Value}\n";
                                    }
                                    if (embed.Title != null) {
                                        embedStr += $"Title: {embed.Title}\n";
                                    }
                                    if (embed.Description != null) {
                                        embedStr += $"Description: {embed.Description}\n";
                                    }
                                    if (embed.Footer != null) {
                                        embedStr += $"Footer: {embed.Footer.Value}\n";
                                    }
                                    if (embed.Timestamp != null) {
                                        embedStr += $"Timestamp: {embed.Timestamp.Value}\n";
                                    }
                                    if (embed.Image.HasValue) {
                                        try {
                                            string meta = MediaReader.GetImageMeta(embed.Image.Value);
                                            if (!string.IsNullOrWhiteSpace(meta)) {
                                                embedStr += $"\nText in image:\n{meta}";
                                            }
                                        }
                                        catch (Exception e) {
                                            Console.WriteLine("Embed fetch issue");
                                            Console.WriteLine(e);
                                        }
                                    }
                                    if (embed.Thumbnail.HasValue) {
                                        try {
                                            string meta = MediaReader.GetImageMeta(embed.Thumbnail.Value);
                                            if (!string.IsNullOrWhiteSpace(meta)) {
                                            embedStr += $"\nText in image:\n{meta}";
                                            }
                                        }
                                        catch (Exception e) {
                                            Console.WriteLine("Embed fetch issue");
                                            Console.WriteLine(e);
                                        }
                                    }
                                    foreach (EmbedField field in embed.Fields) {
                                        embedStr += $"{field.Name}: {field.Value}\n";
                                    }

                                    embeds += $"\n[EMBED:\n{embedStr}]";
                                }

                                string content = DiscordParse(messages[i].Content, guild);
                                if (messageOverrides.TryGetValue(messages[i].Id, out string? value)) {
                                    content = value;
                                }
                                chat[i] = $"{authorDisplayName} {said} {content.Replace("\n", "\\n")}{embeds}";
                                if (currentDate != previousDate) {
                                    chat[i] = $"\n({currentDate})\n{chat[i]}";
                                    previousDate = currentDate;
                                }
                            }

                            // Get and "type" the response
                            bool mentioned = msg.MentionedUsers.Any(u => u.Id == client.CurrentUser.Id);

                            Eeper eeper = new();
                            eeper.StartEeper();

                            Dictionary<string, object> generateParams = new() {
                                {"maxTotalTokens", GetConfigInt("maxtokens")},
                                {"tokensPerStep", GetConfigInt("outtokens")},
                                {"temperature", GetConfigFloat("temperature")},
                                {"topP", GetConfigFloat("topp")},
                                {"repetitionPenalty", GetConfigFloat("repetitionpenalty")},
                            };

                            //Thread.Sleep((500 + new Random().Next(1000)) * (!string.IsNullOrEmpty(CurrentlyPlayingGame) ? 2 : 1));

                            var (responses, shouldContinueYap) = NotOpenAI.GetChatResponses(chat, $"{botUsername}", $"{globalPrompt}\n\n{individualPrompt}", "" /*forceResponse ? GetDisplayName(msg.Author) : (mentioned ? "\0" : "")*/,
                            generateParams);
                            theYapper = shouldContinueYap;
                            eeper.EepTo(500 + new Random().Next(1000));

                            int kirrms = 0;
                            foreach ((string mention, string message) _response in responses) {
                                string response = _response.message;
                                if (response == "leavema??") {
                                    if (guild != null) {
                                        IDMChannel channel = client.GetUser(186866145599422464).CreateDMChannelAsync().GetAwaiter().GetResult();
                                        channel.SendMessageAsync($"hello, am about to leavema {guild.Name}, please confirm this action by typing:\n\n`magic leave {guild.Id}`\n\nam leaving a dump of the current chat as a text file").GetAwaiter().GetResult();
                                        File.WriteAllText("leavema.txt", $"{string.Join("\n", chat)}\n\nand my opinion on the matter:\n{string.Join("\n", responses)}");
                                        channel.SendFileAsync("leavema.txt").GetAwaiter().GetResult();
                                    }
                                    break;
                                }

                                Log(null, "Typing " + response);
                                if (!string.IsNullOrEmpty(response)) {
                                    string originalResponse = response;
                                    engagement = Math.Min(engagement + 0.01f, 1.0f);

                                    List<string> meta = new();
                                    {
                                        // Use Regex to find all the content inside curly braces
                                        string pattern = @"\{(.*?)\}";

                                        // Extract matches and store them in the meta list
                                        response = Regex.Replace(response, pattern, match =>
                                        {
                                            meta.Add(match.Groups[1].Value);
                                            return ""; // Remove the matched part from the original string
                                        }).Trim();

                                        // Clean up extra spaces from the original string
                                        response = Regex.Replace(response, @"\s+", " ").Trim();
                                    }

                                    string textResponse = response;
                                    response = DiscordUnparse(response, users, guild);

                                    int typeTime = 0;
                                    foreach (char chr in textResponse) {
                                        if (char.IsLetterOrDigit(chr)) {
                                            typeTime += 1;
                                        }
                                        else {
                                            typeTime += 3;
                                        }
                                    }

                                    int typems = (500 + (int)(typeTime * 80 * (new Random().NextDouble() / 2.0 + 0.75)));

                                    if (kirrms < typems) {
                                        int slep = typems - kirrms;
                                        while (slep > 4000)
                                        {
                                            try {
                                                msg.Channel.TriggerTypingAsync().GetAwaiter().GetResult();
                                            } catch { }
                                            Thread.Sleep(4000);
                                            slep -= 4000;
                                        }

                                        try {
                                            msg.Channel.TriggerTypingAsync().GetAwaiter().GetResult();
                                        } catch { }
                                        Thread.Sleep(slep);
                                    }
                                    kirrms = 0;

                                    string uploadFile = "";
                                    foreach (string str in meta) {
                                        string[] split = str.Split(":");
                                        if (split.Length > 1) {
                                            string type = split[0].ToLowerInvariant();
                                            string data = str[(split[0].Length + 1)..];
                                            
                                            switch (type) {
                                                case "comfyui":
                                                    uploadFile = $"/tmp/funy{chat.GetHashCode()}.png";
                                                    ComfyUI.GenerateImageByPrompt(File.ReadAllText("workflow_api.json").Replace(":783088509829469,", $":{(ulong)Random.Shared.Next()},").Replace("windows xp wallpaper, catgirl, orange hair, orange ears, sexy", data), uploadFile, true);
                                                    break;
                                            }
                                        }
                                    }

                                    MessageReference? reference = null;
                                    string referenceAuthor = "";
                                    string referenceAuthorMention = "";

                                    if (_response.mention == "" && mentioned) {
                                        reference = new MessageReference(msg.Id, msg.Channel.Id);
                                        referenceAuthor = GetDisplayName(msg.Author);
                                        referenceAuthorMention = msg.Author.Mention;
                                        mentioned = false;
                                    }
                                    else {
                                        referenceAuthor = _response.mention;
                                        /*if (referenceAuthor.StartsWith("User ", StringComparison.OrdinalIgnoreCase)) {
                                            referenceAuthor = referenceAuthor[5..];
                                        }*/

                                        KeyValuePair<string, ulong> user = users.FirstOrDefault(user => user.Key == referenceAuthor, new KeyValuePair<string, ulong>("", 0));

                                        referenceAuthor = "";
                                        referenceAuthorMention = "";
                                        if (user.Value != 0) {
                                            for (int i = messages.Count - 1; i >= 0; i--) {
                                                if (messages[i].Author.Id == user.Value) {
                                                    reference = new MessageReference(messages[i].Id, messages[i].Channel.Id);
                                                    referenceAuthor = GetDisplayName(messages[i].Author);
                                                    referenceAuthorMention = messages[i].Author.Mention;
                                                    break;
                                                }
                                            }
                                        }
                                    }

                                    Discord.Rest.RestUserMessage newMsg;
                                    if (reference == null) {
                                        if (!string.IsNullOrWhiteSpace(uploadFile))
                                            newMsg = msg.Channel.SendFileAsync(uploadFile, response, true).GetAwaiter().GetResult();
                                        else
                                            newMsg = msg.Channel.SendMessageAsync(response, true).GetAwaiter().GetResult();
                                    }
                                    else {
                                        try {
                                            if (!string.IsNullOrWhiteSpace(uploadFile))
                                                newMsg = msg.Channel.SendFileAsync(uploadFile, response, true, null, null, false, null, reference).GetAwaiter().GetResult();
                                            else
                                                newMsg = msg.Channel.SendMessageAsync(response, true, null, null, null, reference).GetAwaiter().GetResult();
                                            mentioned = false;
                                        }
                                        catch {
                                            if (!textResponse.Contains($"@{referenceAuthor}")) {
                                                response = $"{referenceAuthorMention} {response}";
                                            }
                                            if (!string.IsNullOrWhiteSpace(uploadFile))
                                                newMsg = msg.Channel.SendFileAsync(uploadFile, response, true).GetAwaiter().GetResult();
                                            else
                                                newMsg = msg.Channel.SendMessageAsync(response, true).GetAwaiter().GetResult();
                                        }
                                    }

                                    if (newMsg != null) {
                                        messageOverrides.Add(newMsg.Id, originalResponse);
                                    }

                                    /*if (Utils.IsGoingToSleep(response) && serverId != 702467535307538502) {
                                        sleepTime = DateTime.UtcNow;
                                        client.SetStatusAsync(UserStatus.DoNotDisturb).GetAwaiter().GetResult();
                                        Log(null, "The bot is sleeping now :D");
                                    }*/

                                    if (!selfResponse)
                                        lastResponseTime = DateTime.UtcNow;
                                }
                            }

                            nextResponse = DateTime.UtcNow.AddSeconds(1 + new Random().NextDouble() * 7 / sleepMultiplier);
                        }
                    }
                }
                catch (System.Net.Http.HttpRequestException e) {
                    Log(null, "The bot is a failure. " + e);
                    msg.Channel.SendMessageAsync("There is some funny HTTP exception. Are you trying to spam me idiot? " + e.Message).GetAwaiter().GetResult();
                }
                catch (Exception e) {
                    Log(null, "The bot is a failure. " + e);
                    //if (!e.Message.Contains("Object reference not set"))
                    //    msg.Channel.SendMessageAsync("I am a failure. " + e.Message).GetAwaiter().GetResult();
                }
            }
            catch { }
        }

        // The rest of the bound methods
        async Task OnMessageReceived(SocketMessage msg) {
            GetChannelCache(msg.Channel.Id).UpdateMessage(msg);

            Log(null, $"President, a message has hit the OnMessageReceived method! ({msg.Channel.Id}) {GetDisplayName(msg.Author)}: {msg.Content}");
            if (client == null) {
                Log(null, "whar");
                return;
            }

            try {
                try {
                    if (msg.Author.Id == 186866145599422464 && msg.Content.StartsWith("magic", StringComparison.Ordinal)) {
                        string cmd = msg.Content.Remove(0, 6);
                        string[] args = cmd.Split(' ');
                        switch (args[0]) {
                            case "setgame":
                                if (args.Length == 1) {
                                    CurrentlyPlayingGame = null;
                                    client.SetGameAsync("", null, ActivityType.Playing).GetAwaiter().GetResult();
                                }
                                else {
                                    string game = cmd.Remove(0, 8);
                                    CurrentlyPlayingGame = game;
                                    client.SetGameAsync(CurrentlyPlayingGame, null, ActivityType.Playing).GetAwaiter().GetResult();
                                }
                                nextGame = DateTime.UtcNow.AddMinutes((new Random().Next(20) + 6) * (CurrentlyPlayingGame == null ? 1 : 10));
                                break;
                            case "sleep":
                                sleepTime = DateTime.UtcNow;
                                await client.SetStatusAsync(UserStatus.DoNotDisturb);
                                Log(null, "The bot is sleeping now :D");
                                break;
                            case "awaken":
                                sleepTime -= TimeSpan.FromMinutes(69 * 69);
                                await client.SetStatusAsync(UserStatus.Online);
                                break;
                            case "leave":
                                if (args.Length >= 2 && ulong.TryParse(args[1], out ulong leavemaId)) {
                                    await client.GetGuild(leavemaId).LeaveAsync();
                                    await msg.Channel.SendMessageAsync("troll complete, returning to HQ");
                                }
                                break;
                            case "leaknotes":
                            case "leakmemory":
                                await msg.Channel.SendMessageAsync(OpenAIButNot.DumpMemory());
                                break;
                        }
                        return;
                    }
                    // TODO: check if you can type in the channel. This is painful on discord.net for some reason

                    Log(null, $"The Great Comparison. {msg.Channel.Id} == {ChatPeerId} ?");
                    //if (msg.Channel.Id != ChatPeerId) return;
                    lastMessage = msg;

                    bool mentioned = msg.MentionedUsers.Any(u => u.Id == client.CurrentUser.Id) && msg.Author.Id != client.CurrentUser.Id;
                    /*if (!mentioned && DateTime.UtcNow < nextResponse) {
                        Log(null, "The bot is on randomness cooldown.");
                        return;
                    }*/

                    Log(null, $"President, a message is being threaded! ({msg.Channel.Id}) {GetDisplayName(msg.Author)}: {msg.Content}");
                    new Thread(() => {
                        RespondTo(msg, msg.MentionedUsers.Any(u => u.Id == client.CurrentUser.Id)).GetAwaiter().GetResult();
                    }).Start();

                    double sleepMultiplier = Math.Min(DateTime.UtcNow.Subtract(sleepTime).TotalHours / 8.0 + 0.0125, 1.0);
                    nextResponse = DateTime.UtcNow.AddSeconds(1 + new Random().NextDouble() * 7 / sleepMultiplier);
                }
                catch (Exception e) {
                    Log(null, "The bot is a failure. " + e);
                    //await msg.Channel.SendMessageAsync("I am a failure. " + e.Message);
                }
            } catch { }
        }


        private async Task OnMessageUpdated(Cacheable<IMessage, ulong> cacheable, SocketMessage msg, ISocketMessageChannel channel)
        {
            GetChannelCache(channel.Id).UpdateMessage(msg);
        }

        private async Task OnMessageDeleted(Cacheable<IMessage, ulong> cacheable1, Cacheable<IMessageChannel, ulong> cacheable2)
        {
            GetChannelCache(cacheable2.Id).DeleteMessage(cacheable1.Id);
        }

        private async Task OnMessagesBulkDeleted(IReadOnlyCollection<Cacheable<IMessage, ulong>> collection, Cacheable<IMessageChannel, ulong> cacheable)
        {
            ChannelCache cache = GetChannelCache(cacheable.Id);

            foreach (var msg in collection) {
                cache.DeleteMessage(msg.Id);
            }
        }
    }
}
