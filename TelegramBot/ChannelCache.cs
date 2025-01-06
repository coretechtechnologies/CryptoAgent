using WTelegram;
using TL;

// Async method lacks 'await' operators and will run synchronously
// Using a sync method requires us to return though, and requires a code change to allow awaiting so...
#pragma warning disable CS1998

namespace LeFunnyAI {
    public partial class TelegramBot {
        // A cache to prevent mass requests to the server.
        class ChannelCache {
            readonly IPeerInfo key;
            readonly List<Message> messages = new();
            bool canRedownload = true;
            const int DOWNLOAD_THRESHOLD = 30; // the point at which we have so few messages, should just download more

            public ChannelCache(IPeerInfo channel) {
                key = channel;
            }


            /// Inserts a message into the channel cache.
            /// If the channel contains this message by ID, it is overwritten, assumed to be edited.
            public void UpdateMessage(Message msg) {
                _Maintenance();

                for (int i = 0; i < messages.Count; i++) {
                    if (messages[i].ID >= msg.ID) {
                        if (messages[i].ID == msg.ID) {
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

            public void DeleteMessage(int msgId) {
                _Maintenance();
                
                messages.RemoveAll(msg => msg.ID == msgId);
            }

            public async Task<IEnumerable<Message>> GetAllMessages(Client client) {
                _Maintenance();

                if (messages.Count < DOWNLOAD_THRESHOLD && canRedownload) {
                    canRedownload = false;
                    await _DownloadMessages(client);
                }
                else {
                    canRedownload = true;
                }

                return new List<Message>(messages);
            }


            async Task _DownloadMessages(Client client) {
                Console.WriteLine("Downloading messages for peer " + key);
                if (key is User user) {
                    Messages_MessagesBase list = await client.Messages_GetHistory(user);

                    foreach (var m in list.Messages) {
                        //Console.WriteLine("\t" + m);
                        if (m is Message msg) {
                            UpdateMessage(msg);
                        }
                    }
                }
                else if (key is ChatBase chatBase) {
                    Messages_MessagesBase list = await client.Messages_GetHistory(chatBase);

                    foreach (var m in list.Messages) {
                        //Console.WriteLine("\t" + m);
                        if (m is Message msg) {
                            UpdateMessage(msg);
                        }
                    }
                }
            }

            void _Maintenance() {
                // Remove messages past their TTL
                //Console.WriteLine("\tbefore purge: " + messages.Count);
                messages.RemoveAll(msg => (msg.flags & Message.Flags.has_ttl_period) != 0 && msg.date.AddSeconds(msg.ttl_period) > DateTime.Now);
                //Console.WriteLine("\tafter purge:  " + messages.Count);
            }
        }
    }
}