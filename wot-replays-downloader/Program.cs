using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace wot_replays_downloader
{
    class Program
    {
        private static readonly string replayFolder = Path.GetFullPath("replays");

        static void Main(string[] args)
        {
            Console.WriteLine($"The replays will be saved to '{replayFolder}'");
            Directory.CreateDirectory(replayFolder);

            int count = 0;
            int counter = 0;

            Task.WaitAll(GenerateURLsForCompatibilityReplays()
                .Select(url => GetDownloadUrlsFromWotreplays(url)
                    .ContinueWith(async t =>
                    {
                        if (t.Result.Count > 0)
                        {
                            Interlocked.Increment(ref count);
                            await DownloadReplay($"http://wotreplays.eu{t.Result[0]}", replayFolder);
                            Console.WriteLine($"finished {Interlocked.Increment(ref counter)}/{count}");
                        }
                    })).ToArray());
            Console.Read();
        }

        private static async Task DownloadReplay(string url, string destinationFolder)
        {
            WebRequest request = WebRequest.Create(url);
            using (WebResponse response = await request.GetResponseAsync())
            {
                var fileName = Path.GetFileName(response.ResponseUri.LocalPath);
                var responseStream = response.GetResponseStream();
                using (var fileStream = File.Create(Path.Combine(destinationFolder, fileName)))
                {
                    await responseStream.CopyToAsync(fileStream);
                }
            }
        }

        private static List<string> ReplayVersions = new List<string>
        {
            "59", "58", "57", "56", "55", "54", "53", "50", "51",
            "49", "48", "46", "44", "43", "41",
            "39", "38", "37", "36", "35", "34", "32", "31", "30",
            "29", "28", "27", "26", "25", "24", "23", "22",
            "16", "14", "15", "13", "19", "18",
            "20", "17", "21", "52", "47", "33", "45", "40", "42"
        };

        private static List<string> BattleTypes = new List<string>
        {
             "1", "2", "3", "6", "7", "8", "9", "10", "11", "12", "1009", "22", "24", "4-14", "5-13", "16-17"
        };

        private static IEnumerable<string> GenerateURLsForCompatibilityReplays()
        {
            return GenerateURLsForCompatibilityReplays(ReplayVersions, BattleTypes);
        }

        private static IEnumerable<string> GenerateURLsForCompatibilityReplays(List<string> replayVersions, List<string> battleTypes)
        {
            foreach (var version in replayVersions)
                foreach (var type in battleTypes)
                     yield return $"http://wotreplays.eu/site/index/version/{version}/battle_type/{type}/sort/xp.desc/";
        }

        private static int UrlCounter = 0;
        private static readonly int UrlCount = ReplayVersions.Count * BattleTypes.Count;

        private async static Task<List<string>> GetDownloadUrlsFromWotreplays(string url)
        {
            string pattern = @"\/site\/download\/([0-9]){7}"; // href="\/site\/download\/([0-9]){7}"

            WebRequest request = WebRequest.Create(url);
            using (WebResponse response = await request.GetResponseAsync())
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                MatchCollection matchList = Regex.Matches(await reader.ReadToEndAsync(), pattern);
                Console.WriteLine($"parsed {Interlocked.Increment(ref UrlCounter)}/{UrlCount} {url}");
                return matchList.Cast<Match>().Select(x => x.Value).ToList();
            }
        }
    }
}
