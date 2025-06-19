using FunctionApp.Dto.Elastic;
using FunctionApp.Services.Elastic.Interfaces;
using Nest;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FunctionApp.Services.Elastic
{
    public class ElasticService : IElasticService
    {
        public readonly IElasticClient _elastic;
        public ElasticService(IElasticClient elasticClient)
        {
            _elastic = elasticClient;
        }

        public async Task InserirLogProcessoIntegracaoAsync(
            LogProcessoIntegracaoDto logProcessoIntegracaoDto)
        {
            var resultado = "Sucesso";
            if (!string.IsNullOrEmpty(logProcessoIntegracaoDto.MensagemFalha))
                resultado = "Falha";

            await InserirLogProcessoAsync(
                logProcessoIntegracaoDto: logProcessoIntegracaoDto,
                resultado: resultado);
        }

        private async Task InserirLogProcessoAsync(
            LogProcessoIntegracaoDto logProcessoIntegracaoDto,
            string resultado)
        {
            logProcessoIntegracaoDto.Resultado = resultado;

            await _elastic.IndexDocumentAsync(logProcessoIntegracaoDto);
        }
    }
}

