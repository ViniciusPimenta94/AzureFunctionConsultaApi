using FunctionApp.Dto.Twm;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FunctionApp.Configurations
{
    public static class HttpClientConfiguration
    {
        private static readonly string IguatemiHeaderHttpClientId = Environment.GetEnvironmentVariable("Iguatemi:ApiConsultaClientId");
        private static readonly string IguatemiHeaderHttpAcessToken = Environment.GetEnvironmentVariable("Iguatemi:ApiConsultaAcessToken");
        private static readonly string IguatemiHeaderHttpSecret = Environment.GetEnvironmentVariable("Iguatemi:ApiConsultaSecret");

        private static readonly string TwmUrlAuth = Environment.GetEnvironmentVariable("Twm:UrlToken");
        private static readonly string TwmUsernameAuth = Environment.GetEnvironmentVariable("Twm:UsernameToken");
        private static readonly string TwmPasswordAuth = Environment.GetEnvironmentVariable("Twm:PasswordToken");

        public static void AddHttpClientConfiguration(this IServiceCollection services)
        {
            services.AddHttpClient("Iguatemi", config =>
            {
                config.Timeout = new TimeSpan(0, 10, 0);
                config.DefaultRequestHeaders.Add("client-id", IguatemiHeaderHttpClientId);
                config.DefaultRequestHeaders.Add("access-token", IguatemiHeaderHttpAcessToken);
                config.DefaultRequestHeaders.Add("secret", IguatemiHeaderHttpSecret);
            })
            .ConfigurePrimaryHttpMessageHandler(() => { return new SocketsHttpHandler { UseCookies = false }; })
            .AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(300 * retryAttempt)));

            services.AddHttpClient("TWM", config =>
            {
                config.Timeout = new TimeSpan(1, 0, 0);
                var token = GetToken(TwmUsernameAuth, TwmPasswordAuth, TwmUrlAuth).Result;
                config.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            })
            .ConfigurePrimaryHttpMessageHandler(() => { return new SocketsHttpHandler { UseCookies = false }; })
            .AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(300 * retryAttempt)));
        }

        private static async Task<string> GetToken(string username, string password, string url)
        {
            IEnumerable<KeyValuePair<string, string>> listKeyValuePair = new[]
            {
                new KeyValuePair<string,string>("grant_type", "password"),
                new KeyValuePair<string,string>("username", username),
                new KeyValuePair<string,string>("password", password)
            };

            using (var client = new HttpClient())
            {
                var result = client.PostAsync(url, new FormUrlEncodedContent(listKeyValuePair)).Result;
                return result.Content.ReadAsAsync<TokenAutenticacaoDto>().Result.AccessToken;
            }
        }

    }
}