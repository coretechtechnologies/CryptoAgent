namespace LeFunnyAI {
    class MainClass {
        public static int Main(string[] args) {
            if (!Directory.Exists("data/")) {
                Directory.CreateDirectory("data/");
                Console.WriteLine("hi welcome to le bot");
                Console.WriteLine("create directory ./data/tg/ to setup telegram, and create ./data/dc/ to set up discord :D");
                Console.WriteLine("rerun to get example files and stuff");
                return -1;
            }

            DiscordBot[]? dcBots = null;
            TelegramBot[]? tgBots = null;
            
            if (Directory.Exists("data/dc/")) {
                dcBots = DiscordBot._Connect();
                if (dcBots == null) return -1;
            }

            if (Directory.Exists("data/tg/")) {
                tgBots = TelegramBot._Connect();
                if (tgBots == null) return -1;
            }
            
            if (dcBots == null && tgBots == null) {
                Console.WriteLine("hi welcome to le bot");
                Console.WriteLine("create directory ./data/tg/ to setup telegram, and create ./data/dc/ to set up discord :D");
                Console.WriteLine("rerun to get example files and stuff");
                return -1;
            }

            while (true) {
                if (dcBots != null)
                    foreach (DiscordBot bot in dcBots)
                        bot.UpdateGame();
                if (tgBots != null)
                    foreach (TelegramBot bot in tgBots)
                        bot.UpdateGame();
                Thread.Sleep(1000);
            }

            //return 0;
        }
    }
}

