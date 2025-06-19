using FunctionApp.Builders.Elastic;
using FunctionApp.Dto.Twm;
using FunctionApp.Services.Api.Interfaces;
using FunctionApp.Services.Elastic.Interfaces;
using FunctionApp.Services.Iguatemi.Interfaces;
using FunctionApp.Services.Twm.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FunctionApp.Services.Api
{
    public class ApiService : IApiService
    {
        //Status que após validar a fatura enviada na caixa de e-mail, deve-se consultar pagamento ou aprovação
        const int idStatusIntegracaoAguardandoPagamentoAprovacaoPedido = (int)IntegrationStatus.AguardandoPagamentoPedido;

        const string campoCustomizadoFaturaFV60 = "Numero FV60 Integracao";
        const string campoCustomizadoFaturaFolhaServico = "Numero Folha de Servico Integracao";
        const string campoCustomizadoFaturaPedido = "Numero Pedido Integracao";
        const string campoCustomizadoEmpresa = "Empresa SAP";

        public async Task ConsultarApiAsync(LogProcessoIntegracaoBuilder logProcessoBuilder, IElasticService elasticService, IIguatemiService iguatemiService, ITwmService twmService)
        {
            var faturasEmAndamentoTwm = await twmService.BuscarFaturasEmAndamento(logProcessoBuilder, elasticService);
            var faturasEmAndamentoIguatemi = await ObterListaFaturasIguatemi(logProcessoBuilder, elasticService, faturasEmAndamentoTwm.ToList());

            await iguatemiService.ConsultarApiFolhaClienteAsync(faturasEmAndamentoIguatemi, logProcessoBuilder, elasticService, twmService);
        }

        private async Task<List<FaturasEmAndamentoIguatemiDto>> ObterListaFaturasIguatemi(LogProcessoIntegracaoBuilder logProcessoBuilder, IElasticService elasticService, List<FaturasEmAndamentoTwmDto> faturasEmAndamentoTwmDto)
        {
            faturasEmAndamentoTwmDto.RemoveAll(p => p.IdStatusIntegracao != idStatusIntegracaoAguardandoPagamentoAprovacaoPedido);

            var faturasEmAndamentoIguatemi = new List<FaturasEmAndamentoIguatemiDto>();

            try
            {
                faturasEmAndamentoIguatemi = faturasEmAndamentoTwmDto
                    .Select(fatura => ConverterParaFaturaIguatemi(fatura))
                    .ToList();
            }
            catch (Exception ex)
            {
                var mensagemErro = $"Erro inesperado ao fazer o parseamento das faturas em andamento no TWM.\n{ex.Message}";
                logProcessoBuilder.AdicionarMensagemFalha(mensagemErro);
                await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());
                throw new Exception(mensagemErro);
            }

            return faturasEmAndamentoIguatemi;
        }

        private FaturasEmAndamentoIguatemiDto ConverterParaFaturaIguatemi(FaturasEmAndamentoTwmDto fatura)
        {
            var faturaIguatemi = new FaturasEmAndamentoIguatemiDto
            {
                IdFatura = fatura.IdFatura,
                IdStatusIntegracao = fatura.IdStatusIntegracao,
                IdentificadorFatura = fatura.IdentificadorFatura,
                DataUltimoStatus = fatura.DataUltimoStatus
            };

            if (fatura.CampoCustomizados != null)
            {
                foreach (var campoCustomizado in fatura.CampoCustomizados)
                {
                    if (campoCustomizado.Valor != null)
                    {
                        switch (campoCustomizado.DescricaoCampo)
                        {
                            case campoCustomizadoEmpresa:
                                faturaIguatemi.Empresa = campoCustomizado.Valor;
                                break;
                            case campoCustomizadoFaturaFV60:
                            case campoCustomizadoFaturaFolhaServico:
                            case campoCustomizadoFaturaPedido:
                                faturaIguatemi.NumeroFV60Integracao = long.Parse(campoCustomizado.Valor);
                                break;
                        }
                    }
                }
            }

            return faturaIguatemi;
        }
    }
}