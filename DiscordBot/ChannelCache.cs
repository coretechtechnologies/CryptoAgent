using Discord;

// Async method lacks 'await' operators and will run synchronously
// Using a sync method requires us to return though, and requires a code change to allow awaiting so...
#pragma warning disable CS1998

namespace LeFunnyAI {
    public partial class DiscordBot {
        // A cache to prevent mass requests to the server.
        class ChannelCache {
            readonly ulong key;
            readonly List<IMessage> messages = new();
            bool canRedownload = true;
            const int DOWNLOAD_THRESHOLD = 30; // the point at which we have so few messages, should just download more

            public ChannelCache(ulong channel) {
                key = channel;
            }


            /// Inserts a message into the channel cache.
            /// If the channel contains this message by ID, it is overwritten, assumed to be edited.
            public void UpdateMessage(IMessage msg) {
                _Maintenance();

                for (int i = 0; i < messages.Count; i++) {
                    if (messages[i].Id >= msg.Id) {
                        if (messages[i].Id == msg.Id) {
                            messages[i] = msg;
                        }
                        else {
                            messages.Insert(i, msg);
                        }
                        
                        return;
                    }
                }
                
                // Add the message to the end if not found.
                messages.Add(msg);
            }

            public void DeleteMessage(ulong msgId) {
                _Maintenance();
                
                messages.RemoveAll(msg => msg.Id == msgId);
            }

            public async Task<IEnumerable<IMessage>> GetAllMessages(IDiscordClient client, IMessageChannel channel) {
                _Maintenance();

                if (messages.Count < DOWNLOAD_THRESHOLD && canRedownload) {
                    canRedownload = false;
                    await _DownloadMessages(client, channel);
                }
                else {
                    canRedownload = true;
                }

                return new List<IMessage>(messages);
            }


            async Task _DownloadMessages(IDiscordClient client, IMessageChannel channel) {
                Console.WriteLine("Downloading messages for channel " + key);
                await foreach (var messages in channel.GetMessagesAsync()) {
                    foreach (IMessage msg in messages) {
                        UpdateMessage(msg);
                    }
                }
            }

            void _Maintenance() {
            }
        }
    }
}