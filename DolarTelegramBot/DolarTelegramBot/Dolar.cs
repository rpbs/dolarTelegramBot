using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DolarTelegramBot
{
    public class Dolar
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public Dolar(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        internal Root GitHubBranches { get; private set; }

        private static TelegramBotClient Bot;

        [FunctionName("Dolar")]
        public async Task RunAsync([TimerTrigger("*/5 * * * * *")]TimerInfo myTimer, ILogger log)
        {

            Bot = new TelegramBotClient("1862432027:AAGg5a6K9ADlcYvF8D3ehCAjbGdZCcvNmTc");

            var me = await Bot.GetMeAsync();
            Console.Title = me.Username;

            Bot.OnApiResponseReceived += Bot_OnApiResponseReceived;

            Bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync);

            Console.WriteLine($"Start listening for @{me.Username}");

            Console.ReadLine();
            //Bot.StopReceiving();

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://economia.awesomeapi.com.br/json/last/USD-BRL");

            var httpClient = _httpClientFactory.CreateClient();
            var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                using var contentStream = await httpResponseMessage.Content.ReadAsStreamAsync();

                GitHubBranches = await JsonSerializer.DeserializeAsync<Root>(contentStream);

                log.LogInformation(GitHubBranches.USDBRL.bid);
            }
        }

        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is Message message)
            {
                await botClient.SendTextMessageAsync(message.Chat, "Hello");
            }
        }

        async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is ApiRequestException apiRequestException)
            {
                await botClient.SendTextMessageAsync(123, apiRequestException.ToString());
            }
        }


        private ValueTask Bot_OnApiResponseReceived(
            ITelegramBotClient botClient, 
            Telegram.Bot.Args.ApiResponseEventArgs args, 
            System.Threading.CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
