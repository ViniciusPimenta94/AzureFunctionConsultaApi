using FunctionApp.Dto.Elastic;
using System.Threading.Tasks;

namespace FunctionApp.Services.Elastic.Interfaces
{
    public interface IElasticService
    {
        Task InserirLogProcessoIntegracaoAsync(LogProcessoIntegracaoDto logTransacaoAuditoriaDto);
    }
}
