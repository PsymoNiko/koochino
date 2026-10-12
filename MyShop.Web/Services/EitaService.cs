using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MyShop.Application.Interfaces.Security;
using MyShop.Application.Services.Security;
using MyShop.Core.Interfaces;

namespace MyShop.Web.Services
{
    public class EitaService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IModuleConfigService _moduleConfig;
        private readonly ICurrentTenant _tenant;

        public EitaService(
            HttpClient httpClient,
            IConfiguration configuration,
            IModuleConfigService moduleConfig = null,
            ICurrentTenant tenant = null)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _moduleConfig = moduleConfig;
            _tenant = tenant;
        }

        async Task<(string Token, string ChatId)> ResolveAsync()
        {
            var siteId = ModuleConfigRead.SiteIdOf(_tenant);
            var token = await ModuleConfigRead.TryGetAsync(_moduleConfig, siteId, "Messaging", "Eita.BotToken", true)
                        ?? _configuration["Eita:BotToken"];
            return (token, _configuration["Eita:ChatId"]);
        }

        public async Task<bool> SendMessageAsync(string message, string? imageUrl = null)
        {
            try
            {
                var (botToken, chatId) = await ResolveAsync();

                if (string.IsNullOrEmpty(botToken) || string.IsNullOrEmpty(chatId))
                {
                    return false;
                }

                var url = $"https://eitaayar.ir/api/{botToken}/sendMessage";

                var payload = new
                {
                    chat_id = chatId,
                    text = message,
                    parse_mode = "HTML"
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SendPostAsync(string title, string content, string? summary = null, string? imageUrl = null)
        {
            try
            {
                var message = $"<b>{title}</b>\n\n";
                
                if (!string.IsNullOrEmpty(summary))
                {
                    message += $"{summary}\n\n";
                }
                
                message += content;

                if (!string.IsNullOrEmpty(imageUrl) && imageUrl.StartsWith("http"))
                {
                    return await SendPhotoAsync(imageUrl, message);
                }

                return await SendMessageAsync(message);
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> SendPhotoAsync(string photoUrl, string caption)
        {
            try
            {
                var (botToken, chatId) = await ResolveAsync();

                if (string.IsNullOrEmpty(botToken) || string.IsNullOrEmpty(chatId))
                {
                    return false;
                }

                var url = $"https://eitaayar.ir/api/{botToken}/sendPhoto";

                var payload = new
                {
                    chat_id = chatId,
                    photo = photoUrl,
                    caption = caption,
                    parse_mode = "HTML"
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}

