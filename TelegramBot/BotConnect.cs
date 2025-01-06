using WTelegram;
using TL;
using System.Text.RegularExpressions;

// Async method lacks 'await' operators and will run synchronously
// Using a sync method requires us to return though, and requires a code change to allow awaiting so...
#pragma warning disable CS1998

namespace LeFunnyAI {
    public partial class TelegramBot {
        const string CONFIG_ROOT = "data/tg";

        public static TelegramBot[]? _Connect() {
            MediaReader.Init();

            Helpers.Log = (lvl, str) => { }; // kill stdout spam

            static void WriteDefaultConfigFile(string path) {
                StreamWriter sw = new(path);
                if (path == $"{CONFIG_ROOT}/tokens.txt") {
                    sw.WriteLine("appid=");
                    sw.WriteLine("apphash=");
                    sw.WriteLine("aiserver=http://127.0.0.1:8000/");
                    sw.WriteLine("chatpeerid=");
                    sw.WriteLine("globalprompt=Telegram chat log.\\\n\tTopic: financial trolling");
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
                    sw.WriteLine("phone=");
                    sw.WriteLine("firstname=Meow");
                    sw.WriteLine("lastname=Mrrp");
                    sw.WriteLine("prompt=Meow Mrrp is a gamer.\\\nHe is quite excited for the new crypto coin known as MeowCoin which they are discussing in the channel.\\\nMeow.");
                }
                else {
                    throw new Exception($"No default config file for path {path}!");
                }
                sw.Close();
            }

            string appid = "", apphash = "";
            List<UserData> spawnUsers = new();

            if (File.Exists($"{CONFIG_ROOT}/tokens.txt")) {
                Dictionary<string, string> values = ConfigFile.Read($"{CONFIG_ROOT}/tokens.txt");

                foreach (KeyValuePair<string, string> kvp in values) {
                    switch (kvp.Key) {
                        case "appid":
                            appid = kvp.Value;
                            break;
                        case "apphash":
                            apphash = kvp.Value;
                            break;
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

            if (!ConfigValid(appid) || !ConfigValid(apphash) || !ConfigValid(globalPrompt)) {
                Console.WriteLine($"[Telegram Bot Error] Tokens were not configured! Please fill out {CONFIG_ROOT}/tokens.txt");
                return null;
            }

            foreach (string file in Directory.GetFiles($"{CONFIG_ROOT}/users/")) {
                if (Path.GetFileName(file).StartsWith(".")) continue;

                Dictionary<string, string> values = ConfigFile.Read(file);

                UserData newUser = new(values);
                if (!ConfigValid(newUser.phoneNumber)) {
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

            TelegramBot[] bots = new TelegramBot[spawnUsers.Count];
            for (int i = 0; i < spawnUsers.Count; i++) {
                bots[i] = new();
                bots[i].Connect(appid, apphash, spawnUsers[i]).GetAwaiter();
            }

            return bots;
        }

        public async Task Connect(string appid, string apphash, UserData userData) {
            try {
                string? Config(string what)
                {
                    switch (what)
                    {
                        case "api_id": return appid;
                        case "api_hash": return apphash;
                        case "phone_number": return userData.phoneNumber;
                        case "verification_code": Console.Write("Enter verification code for "+userData.registerFirstName+": "); return Console.ReadLine();
                        // if sign-up is required
                        case "first_name": Console.WriteLine($"Signing up {userData.registerFirstName} {userData.registerLastName}; it is recommended to set the account up manually!"); return userData.registerFirstName;
                        case "last_name": return userData.registerLastName;
                        default: return null;
                    }
                }
                string phoneHash = userData.phoneNumber;

                if (client != null) {
                    client.Dispose();
                    client = null;
                }
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

                Log(null, "");
                Log(null, "   --- START LOG ---");
                client = new Client(Config, File.Open(".sessionstore"+phoneHash, FileMode.OpenOrCreate, FileAccess.ReadWrite));
                updateManager = client.WithUpdateManager(Client_OnUpdate, ".updatestate"+phoneHash);
                currentUser = await client.LoginUserIfNeeded();

                Log(null, $"We are logged in as {currentUser.first_name} {currentUser.last_name} @{currentUser.username} (id {currentUser.id})");
                // We collect all infos about the users/chats so that updates can be printed with their names
                var dialogs = await client.Messages_GetAllDialogs(); // dialogs = groups/channels/users
                dialogs.CollectUsersChats(updateManager.Users, updateManager.Chats);
            }
            catch (Exception e) {
                Log(null, $"Connection error:\n{e}\nRetrying in 10 seconds...");
                Thread.Sleep(10000);
                _Connect();
            }
        }

        static readonly HashSet<string> unhandledEvents = new();
        async Task Client_OnUpdate(Update update)
        {
            switch (update)
            {
                case UpdateNewMessage unm: { if (unm.message is Message m) await OnChatMessageReceived(m); break; }
                case UpdateEditMessage uem: { if (uem.message is Message m) await OnChatMessageEdited(m); break; }
                // Note: UpdateNewChannelMessage and UpdateEditChannelMessage are also handled by above cases
                case UpdateDeleteChannelMessages udcm: { await OnChatMessagesDeleted(udcm.messages); break; }
                case UpdateDeleteMessages udm: { await OnChatMessagesDeleted(udm.messages); break; }
                /*case UpdateUserTyping uut: Console.WriteLine($"{User(uut.user_id)} is {uut.action}"); break;
                case UpdateChatUserTyping ucut: Console.WriteLine($"{Peer(ucut.from_id)} is {ucut.action} in {Chat(ucut.chat_id)}"); break;
                case UpdateChannelUserTyping ucut2: Console.WriteLine($"{Peer(ucut2.from_id)} is {ucut2.action} in {Chat(ucut2.channel_id)}"); break;
                case UpdateChatParticipants { participants: ChatParticipants cp }: Console.WriteLine($"{cp.participants.Length} participants in {Chat(cp.chat_id)}"); break;
                case UpdateUserStatus uus: Console.WriteLine($"{User(uus.user_id)} is now {uus.status.GetType().Name[10..]}"); break;
                case UpdateUserName uun: Console.WriteLine($"{User(uun.user_id)} has changed profile name: {uun.first_name} {uun.last_name}"); break;
                case UpdateUser uu: Console.WriteLine($"{User(uu.user_id)} has changed infos/photo"); break;*/
                default: {
                    // there are much more update types than the above example cases
                    string updateType = update.GetType().Name;
                    if (!unhandledEvents.Contains(updateType)) {
                        Log(null, $"Unhandled event {update.GetType().Name}; not printing this anymore");
                        unhandledEvents.Add(updateType);
                    }
                    break;
                }
            }
        }


        DateTime sleepTime = DateTime.MinValue;

        readonly HashSet<long> obtainedChats = new();
        readonly List<long> no = new() { 7285693141 };
        DateTime lastResponseTime = DateTime.UtcNow;
        Message? lastMessage = null;
        float engagement = 0.0f;

        // tick that runs once about every 1 seconds
        public void UpdateGame() {
            double pow = DateTime.UtcNow.Subtract(lastResponseTime).TotalMinutes;
            if (Random.Shared.NextDouble() < Math.Pow(Yappiness, pow)) {
                if (lastMessage != null && !PostedByMe(lastMessage)) {
                    Log(null, "suddenly, he appear.");
                    RespondTo(lastMessage, true).GetAwaiter().GetResult();
                }
            }
        }

        readonly Dictionary<long, object> channelLocks = new();
        readonly Dictionary<long, string> messageOverrides = new();
        async Task RespondTo(Message msg, bool encourageResponse) {
            if (client == null || updateManager == null) {
                Log(null, "whar");
                return;
            }

            if (no.Contains(msg.peer_id?.ID ?? 0)) return;

            Log(null, "self check " + msg.peer_id?.ID);
            bool selfResponse = PostedByMe(msg);
            double pow = DateTime.UtcNow.Subtract(lastResponseTime).TotalMinutes;
            if (selfResponse && Random.Shared.NextDouble() > Math.Pow(Yappiness, pow)) return;
            //if (CheckChannelId(msg.peer_id?.ID)) return;
            Log(null, "passed");

            try {
                try {
                    /*if (!encourageResponse && DateTime.UtcNow < nextResponse) {
                        Log(null, "The bot is on cooldown.");
                        return;
                    }*/

                    bool theYapper = true;
                    while (theYapper) {
                        theYapper = false; // reset by default

                        IPeerInfo channel = updateManager.UserOrChat(msg.peer_id);
                        ChannelCache cache = GetChannelCache(channel);
                        InputPeer channelSend = channel is User __u ? new InputPeerUser(__u.ID, __u.access_hash) : (
                            channel is Chat __chat ? new InputPeerChat(__chat.ID) : (
                                channel is Channel __ch ? new InputPeerChannel(__ch.ID, __ch.access_hash) : throw new Exception("weird type for " + channel)
                            )
                        );
                        if (channel is Chat ceow) {
                            lock (obtainedChats) {
                                if (!obtainedChats.Contains(channel.ID)) {
                                    obtainedChats.Add(channel.ID);
                                    var chatFull = client.Messages_GetFullChat(channel.ID).GetAwaiter().GetResult(); // the chat we want
                                    chatFull.CollectUsersChats(updateManager.Users, updateManager.Chats);
                                }
                            }
                        }
                        string leType = channel is User ? "User" : (
                            channel is Chat ? "Chat" : (
                                channel is Channel ? "Channel" : throw new Exception("weird type for " + channel)
                            )
                        );

                        string channelName = "";
                        long channelId = msg.peer_id.ID;
                        long authorId = msg.From?.ID ?? client.UserId;
                        string botUsername = GetDisplayName(client.User);
                        //string topic = "";

                        Log(null, $"channel: {channelId} (type is {leType}), user: {authorId}");

                        object? lockma;
                        lock (channelLocks) {
                            if (!channelLocks.TryGetValue(channelId, out lockma)) {
                                lockma = new object();
                                channelLocks.Add(channelId, lockma);
                            }
                        }
                        if (!Monitor.TryEnter(lockma)) {
                            return;
                        }
                        Monitor.Exit(lockma);
                        
                        lock (lockma) {
                            List<Message> rawMessages = new(cache.GetAllMessages(client).GetAwaiter().GetResult());
                            long? maxctxmessages = GetConfigIntOptional("maxctxmessages");
                            if (maxctxmessages != null)
                                if (rawMessages.Count > maxctxmessages)
                                    rawMessages.RemoveRange(0, rawMessages.Count - (int)maxctxmessages);
                                
                            // Time to respond
                            Log(null, "boutta generate a response");

                            // Get the last 100 messages for context
                            if (!rawMessages.Any(m => m.ID == msg.ID)) {
                                rawMessages.Add(msg);
                            }
                            List<Message> allMessagesOrdered = rawMessages.OrderBy(m => m.ID).ToList();
                            Dictionary<long, int> sentTimes = new();
                            // If any messages are spammed, then we collapse with the tag "(sent x times)"
                            for (int i = 0; i < allMessagesOrdered.Count - 1; i++) {
                                Message compareTo = allMessagesOrdered[i];

                                /* TODO: don't collapse if we have embeds
                                if (allMessagesOrdered[i].Embeds.Count > 0) {
                                    continue;
                                }*/

                                int count = 1;
                                int j = i + 1;
                                while (j < allMessagesOrdered.Count) {
                                    if (allMessagesOrdered[j].message.ToLowerInvariant() != compareTo.message.ToLowerInvariant() || allMessagesOrdered[j].from_id?.ID != compareTo.from_id?.ID) {
                                        break;
                                    }
                                    allMessagesOrdered.RemoveAt(j);
                                    count++;
                                }

                                if (count > 1) {
                                    sentTimes.Add(allMessagesOrdered[i].ID, count);
                                }
                            }

                            List<Message> messages = new();
                            // Keep the last 20 messages for sure
                            for (int i = allMessagesOrdered.Count - 1, j = 0; i >= 0 && j < 20; i--, j++) {
                                messages.Add(allMessagesOrdered[i]);
                                allMessagesOrdered.RemoveAt(i);
                            }

                            try {
                                // Completely nuke non-unique messages
                                allMessagesOrdered = allMessagesOrdered.GroupBy(m => m.message.ToLowerInvariant()).Select(m => m.First()).OrderBy(m => m.ID).ToList();
                                // Remove the least 10% "relevant" messages. (8 messages)
                                double relevancyFloor = allMessagesOrdered.ConvertAll(m => Utils.MessageRelevance(allMessagesOrdered.Count - allMessagesOrdered.IndexOf(m), m.message)).OrderBy(d => d)
                                                .ToArray()[(int)(allMessagesOrdered.Count * 0.1)];
                                allMessagesOrdered = allMessagesOrdered.Where(m => Utils.MessageRelevance(allMessagesOrdered.Count - allMessagesOrdered.IndexOf(m), m.message) > relevancyFloor).OrderBy(m => m.ID).ToList();
                            }
                            catch { }

                            // Add 20 more messages
                            for (int i = allMessagesOrdered.Count - 1, j = 0; i >= 0 && j < 20; i--, j++) {
                                messages.Add(allMessagesOrdered[i]);
                                allMessagesOrdered.RemoveAt(i);
                            }

                            // Remove messages from the bot, to attempt to keep more of what others said (which is more important to the bot)
                            allMessagesOrdered = allMessagesOrdered.Where(m => !PostedByMe(m)).ToList();

                            // Add 40 more messages
                            for (int i = allMessagesOrdered.Count - 1, j = 0; i >= 0 && j < 40; i--, j++) {
                                messages.Add(allMessagesOrdered[i]);
                                allMessagesOrdered.RemoveAt(i);
                            }

                            messages = messages.OrderBy(m => m.ID).ToList();

                            // The bot should attempt to generate text less often if it hasn't been active in the channel ("not paying attention"). Reduces cost :D
                            int countMessagesFromBot = messages.Count(m => PostedByMe(m));
                            double baseChance = countMessagesFromBot * 0.15;
                            if (baseChance > 1.0) {
                                baseChance = 1.0 - (baseChance - 1.0) / 2.0;
                            }
                            double talkChance = Math.Min(1.0, 0.003313 + baseChance +
                                (messages.Count > 1 && PostedByMe(messages[^2]) ? 0.4 : 0.0));
                            if (encourageResponse) {
                                talkChance = 1.0;
                            }
                            else {
                                /*if (CurrentlyPlayingGame != null) {
                                    double reduce = 1.0 - 0.7 * Math.Min(DateTime.UtcNow.Subtract(lastResponseTime).TotalMinutes, 1.0);
                                    talkChance *= reduce;
                                }*/
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
                                client.Account_UpdateStatus(false).GetAwaiter().GetResult();
                            }
                            catch {
                                sleepTime = DateTime.MinValue + TimeSpan.FromMinutes(6969);
                            }

                            string[] chat = new string[messages.Count];
                            string previousDate = "";
                            Dictionary<string, ulong> users = new();
                            for (int i = 0; i < chat.Length; i++) {
                                Message message = messages[i];

                                string authorDisplayName = "\0OOPSIE";
                                while (authorDisplayName == "\0OOPSIE") {
                                    authorDisplayName = "(anonymous)";
                                    if (messages[i].peer_id != null && updateManager.Users.TryGetValue(message.peer_id.ID, out User mew)) {
                                        authorDisplayName = GetDisplayName(mew);
                                    }
                                    if (messages[i].from_id != null)
                                    {
                                        User? u = null;
                                        if (!updateManager.Users.TryGetValue(message.From?.ID ?? 0, out u)) {
                                            /*if (message.from_id is PeerUser fromPeer) {
                                                var inputUser = new InputUser(fromPeer.user_id, channel.access_hash);
                                                var ub = client.Users_GetUsers(new[] { inputUser }).GetAwaiter().GetResult();
                                                if (ub.Length > 0) {
                                                    if (ub[0] is User user1) {
                                                        u = user1;
                                                    }
                                                }
                                            }*/
                                        }

                                        if (u != null) {
                                            authorDisplayName = GetDisplayName(u);
                                        }
                                    }
                                    else if (messages[i].post_author != null)
                                    {
                                        authorDisplayName = messages[i].post_author;
                                    }
                                }

                                /*if (!users.ContainsKey(authorDisplayName)) {
                                    users.Add(authorDisplayName, messages[i].Author.Id);
                                }*/

                                string said = "said:";

                                /* TODO: reply tags
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

                                    repliedTo ??= msg.Channel.GetMessageAsync(messages[i].Reference.MessageId.Value).GetAwaiter().GetResult();
                                    if (repliedTo != null) {
                                        string replyDisplayName = GetDisplayName(repliedTo.Author);

                                        if (repliedTo.Author.Id != client.CurrentUser.Id) {
                                        replyDisplayName = $"{(repliedTo.Author.IsBot ? "Bot" : "User")} {replyDisplayName}";
                                        }

                                        said = $"replied to {replyDisplayName}:";
                                    }
                                    }
                                }*/

                                string currentDate = messages[i].date.ToString("yyyy/MM/dd");
                                //said = said.TrimEnd(':') + " at " + (messages[i].CreatedAt.UtcDateTime.Hour + ":" + messages[i].CreatedAt.UtcDateTime.Minute.ToString().PadLeft(2, '0')) + ":";

                                if (sentTimes.ContainsKey(messages[i].ID)) {
                                    said = $"{said.TrimEnd(':')} ({sentTimes[messages[i].ID]}x):";
                                }

                                string embeds = "";

                                if (messages[i].media != null) {
                                    if (messages[i].media is MessageMediaPhoto media) {
                                        if (media.photo is Photo photo) {
                                            string embedStr = $"[Photo";
                                            bool append = false;
                                            try {
                                                string meta = MediaReader.GetImageMeta(client, photo);
                                                if (!string.IsNullOrWhiteSpace(meta)) {
                                                    embedStr += $"\nText in image:\n{meta}";
                                                    append = true;
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
                                    }
                                }
                                
                                /* TODO: embeds
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
                                }*/

                                string content = messages[i].message;
                                if (messageOverrides.ContainsKey(messages[i].id)) {
                                    content = messageOverrides[messages[i].id];
                                }
                                chat[i] = $"{authorDisplayName} {said} {content.Replace("\n", "\\n")}{embeds}";
                                if (currentDate != previousDate) {
                                    chat[i] = $"\n({currentDate})\n{chat[i]}";
                                    previousDate = currentDate;
                                }
                            }

                            // Get and "type" the response
                            bool mentioned = (msg.flags & Message.Flags.mentioned) != 0;

                            Eeper eeper = new();
                            eeper.StartEeper();

                            Dictionary<string, object> generateParams = new() {
                                {"maxTotalTokens", GetConfigInt("maxtokens")},
                                {"tokensPerStep", GetConfigInt("outtokens")},
                                {"temperature", GetConfigFloat("temperature")},
                                {"topP", GetConfigFloat("topp")},
                                {"repetitionPenalty", GetConfigFloat("repetitionpenalty")},
                            };

                            var (responses, shouldContinueYap) = NotOpenAI.GetChatResponses(chat, $"{botUsername}", $"{globalPrompt}\n\n{individualPrompt}", "" /*forceResponse ? GetDisplayName(msg.Author) : (mentioned ? "\0" : "")*/,
                            generateParams);
                            //responses = new (string mention, string message)[0];
                            theYapper = shouldContinueYap;
                            eeper.EepTo(500 + new Random().Next(1000));

                            int kirrms = 0;
                            Message? respondTo = null;
                            foreach ((string mention, string message) _response in responses) {
                                string response = _response.message;
                                /* TODO: leavema
                                if (response == "leavema??") {
                                    if (guild != null) {
                                        IDMChannel channel = client.GetUser(186866145599422464).CreateDMChannelAsync().GetAwaiter().GetResult();
                                        channel.SendMessageAsync($"hello, am about to leavema {guild.Name}, please confirm this action by typing:\n\n`magic leave {guild.Id}`\n\nam leaving a dump of the current chat as a text file").GetAwaiter().GetResult();
                                        File.WriteAllText("leavema.txt", $"{string.Join("\n", chat)}\n\nand my opinion on the matter:\n{string.Join("\n", responses)}");
                                        channel.SendFileAsync("leavema.txt").GetAwaiter().GetResult();
                                    }
                                    break;
                                }*/

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
                                    //response = DiscordUnparse(response, users, guild);

                                    int typeTime = 0;
                                    foreach (char chr in textResponse) {
                                        if (char.IsLetterOrDigit(chr)) {
                                            typeTime += 1;
                                        }
                                        else {          
                                            typeTime += 3;
                                        }
                                    }

                                    int typems = 500 + (int)(typeTime * 80 * (new Random().NextDouble() / 2.0 + 0.75));

                                    if (kirrms < typems) {
                                        int slep = typems - kirrms;
                                        while (slep > 4000)
                                        {
                                            try {
                                                client.Messages_SetTyping(channelSend, new SendMessageTypingAction()).GetAwaiter().GetResult();
                                            } catch { }
                                            Thread.Sleep(4000);
                                            slep -= 4000;
                                        }

                                        try {
                                            client.Messages_SetTyping(channelSend, new SendMessageTypingAction()).GetAwaiter().GetResult();
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

                                    UpdatesBase update;
                                    if (!string.IsNullOrEmpty(uploadFile)) {
                                        var tol = client.UploadFileAsync(uploadFile).GetAwaiter().GetResult();
                                        var inputMedia = new InputMediaUploadedPhoto
                                        {
                                            file = tol,
                                            //mime_type = "image/png", // Adjust MIME type accordingly (e.g., "application/pdf")
                                            //attributes = new[] { new DocumentAttributeFilename { file_name = Path.GetFileName("tol.png") } }
                                        };
                                        update = client.Messages_SendMedia(channelSend, inputMedia, response, Helpers.RandomLong()).GetAwaiter().GetResult();
                                    }
                                    else {
                                        update = client.Messages_SendMessage(channelSend, response, Helpers.RandomLong()).GetAwaiter().GetResult();
                                    }

                                    if (update is UpdateShortSentMessage updateShortSentMessage) {
                                        Message m = new() {
                                            id = updateShortSentMessage.id,
                                            peer_id = msg.peer_id,
                                            from_id = new PeerUser() { user_id = client.UserId },
                                            message = response,
                                            ttl_period = updateShortSentMessage.ttl_period,
                                            date = updateShortSentMessage.date,
                                            media = updateShortSentMessage.media
                                        };
                                        messageOverrides.Add(m.id, originalResponse);
                                        cache.UpdateMessage(m);
                                        lastMessage = m;
                                        respondTo = m;
                                    }
                                    else {
                                        //Console.WriteLine($"{update.UpdateList.Length} new updates dropped gg");
                                        foreach (Update u in update.UpdateList) {
                                            //Console.WriteLine("\t" + u);
                                            if (u is UpdateNewMessage updateNewMessage) {
                                                if (updateNewMessage.message is Message meow) {
                                                    messageOverrides.Add(meow.id, originalResponse);
                                                    cache.UpdateMessage(meow);
                                                    lastMessage = meow;
                                                    respondTo = meow;
                                                }
                                            }
                                        }
                                    }
                                    /* TODO: mentions by the bot
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
                                        if (referenceAuthor.StartsWith("User ", StringComparison.OrdinalIgnoreCase)) {
                                            referenceAuthor = referenceAuthor[5..];
                                        }

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

                                    if (reference == null) {
                                        msg.Channel.SendMessageAsync(response, true).GetAwaiter().GetResult();
                                    }
                                    else {
                                        try {
                                            msg.Channel.SendMessageAsync(response, true, null, null, null, reference).GetAwaiter().GetResult();
                                            mentioned = false;
                                        }
                                        catch {
                                            if (!textResponse.Contains($"@{referenceAuthor}")) {
                                            response = $"{referenceAuthorMention} {response}";
                                            }
                                            msg.Channel.SendMessageAsync($"{response}", true).GetAwaiter().GetResult();
                                        }
                                    }*/

                                    /* TODO: eeping
                                    if (Utils.IsGoingToSleep(response) && serverId != 702467535307538502) {
                                        sleepTime = DateTime.UtcNow;
                                        client.SetStatusAsync(UserStatus.DoNotDisturb).GetAwaiter().GetResult();
                                        Log(null, "The bot is sleeping now :D");
                                    }*/

                                    if (!selfResponse)
                                        lastResponseTime = DateTime.UtcNow;
                                }
                            }

                            nextResponse = DateTime.UtcNow.AddSeconds(1 + new Random().NextDouble() * 7 / sleepMultiplier);

                            if (respondTo != null) {
                                RespondTo(respondTo, false).GetAwaiter();
                            }
                        }
                    }
                }
                catch (Exception e) {
                    Log(null, "The bot is a failure. " + e);
                }
            }
            catch { }
        }

        // The rest of the bound methods
        async Task OnChatMessageReceived(Message msg) {
            if (client == null || updateManager == null) {
                Log(null, "whar");
                return;
            }

            IPeerInfo channel = updateManager.UserOrChat(msg.peer_id);
            GetChannelCache(channel).UpdateMessage(msg);

            try {
                /* TODO: magic commands
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
                }*/
                // TODO: check if you can type in the channel. This is painful on discord.net for some reason

                Log(null, "the peer id is: " + msg.peer_id?.ID);
                if (no.Contains(msg.peer_id?.ID ?? 0)) return;
                //if (CheckChannelId(msg.peer_id?.ID)) return;
                lastMessage = msg;

                bool mentioned = (msg.flags & Message.Flags.mentioned) != 0 && !PostedByMe(msg);

                new Thread(() => {
                    RespondTo(msg, false).GetAwaiter().GetResult();

                    double sleepMultiplier = Math.Min(DateTime.UtcNow.Subtract(sleepTime).TotalHours / 8.0 + 0.0125, 1.0);
                    nextResponse = DateTime.UtcNow.AddSeconds(1 + new Random().NextDouble() * 7 / sleepMultiplier);
                }).Start();
            }
            catch (Exception e) {
                Log(null, "The bot is a failure. " + e);
            }
        }

        // The rest of the bound methods
        async Task OnChatMessageEdited(Message msg) {
            if (client == null || updateManager == null) {
                Log(null, "whar");
                return;
            }

            IPeerInfo channel = updateManager.UserOrChat(msg.peer_id);
            GetChannelCache(channel).UpdateMessage(msg);
        }

        // The rest of the bound methods
        async Task OnChatMessagesDeleted(IEnumerable<int> msgIds) {
            if (client == null || updateManager == null) {
                Log(null, "whar");
                return;
            }

            foreach (int id in msgIds) {
                foreach (var kvp in channelCache) {
                    kvp.Value.DeleteMessage(id);
                }
            }
        }
    }
}
