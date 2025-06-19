using FunctionApp.Builders.Elastic;
using FunctionApp.Dto.Twm;
using FunctionApp.Services.Elastic.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FunctionApp.Services.Twm.Interfaces
{
    public interface ITwmService
    {
        Task<IList<FaturasEmAndamentoTwmDto>> BuscarFaturasEmAndamento(LogProcessoIntegracaoBuilder logProcessoBuilder, IElasticService elasticService);
        Task SalvarRetornoIntegracaoAsync(RetornoIntegracaoTwmDto mensagemRetornoDto, LogProcessoIntegracaoBuilder logProcessoBuilder, IElasticService elasticService);
        Task IniciarProcessoIntegracao(int idFatura, LogProcessoIntegracaoBuilder logProcessoBuilder, IElasticService elasticService);
        Task AtualizarCampoCustomizadoTWMAsync(string identificadorFatura, IList<CampoCustomizadoChaveValor> listaCamposCustomizados, LogProcessoIntegracaoBuilder logProcessoBuilder, IElasticService elasticService);

    }
}
