using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using FunctionApp.Dto.Elastic;
using FunctionApp.Services.Api.Interfaces;
using FunctionApp.Services.Elastic.Interfaces;
using FunctionApp.Services.Twm.Interfaces;
using FunctionApp.Services.Iguatemi.Interfaces;

namespace FunctionApp
{
    public class Function 
    {
        private readonly IElasticService _elasticService;
        private readonly IApiService _apiService;
        private readonly IIguatemiService _iguatemiService;
        private readonly ITwmService _twmService;

        public Function(IElasticService elasticService, IApiService apiService, IIguatemiService iguatemiService, ITwmService twmService) {

            _elasticService = elasticService;
            _apiService = apiService;
            _iguatemiService = iguatemiService;
            _twmService = twmService;
        }

        [FunctionName("Function")]
        public async Task RunAsync([TimerTrigger("%IntervaloTempoFuncao%")] TimerInfo myTimer)
        {
            var logProcessoBuilder = LogProcessoIntegracaoDto.Create();

            logProcessoBuilder.AdicionarMensagemTrace("Iniciando processo de integração para o cliente Iguatemi.");
            await _elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());

            await _apiService.ConsultarApiAsync(logProcessoBuilder, _elasticService, _iguatemiService, _twmService);

            logProcessoBuilder.AdicionarMensagemTrace("Finalizando processo de integração para o cliente Iguatemi.");
            await _elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());
        }
    }
}
