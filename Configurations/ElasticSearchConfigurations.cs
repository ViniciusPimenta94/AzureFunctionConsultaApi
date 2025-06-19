using FunctionApp.Dto.Elastic;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using System;

namespace FunctionApp.Configurations
{
    public static class ElasticSearchConfigurations
    {
        public static readonly string nomeIndex = Environment.GetEnvironmentVariable("Elastic:IndexNameFunction");
        public static readonly string urlConexao = Environment.GetEnvironmentVariable("Elastic:Url");
        public static readonly string username = Environment.GetEnvironmentVariable("Elastic:Username");
        public static readonly string password = Environment.GetEnvironmentVariable("Elastic:Password");

        public static void AddElasticSearchConfiguration(this IServiceCollection services)
        {
            var connectionSettings = new ConnectionSettings(new Uri(urlConexao));
            connectionSettings.DefaultMappingFor<LogProcessoIntegracaoDto>(map => map.IndexName(nomeIndex + "-" + DateTime.Now.ToString("yyyyMM")));
            connectionSettings = connectionSettings.BasicAuthentication(username, password);

            var elasticClient = new ElasticClient(connectionSettings);

            elasticClient.Indices.Create(
                nomeIndex + "-" + DateTime.Now.ToString("yyyyMM"),
               index => index.Map<LogProcessoIntegracaoDto>(x => x.AutoMap())
           );

            services.AddSingleton(typeof(IElasticClient), elasticClient);
        }
    }
}
