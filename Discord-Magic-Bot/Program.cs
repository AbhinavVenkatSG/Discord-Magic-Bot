using System;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;

namespace CardBot
{
    class Program
    {
        private DiscordSocketClient _client;
        private const string Token = "YOUR_DISCORD_BOT_TOKEN_HERE"; // ⚠️ Replace with your bot token
        private static readonly HttpClient http = new HttpClient();

        static async Task Main(string[] args) => await new Program().MainAsync();

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            _client.Log += Log;
            _client.MessageReceived += HandleCommandAsync;

            await _client.LoginAsync(TokenType.Bot, Token);
            await _client.StartAsync();

            // Block until program closes
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async Task HandleCommandAsync(SocketMessage message)
        {
            if (message.Author.IsBot) return;

            if (message.Content.StartsWith("!card "))
            {
                string cardName = message.Content.Substring(6).Trim();

                if (string.IsNullOrWhiteSpace(cardName))
                {
                    await message.Channel.SendMessageAsync("Please provide a card name.");
                    return;
                }

                string apiUrl = $"https://api.scryfall.com/cards/named?fuzzy={Uri.EscapeDataString(cardName)}";

                try
                {
                    var response = await http.GetStringAsync(apiUrl);
                    var json = JObject.Parse(response);

                    string name = json["name"]?.ToString() ?? "Unknown";
                    string set = json["set_name"]?.ToString() ?? "Unknown set";
                    string type = json["type_line"]?.ToString() ?? "Unknown type";
                    string oracle = json["oracle_text"]?.ToString() ?? "No text available";
                    string image = json["image_uris"]?["normal"]?.ToString() ?? "";
                    string priceCad = json["prices"]?["cad"]?.ToString() ?? "N/A";

                    var embed = new EmbedBuilder()
                        .WithTitle(name)
                        .WithDescription($"{type}\n\n{oracle}")
                        .WithFooter($"Set: {set} | Price: {priceCad} CAD")
                        .WithColor(Color.Blue);

                    if (!string.IsNullOrEmpty(image))
                        embed.WithThumbnailUrl(image);

                    await message.Channel.SendMessageAsync(embed: embed.Build());
                }
                catch (Exception ex)
                {
                    await message.Channel.SendMessageAsync($"Error: Could not find card `{cardName}`.");
                    Console.WriteLine(ex);
                }
            }
        }
    }
}
