using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using FunctionApp.Services.Elastic.Interfaces;
using FunctionApp.Services.Elastic;
using FunctionApp.Services.Api.Interfaces;
using FunctionApp.Services.Api;
using FunctionApp.Services.Iguatemi.Interfaces;
using FunctionApp.Services.Twm.Interfaces;
using FunctionApp.Services.Iguatemi;
using FunctionApp.Services.Twm;
using FunctionApp.Configurations;

[assembly: FunctionsStartup(typeof(FunctionApp.Startup))]

namespace FunctionApp
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<IElasticService, ElasticService>();
            builder.Services.AddSingleton<IApiService, ApiService>();
            builder.Services.AddSingleton<IIguatemiService, IguatemiService>();
            builder.Services.AddSingleton<ITwmService, TwmService>();
            builder.Services.AddElasticSearchConfiguration();
            builder.Services.AddHttpClientConfiguration();
        }
    }
}
