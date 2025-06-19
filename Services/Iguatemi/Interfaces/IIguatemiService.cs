using FunctionApp.Builders.Elastic;
using FunctionApp.Dto.Twm;
using FunctionApp.Services.Elastic.Interfaces;
using FunctionApp.Services.Twm.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionApp.Services.Iguatemi.Interfaces
{
    public interface IIguatemiService
    {
        Task ConsultarApiFolhaClienteAsync(List<FaturasEmAndamentoIguatemiDto> faturasEmAndamento, LogProcessoIntegracaoBuilder logProcessoBuilder, IElasticService elasticService, ITwmService twmService);
    }
}
