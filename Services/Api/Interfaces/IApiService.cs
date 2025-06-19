using FunctionApp.Builders.Elastic;
using FunctionApp.Services.Elastic.Interfaces;
using FunctionApp.Services.Iguatemi.Interfaces;
using FunctionApp.Services.Twm.Interfaces;
using System.Threading.Tasks;

namespace FunctionApp.Services.Api.Interfaces
{
    public interface IApiService
    {
        Task ConsultarApiAsync(LogProcessoIntegracaoBuilder logProcessoBuilder, IElasticService elasticService, IIguatemiService iguatemiService, ITwmService twmService);
    }
}
