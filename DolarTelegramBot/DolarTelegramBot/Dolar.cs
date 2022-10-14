using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DolarTelegramBot
{
    public class Dolar
    {
        readonly IHttpClientFactory _httpClientFactory;
        readonly IMemoryCache _cache;
        readonly long CHAT_ID;
        readonly string BOT_KEY;
        readonly TelegramBotClient Bot;
        public Dolar(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            CHAT_ID = long.Parse(Environment.GetEnvironmentVariable("CHAT_ID"));
            BOT_KEY = Environment.GetEnvironmentVariable("BOT_KEY");
            Bot = new TelegramBotClient(BOT_KEY);
        }

        internal Root GitHubBranches { get; private set; }

        [FunctionName("Dolar")]
        public async Task RunAsync([TimerTrigger("* */30 * * * *")] TimerInfo myTimer, ILogger log)
        {

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            Chat c = new()
            {
                Id = CHAT_ID
            };

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://economia.awesomeapi.com.br/json/last/USD-BRL");
            var httpClient = _httpClientFactory.CreateClient();
            var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);

            log.LogInformation("httpResponseMessage.IsSuccessStatusCode = " + httpResponseMessage.IsSuccessStatusCode);

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                using var contentStream = await httpResponseMessage.Content.ReadAsStreamAsync();

                GitHubBranches = await JsonSerializer.DeserializeAsync<Root>(contentStream);

                var valorAtual = GitHubBranches.USDBRL.bid;

                _cache.TryGetValue("DOLAR", out string fromCache);

                log.LogWarning($"valorAtual = {valorAtual}");
                log.LogWarning($"fromCache = {fromCache}");

                if (fromCache != valorAtual)
                {
                    await Bot.SendTextMessageAsync(c, "Valor atual: R$" + valorAtual);
                    _cache.Set("DOLAR", valorAtual, TimeSpan.FromDays(1));
                }                
            } else
                await Bot.SendTextMessageAsync(c, "Erro na consulta");

        }
    }
}
