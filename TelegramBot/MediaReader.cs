using WTelegram;
using System.Diagnostics;
using TL;

namespace LeFunnyAI {
    public partial class TelegramBot {
        public static class MediaReader {
            const string CACHE_DIRECTORY = "/tmp/cyborcache_tg/";

            public static void Init() {
                if (!Directory.Exists(CACHE_DIRECTORY)) {
                    Directory.CreateDirectory(CACHE_DIRECTORY);
                }
            }

            static string _GetImageMeta(Client client, Photo photo) {
                ProcessStartInfo psi;
                Process? p;

                Console.WriteLine($"ENTER _GetImageData {photo.ID}");
                string imagePath = $"{CACHE_DIRECTORY}/TEMPORARY_IMAGE";
                string pngPath = $"{CACHE_DIRECTORY}/TEMPORARY_IMAGE.png";

                FileStream fs = File.OpenWrite(imagePath);
                client.DownloadFileAsync(photo, fs).GetAwaiter().GetResult();
                fs.Close();

                Console.WriteLine($"Executing convert '{imagePath[0]}' -resize 200% '{pngPath}'");
                psi = new(){FileName = "convert"};
                psi.ArgumentList.Add($"{imagePath}[0]");
                psi.ArgumentList.Add($"-resize");
                psi.ArgumentList.Add($"200%");
                psi.ArgumentList.Add($"{pngPath}");
                p = Process.Start(psi);
                p?.WaitForExit();

                Console.WriteLine($"Executing tesseract '{pngPath}' '{CACHE_DIRECTORY}TEMPORARY'");
                psi = new(){FileName = "tesseract"};
                psi.ArgumentList.Add(pngPath);
                psi.ArgumentList.Add($"{CACHE_DIRECTORY}TEMPORARY");
                p = Process.Start(psi);
                p?.WaitForExit();

                //File.Delete(imagePath);

                string tempFile = $"{CACHE_DIRECTORY}/TEMPORARY.txt";
                if (!File.Exists(tempFile)) {
                    Console.WriteLine($"EXIT _GetImageData tesseract failed");
                    return "";
                }
                string data = File.ReadAllText(tempFile);
                File.Delete(tempFile);
                Console.WriteLine($"EXIT _GetImageData returning:");
                Console.WriteLine(data);
                return data;
            }

            public static string GetImageMeta(Client client, Photo photo) {
                Console.WriteLine($"ENTER GetImageData {photo.ID}");
                string hashCode = photo.ID.ToString("X16");
                string path = $"{CACHE_DIRECTORY}/{hashCode}.txt";

                if (!File.Exists(path)) {
                    File.WriteAllText(path, _GetImageMeta(client, photo));
                }

                Console.WriteLine($"EXIT GetImageData returning:");
                Console.WriteLine(File.ReadAllText(path));
                return File.ReadAllText(path);
            }

            /*public static string GetImageMeta(EmbedImage embed) {
                Console.WriteLine($"ENTER GetImageData {embed.Url}");
                string hashCode = ((uint)embed.Url.GetHashCode()).ToString("X8");
                string path = $"{CACHE_DIRECTORY}/{hashCode}.txt";

                if (!File.Exists(path)) {
                    File.WriteAllText(path, _GetImageMeta(embed.ProxyUrl));
                }

                Console.WriteLine($"EXIT GetImageData returning:");
                Console.WriteLine(File.ReadAllText(path));
                return File.ReadAllText(path);
            }

            public static string GetImageMeta(EmbedThumbnail embed) {
                Console.WriteLine($"ENTER GetImageData {embed.Url}");
                string hashCode = ((uint)embed.Url.GetHashCode()).ToString("X8");
                string path = $"{CACHE_DIRECTORY}/{hashCode}.txt";

                if (!File.Exists(path)) {
                    File.WriteAllText(path, _GetImageMeta(embed.ProxyUrl));
                }

                Console.WriteLine($"EXIT GetImageData returning:");
                Console.WriteLine(File.ReadAllText(path));
                return File.ReadAllText(path);
            }*/
        }
    }
}