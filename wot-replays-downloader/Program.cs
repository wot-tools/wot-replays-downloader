using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace wot_replays_downloader
{
    class Program
    {
        string replayFolder = Path.GetFullPath("replays");

        static void Main(string[] args)
        {
            Console.WriteLine($"The replays will be saved to '{replayFolder}'");

            List <string> downloadUrls = new List<string>();
            var wotReplaysUrls = GenerateURLsForCompatibilityReplays();

            double currentCount = 1;
            int totalCount = wotReplaysUrls.Count;

            foreach (var url in wotReplaysUrls)
            {
                var progress = currentCount / totalCount;
                ReportProgress(progress, $"Scraping {url}");
                var relativeDownloadUrls = GetDownloadUrlsFromWotreplays(url);

                if (relativeDownloadUrls.Count > 0)
                    downloadUrls.Add($"http://wotreplays.eu{relativeDownloadUrls[0]}");

                currentCount++;
            }

            ReportProgress(0, "Starting downloads");
            double totalDownloads = Convert.ToDouble(downloadUrls.Count);
            int downloadCount = 1;

            foreach (var url in downloadUrls)
            {
                ReportProgress(downloadCount / totalDownloads, $"Downloading replay {downloadCount:N0}/{totalDownloads:N0}");

                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    var fn = Path.GetFileName(response.ResponseUri.LocalPath);
                    string basePath = $@"{replayFolder}";
                    var responseStream = response.GetResponseStream();
                    using (var fileStream = File.Create(Path.Combine(basePath, fn)))
                    {
                        responseStream.CopyTo(fileStream);
                    }
                }

                downloadCount++;
            }

            ReportProgress(100, "All replays have been downloaded");
        }

        private static List<string> GetReplayVersions()
        {
            return new List<string>() { "59", "58", "57", "56", "55", "54", "53", "50", "51",
                                        "49", "48", "46", "44", "43", "41",
                                        "39", "38", "37", "36", "35", "34", "32", "31", "30",
                                        "29", "28", "27", "26", "25", "24", "23", "22",
                                        "16", "14", "15", "13", "19", "18",
                                        "20", "17", "21", "52", "47", "33", "45", "40", "42" };
        }

        private static List<string> GetBattleTypes()
        {
            return new List<string>() { "1", "2", "3", "6", "7", "8", "9", "10", "11", "12", "1009", "22", "24", "4-14", "5-13", "16-17" };
        }

        private static List<string> GenerateURLsForCompatibilityReplays()
        {
            return GenerateURLsForCompatibilityReplays(GetReplayVersions(), GetBattleTypes());
        }

        private static List<string> GenerateURLsForCompatibilityReplays(List<string> replayVersions, List<string> battleTypes)
        {
            List<string> urls = new List<string>();

            foreach (var version in replayVersions)
            {
                foreach (var type in battleTypes)
                {
                    urls.Add($"http://wotreplays.eu/site/index/version/{version}/battle_type/{type}/sort/xp.desc/");
                }
            }

            return urls;
        }

        private static List<string> GetDownloadUrlsFromWotreplays(string url)
        {
            string pattern = @"\/site\/download\/([0-9]){7}"; // href="\/site\/download\/([0-9]){7}"

            using (WebClient client = new WebClient())
            {
                string htmlCode = client.DownloadString(url);

                MatchCollection matchList = Regex.Matches(htmlCode, pattern);
                return matchList.Cast<Match>().Select(x => x.Value).ToList();
            }
        }

        private static void ReportProgress(double progress, string text)
        {
            Console.WriteLine($"{progress:P2} {text}");
        }
    }
}
