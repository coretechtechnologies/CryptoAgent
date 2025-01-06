using Discord;
using System.Diagnostics;

namespace LeFunnyAI {
    public partial class DiscordBot {
        public static class MediaReader {
            static readonly HttpClient mediaDownloadClient = new();
            const string CACHE_DIRECTORY = "/tmp/cyborcache/"; // TODO: config

            public static void Init() {
                if (!Directory.Exists(CACHE_DIRECTORY)) {
                    Directory.CreateDirectory(CACHE_DIRECTORY);
                }
            }

            static string _GetImageMeta(string url) {
                ProcessStartInfo psi;
                Process? p;

                Console.WriteLine($"ENTER _GetImageData {url}");
                string imagePath = $"{CACHE_DIRECTORY}/TEMPORARY_IMAGE";
                string pngPath = $"{CACHE_DIRECTORY}/TEMPORARY_IMAGE.png";
                var response = mediaDownloadClient.GetAsync(url).GetAwaiter().GetResult();
                
                FileStream fs = File.OpenWrite(imagePath);
                response.Content.ReadAsStream().CopyTo(fs);
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

            public static string GetImageMeta(IAttachment attachment) {
                Console.WriteLine($"ENTER GetImageData {attachment.Url}");
                string hashCode = ((uint)attachment.Url.GetHashCode()).ToString("X8");
                string path = $"{CACHE_DIRECTORY}/{hashCode}.txt";

                if (!File.Exists(path)) {
                    File.WriteAllText(path, _GetImageMeta(attachment.ProxyUrl));
                }

                Console.WriteLine($"EXIT GetImageData returning:");
                Console.WriteLine(File.ReadAllText(path));
                return File.ReadAllText(path);
            }

            public static string GetImageMeta(EmbedImage embed) {
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
            }
        }
    }
}