// <copyright file="Downloader.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2022 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Samples.ParallelCountSites
{
    internal class Downloader
    {
        private readonly HttpClient _client = new HttpClient { MaxResponseContentBufferSize = 1_000_000 };

        private List<string> _urlList = new List<string>();
        // {
        //    // "https://mvp.microsoft.com/fr-fr/PublicProfile/5003325?fullName=Christophe%20Nasarre",
        //    // "https://mvp.microsoft.com/fr-fr/PublicProfile/5003493?fullName=Kevin%20Gosse",
        //    // "https://mvp.microsoft.com/fr-fr/PublicProfile/5002437?fullName=Konrad%20Kokosa",
        //    // "https://mvp.microsoft.com/fr-fr/PublicProfile/5001937?fullName=Adrien%20Clerbois",
        //    // "https://mvp.microsoft.com/fr-fr/PublicProfile/5000999?fullName=Alexandre%20Mutel",
        //    // "https://mvp.microsoft.com/fr-fr/PublicProfile/4025637?fullName=Bruno%20Thierry%20Boucard",
        //    // "https://mvp.microsoft.com/fr-fr/PublicProfile/5000641?fullName=Christophe%20Peugnet",
        //    // "https://mvp.microsoft.com/fr-fr/PublicProfile/5536?fullName=Richard%20Clark",
        //    "http://this.does.not.exist.com/sorry",  // should be an error
        //    "http://github.com/Maoni0",              // --> redirect to HTTPS
        //    "https://www.okx.com/api/v5/trade/order",  // --> Unauthorized
        // };

        public Downloader(Scenario scenario)
        {
            if ((scenario & Scenario.Redirect) == Scenario.Redirect)
            {
                _urlList.Add("http://github.com/Maoni0");
            }

            if ((scenario & Scenario.Invalid) == Scenario.Invalid)
            {
                _urlList.Add("http://this.does.not.exist.com/sorry");
            }

            if ((scenario & Scenario.Blog) == Scenario.Blog)
            {
                // might not trigger DNS resolution within Datadog
                // _urlList.Add("https://www.datadoghq.com/blog/engineering/dotnet-continuous-profiler-part-4/");
                // so use an external site instead
                _urlList.Add("https://github.com/Maoni0");
            }
        }

        public async Task SumPagesSize(int iterations)
        {
            for (int i = 0; i < iterations; i++)
            {
                await SumPagesSizeAsync();
            }
        }

        private async Task SumPagesSizeAsync()
        {
            var stopwatch = Stopwatch.StartNew();

            IEnumerable<Task<(string Output, int Size)>> downloadTasksQuery =
                from url in _urlList
                select ProcessUrlAsync(url, _client);

            Task<(string Output, int Size)>[] downloadTasks = downloadTasksQuery.ToArray();

            (string Output, int Size)[] lengths = await Task.WhenAll(downloadTasks);
            stopwatch.Stop();

            int total = lengths.Sum(site => site.Size);
            foreach (var length in lengths)
            {
                await Console.Out.WriteLineAsync(length.Output);
            }

            await Console.Out.WriteLineAsync($"\nTotal bytes returned:  {total:#,#}");
            await Console.Out.WriteLineAsync($"\nElapsed time:          {stopwatch.Elapsed}\n");
        }

        private async Task<(string Output, int Size)> ProcessUrlAsync(string url, HttpClient client)
        {
            try
            {
                byte[] byteArray = await client.GetByteArrayAsync(url);
                var size = byteArray.Length;
                return ($"{url,-90} {size,10:#,#}", size);
            }
            catch (Exception ex)
            {
                return ($"{url,-90} failed: {ex.Message}", 0);
            }
        }
    }
}
