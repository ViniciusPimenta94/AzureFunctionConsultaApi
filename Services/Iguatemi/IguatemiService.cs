using FunctionApp.Builders.Elastic;
using FunctionApp.Dto;
using FunctionApp.Dto.Elastic;
using FunctionApp.Dto.RetornoApi;
using FunctionApp.Dto.Twm;
using FunctionApp.Services.Elastic.Interfaces;
using FunctionApp.Services.Iguatemi.Interfaces;
using FunctionApp.Services.Twm.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FunctionApp.Services.Iguatemi
{
    public class IguatemiService : IIguatemiService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly string prazoExpiracaoConsultaNaApi = Environment.GetEnvironmentVariable("Iguatemi:TempoMaximoEmMesParaExpirarConsultaApi");

        private readonly string idResponsavelIntegracaoTwm = Environment.GetEnvironmentVariable("Twm:IdResponsavelIntegracao");

        private readonly string urlApiConsultaFv60 = Environment.GetEnvironmentVariable("Iguatemi:ApiConsultaUrlFv60");
        private readonly string urlApiConsultaFolhaServico = Environment.GetEnvironmentVariable("Iguatemi:ApiConsultaUrlFolhaDeServico");

        private readonly string campoUsuarioRequest = Environment.GetEnvironmentVariable("Iguatemi:UsuarioApi");
        private readonly string campoEmpresaRequest = Environment.GetEnvironmentVariable("Iguatemi:EmpresaApi");

        private const string campoCustomizadoDataAgendamento = "Data Agendamento";
        private const string campoCustomizadoDataPagamento = "Data do Pagamento";
        private const string campoCustomizadoPago = "Pago";

        private const string dataPadraoApiIguatemi = "ddd MMM dd HH:mm:ss UTC yyyy";
        private const string dataPadrao = "dd/MM/yyyy";

        private const string mensagemErro = "não existe";

        public IguatemiService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task ConsultarApiFolhaClienteAsync(List<FaturasEmAndamentoIguatemiDto> faturasEmAndamento, LogProcessoIntegracaoBuilder logProcessoBuilder, IElasticService elasticService, ITwmService twmService)
        {
            logProcessoBuilder.AdicionarMensagemTrace("Iniciando processo de consultas nas APIs do cliente.");
            await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());

            int prazoExpiracaoConsulta = int.Parse(prazoExpiracaoConsultaNaApi);

            if (faturasEmAndamento.Count == 0)
            {
                logProcessoBuilder.AdicionarMensagemAlerta("Nenhuma fatura em andamento disponível para consulta");
                await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());
            }

            foreach (var faturaDto in faturasEmAndamento)
            {
                logProcessoBuilder = LogProcessoIntegracaoDto.Create();

                try
                {
                    if (string.IsNullOrEmpty(faturaDto.Empresa))
                    {
                        var mensagemErro = "Erro: A fatura não possui Empresa SAP vinculada a Conta Aglutinada.";
                        logProcessoBuilder.AdicionarMensagemFalha(mensagemErro);
                        await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());
                        await twmService.SalvarRetornoIntegracaoAsync(new RetornoIntegracaoTwmDto(faturaDto.IdFatura, int.Parse(idResponsavelIntegracaoTwm), IntegrationStatus.Erro, mensagemErro), logProcessoBuilder, elasticService);
                    }

                    if (faturaDto.NumeroFV60Integracao == null && faturaDto.NumeroFolhaServico == null && faturaDto.NumeroPedido == null)
                    {
                        var mensagemErro = "Erro: A fatura não possui Número FV60, Numero Folha de Serviço ou Numero de Pedido vinculado a fatura.";
                        logProcessoBuilder.AdicionarMensagemFalha(mensagemErro);
                        await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());
                        await twmService.SalvarRetornoIntegracaoAsync(new RetornoIntegracaoTwmDto(faturaDto.IdFatura, int.Parse(idResponsavelIntegracaoTwm), IntegrationStatus.Erro, mensagemErro), logProcessoBuilder, elasticService);
                    }

                    if (DateTime.Today.Year * 12 + DateTime.Today.Month - (faturaDto.DataUltimoStatus.Year * 12 + faturaDto.DataUltimoStatus.Month) > prazoExpiracaoConsulta)
                    {

                        var mensagemErro = "Erro: Tempo limite de consulta excedido, a fatura precisa ser re-integrada.";
                        logProcessoBuilder.AdicionarMensagemFalha(mensagemErro);
                        await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());
                        await twmService.SalvarRetornoIntegracaoAsync(new RetornoIntegracaoTwmDto(faturaDto.IdFatura, int.Parse(idResponsavelIntegracaoTwm), IntegrationStatus.Erro, mensagemErro), logProcessoBuilder, elasticService);
                    }

                    if (faturaDto.NumeroFV60Integracao != null)
                    {
                        logProcessoBuilder.AdicionarMensagemTrace($"Realizando consulta na API FV60 para buscar status de pagamento do pedido da fatura {faturaDto.IdentificadorFatura}.");
                        await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());

                        var consultaApiFvDto = await RequisicaoApiClienteFv60Async(faturaDto.IdentificadorFatura, faturaDto.NumeroFV60Integracao.Value, faturaDto.Empresa, logProcessoBuilder, elasticService);
                        if (consultaApiFvDto.Mensagem.Contains(mensagemErro))
                        {
                            var mensagemRetornoDto = new RetornoIntegracaoTwmDto(faturaDto.IdFatura, int.Parse(idResponsavelIntegracaoTwm), IntegrationStatus.Erro, consultaApiFvDto.Mensagem);
                            await twmService.SalvarRetornoIntegracaoAsync(mensagemRetornoDto, logProcessoBuilder, elasticService);
                        }
                        else if (ValidarPreenchimentoDto(consultaApiFvDto.Dados))
                        {
                            var mensagemRetornoDto = ValidarRespostaConsultaApiFV60(consultaApiFvDto, faturaDto, logProcessoBuilder, elasticService, twmService).Result;

                            await twmService.SalvarRetornoIntegracaoAsync(mensagemRetornoDto, logProcessoBuilder, elasticService);
                        }
                        continue;
                    }

                    if (faturaDto.NumeroFolhaServico != null)
                    {
                        logProcessoBuilder.AdicionarMensagemTrace($"Realizando consulta na API de Folha de Serviço para buscar status de pagamento do pedido da fatura {faturaDto.IdentificadorFatura}.");
                        await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());

                        var consultaApiDto = await RequisicaoApiClienteFolhaServicoAsync(faturaDto.IdentificadorFatura, faturaDto.NumeroFolhaServico.Value, logProcessoBuilder, elasticService);
                        if (consultaApiDto.Mensagem.Contains(mensagemErro))
                        {
                            var mensagemRetornoDto = new RetornoIntegracaoTwmDto(faturaDto.IdFatura, int.Parse(idResponsavelIntegracaoTwm), IntegrationStatus.Erro, consultaApiDto.Mensagem);
                            await twmService.SalvarRetornoIntegracaoAsync(mensagemRetornoDto, logProcessoBuilder, elasticService);
                        }
                        else if (ValidarPreenchimentoDto(consultaApiDto.Dados))
                        {
                            var mensagemRetornoDto = ValidarRespostaConsultaApiFolhaServico(consultaApiDto, faturaDto, logProcessoBuilder, elasticService, twmService).Result;

                            await twmService.SalvarRetornoIntegracaoAsync(mensagemRetornoDto, logProcessoBuilder, elasticService);
                        }
                        continue;
                    }

                    if (faturaDto.NumeroPedido != null)
                    {
                        logProcessoBuilder.AdicionarMensagemTrace($"Realizando consulta na API de Folha de Serviço para buscar status de aprovação do pedido da fatura {faturaDto.IdentificadorFatura}.");
                        await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());

                        var consultaApiDto = await RequisicaoApiClienteFolhaServicoAsync(faturaDto.IdentificadorFatura, faturaDto.NumeroFolhaServico.Value, logProcessoBuilder, elasticService);
                        if (consultaApiDto.Mensagem.Contains(mensagemErro))
                        {
                            var mensagemRetornoDto = new RetornoIntegracaoTwmDto(faturaDto.IdFatura, int.Parse(idResponsavelIntegracaoTwm), IntegrationStatus.Erro, consultaApiDto.Mensagem);
                            await twmService.SalvarRetornoIntegracaoAsync(mensagemRetornoDto, logProcessoBuilder, elasticService);
                        }
                        else if (ValidarPreenchimentoDto(consultaApiDto.Dados))
                        {
                            var mensagemRetornoDto = ValidarRespostaConsultaApiPedido(consultaApiDto, faturaDto, logProcessoBuilder, elasticService).Result;

                            await twmService.SalvarRetornoIntegracaoAsync(mensagemRetornoDto, logProcessoBuilder, elasticService);

                            if (mensagemRetornoDto.IntegrationStatus == IntegrationStatus.Sucesso)
                                await twmService.IniciarProcessoIntegracao(mensagemRetornoDto.InvoiceId, logProcessoBuilder, elasticService);
                        }
                        continue;
                    }

                }
                catch (Exception e)
                {
                    await twmService.SalvarRetornoIntegracaoAsync(new RetornoIntegracaoTwmDto(faturaDto.IdFatura, int.Parse(idResponsavelIntegracaoTwm), IntegrationStatus.Erro, e.Message), logProcessoBuilder, elasticService);
                }
            }
        }

        private async Task<RetornoApiDto> RequisicaoApiClienteFv60Async(string identificadorFatura, long numeroPedido, string empresa, LogProcessoIntegracaoBuilder logProcessoBuilder, IElasticService elasticService)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("Iguatemi");

                var pedidoConsultaDto = ObterPedidoConsultaFvDto(numeroPedido, empresa);
                var json = JsonSerializer.Serialize(pedidoConsultaDto);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(urlApiConsultaFv60, content);
                var retornoContent = response.Content.ReadAsStringAsync().Result;

                if (!response.IsSuccessStatusCode)
                    new Exception($"StatusCode: {response.StatusCode}\nConteúdo resposta: {response.Content.ReadAsStringAsync()}");

                var mensagemSucesso = $"Consulta API FV60 para o pedido {numeroPedido} de numero de fatura {identificadorFatura} foi realizado com sucesso.";
                logProcessoBuilder.AdicionarMensagemTrace(mensagemSucesso);
                logProcessoBuilder.AdicionarMensagemTrace(retornoContent);
                await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());

                return JsonSerializer.Deserialize<RetornoApiDto>(retornoContent);
            }
            catch (Exception e)
            {
                var mensagemErro = $"Ocorreu uma falha consultar API FV60 para o pedido {numeroPedido}.\nErro: {e.Message}";
                logProcessoBuilder.AdicionarMensagemFalha(mensagemErro);
                await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());
                throw new Exception(mensagemErro);
            }
        }

        private ApiPedidoFv60Dto ObterPedidoConsultaFvDto(long numeroPedido, string empresa)
        {
            try
            {
                return new ApiPedidoFv60Dto
                {
                    Numero = numeroPedido,
                    Usuario = campoUsuarioRequest,
                    Empresa = empresa,
                };
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        private async Task<RetornoApiDto> RequisicaoApiClienteFolhaServicoAsync(string identificadorFatura, long numeroPedido, LogProcessoIntegracaoBuilder logProcessoBuilder, IElasticService elasticService)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("Iguatemi");

                var pedidoConsultaDto = ObterPedidoConsultaFolhaDeServicoDto(numeroPedido);
                var json = JsonSerializer.Serialize(pedidoConsultaDto);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(urlApiConsultaFolhaServico, content);
                var retornoContent = response.Content.ReadAsStringAsync().Result;

                if (!response.IsSuccessStatusCode)
                    new Exception($"StatusCode: {response.StatusCode}\nConteúdo resposta: {response.Content.ReadAsStringAsync()}");

                var mensagemSucesso = $"Consulta API Folha de Serviço para o pedido {numeroPedido} de numero de fatura {identificadorFatura} foi realizado com sucesso.";
                logProcessoBuilder.AdicionarMensagemTrace(mensagemSucesso);
                logProcessoBuilder.AdicionarMensagemTrace(retornoContent);
                await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());

                return JsonSerializer.Deserialize<RetornoApiDto>(retornoContent);
            }
            catch (Exception e)
            {
                var mensagemErro = $"Ocorreu uma falha consultar API Folha de Serviço para o pedido {numeroPedido}.\nErro: {e.Message}";
                logProcessoBuilder.AdicionarMensagemFalha(mensagemErro);
                await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());
                throw new Exception(mensagemErro);
            }
        }

        private ApiPedidoFolhaDeServicoDto ObterPedidoConsultaFolhaDeServicoDto(long numeroPedido)
        {
            try
            {
                return new ApiPedidoFolhaDeServicoDto
                {
                    Numero = numeroPedido,
                    Usuario = campoUsuarioRequest
                };
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        private static bool ValidarPreenchimentoDto(DadosApiDto dadosApiDto)
        {
            if (dadosApiDto is null)
                return false;

            return dadosApiDto.Gasto != null &&
                   dadosApiDto.Fiscal != null &&
                   dadosApiDto.Tesouro != null;
        }

        private async Task<RetornoIntegracaoTwmDto> ValidarRespostaConsultaApiFV60(RetornoApiDto dadosConsultaApiDto, FaturasEmAndamentoIguatemiDto faturaDto, LogProcessoIntegracaoBuilder logProcessoBuilder, IElasticService elasticService, ITwmService twmService)
        {
            try
            {
                logProcessoBuilder.AdicionarMensagemTrace($"Realizando a validação do retorno obtido pela API FV60.");
                await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());

                string observacao = dadosConsultaApiDto.Dados.Gasto?.Observacao;

                if (observacao == null)
                {
                    logProcessoBuilder.AdicionarMensagemFalha($"A observação de gasto na API FV60 retornou nula para a fatura {faturaDto.IdentificadorFatura}.");
                    await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());
                    throw new Exception($"A observação de gasto na API FV60 retornou nula para a fatura {faturaDto.IdentificadorFatura}.");
                }

                var sucesso = !string.IsNullOrWhiteSpace(dadosConsultaApiDto.Dados.Tesouro.DataPagamento);
                var parteMensagemLog = sucesso ? "Sucesso" : "Em Andamento";

                logProcessoBuilder.AdicionarMensagemTrace($"O status do pagamento {faturaDto.NumeroFV60Integracao} da fatura {faturaDto.IdentificadorFatura} é {dadosConsultaApiDto.Dados.Gasto.Observacao}, constituindo como {parteMensagemLog}");
                await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());

                var mensagem = string.Empty;

                if (sucesso)
                {
                    mensagem = dadosConsultaApiDto.Dados.Tesouro.Observacao?.Trim();

                    var listaCamposCustomizados = new List<CampoCustomizadoChaveValor>
                    {
                        new CampoCustomizadoChaveValor
                        {
                            Chave = campoCustomizadoDataAgendamento,
                            Valor = ConverterData(dadosConsultaApiDto.Dados.Tesouro.DataAgend)
                        },
                        new CampoCustomizadoChaveValor
                        {
                            Chave = campoCustomizadoDataPagamento,
                            Valor = ConverterData(dadosConsultaApiDto.Dados.Tesouro.DataPagamento)
                        },
                        new CampoCustomizadoChaveValor
                        {
                            Chave = campoCustomizadoPago,
                            Valor = "Sim"
                        }
                    };
                    await twmService.AtualizarCampoCustomizadoTWMAsync(faturaDto.IdentificadorFatura, listaCamposCustomizados, logProcessoBuilder, elasticService);

                    return new RetornoIntegracaoTwmDto(faturaDto.IdFatura, int.Parse(idResponsavelIntegracaoTwm), IntegrationStatus.Sucesso, mensagem);
                }
                else 
                {
                    mensagem = string.IsNullOrEmpty(dadosConsultaApiDto.Dados.Tesouro.Observacao?.Trim())
                               ? dadosConsultaApiDto.Dados.Gasto.Observacao?.Trim()
                               : dadosConsultaApiDto.Dados.Tesouro.Observacao?.Trim();

                    var listaCamposCustomizados = new List<CampoCustomizadoChaveValor>
                    {
                        new CampoCustomizadoChaveValor
                        {
                            Chave = campoCustomizadoDataAgendamento,
                            Valor = ConverterData(dadosConsultaApiDto.Dados.Tesouro.DataAgend)
                        }
                    };

                    await twmService.AtualizarCampoCustomizadoTWMAsync(faturaDto.IdentificadorFatura, listaCamposCustomizados, logProcessoBuilder, elasticService);

                    return new RetornoIntegracaoTwmDto(faturaDto.IdFatura, int.Parse(idResponsavelIntegracaoTwm), IntegrationStatus.AguardandoPagamentoPedido, mensagem);
                }

            }
            catch (Exception e)
            {
                logProcessoBuilder.AdicionarMensagemFalha($"Erro ao validar status do numero {dadosConsultaApiDto.Dados.Gasto.Observacao} na API FV60 para a fatura {faturaDto.IdentificadorFatura}.\nErro:{e.Message}");
                await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());
                throw new Exception($"Erro ao validar status do numero de pedido {dadosConsultaApiDto.Dados.Gasto.Observacao}.<br> Erro:{e.Message}");
            }
        }

        private async Task<RetornoIntegracaoTwmDto> ValidarRespostaConsultaApiFolhaServico(RetornoApiDto dadosConsultaApiDto, FaturasEmAndamentoIguatemiDto faturaDto, LogProcessoIntegracaoBuilder logProcessoBuilder, IElasticService elasticService, ITwmService twmService)
        {
            try
            {
                logProcessoBuilder.AdicionarMensagemTrace($"Realizando a validação do retorno obtido pela API Pedido - Folha de Serviço.");
                await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());

                if (!string.IsNullOrWhiteSpace(dadosConsultaApiDto.Dados.Tesouro.DataPagamento))
                {

                    logProcessoBuilder.AdicionarMensagemTrace($"O status da Folha de Serviço da fatura {faturaDto.IdentificadorFatura} está com data de pagamento, constituindo Sucesso");
                    await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());

                    var mensagem = $"Número do Pedido: {dadosConsultaApiDto.Dados.Tesouro.NumeroPedido}<br>Observação: {dadosConsultaApiDto.Dados.Tesouro.Observacao}";

                    return new RetornoIntegracaoTwmDto(faturaDto.IdFatura, int.Parse(idResponsavelIntegracaoTwm), IntegrationStatus.Sucesso, mensagem);
                }
                else if (dadosConsultaApiDto.Dados.Gasto.Observacao == "Concluido")
                {
                    logProcessoBuilder.AdicionarMensagemTrace($"O status da Folha de Serviço da fatura {faturaDto.IdentificadorFatura} está concluído, constituindo Sucesso");
                    await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());

                    var mensagem = $"Número do Pedido: {dadosConsultaApiDto.Dados.Gasto.Pedido}<br> Status Folha de serviço: {dadosConsultaApiDto.Dados.Gasto.Observacao}";

                    var listaCamposCustomizados = new List<CampoCustomizadoChaveValor>
                    {
                        new CampoCustomizadoChaveValor
                        {
                            Chave = campoCustomizadoDataAgendamento,
                            Valor = ConverterData(dadosConsultaApiDto.Dados.Tesouro.DataAgend)
                        },
                        new CampoCustomizadoChaveValor
                        {
                            Chave = campoCustomizadoDataPagamento,
                            Valor = ConverterData(dadosConsultaApiDto.Dados.Tesouro.DataPagamento)
                        }
                    };

                    await twmService.AtualizarCampoCustomizadoTWMAsync(faturaDto.IdentificadorFatura, listaCamposCustomizados, logProcessoBuilder, elasticService);

                    return new RetornoIntegracaoTwmDto(faturaDto.IdFatura, int.Parse(idResponsavelIntegracaoTwm), IntegrationStatus.Sucesso, mensagem);
                }
                else
                {
                    logProcessoBuilder.AdicionarMensagemTrace($"O status da Folha de Serviço da fatura {faturaDto.IdentificadorFatura} é diferente de 'Sucesso'. Caso for 'Reprovado' iremos lançar como falha.");
                    await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());

                    var statusReprovado = dadosConsultaApiDto.Dados.Gasto.Observacao == "Reprovado";
                    var mensagem = $"Mensagem: {dadosConsultaApiDto.Mensagem}<br> Observação: {dadosConsultaApiDto.Dados.Gasto.Observacao}";

                    if (statusReprovado)
                        return new RetornoIntegracaoTwmDto(faturaDto.IdFatura, int.Parse(idResponsavelIntegracaoTwm), IntegrationStatus.Erro, mensagem);

                    return new RetornoIntegracaoTwmDto(faturaDto.IdFatura, int.Parse(idResponsavelIntegracaoTwm), IntegrationStatus.AguardandoPagamentoPedido, mensagem);
                }
            }
            catch (Exception e)
            {
                logProcessoBuilder.AdicionarMensagemFalha($"Erro ao validar status do numero {dadosConsultaApiDto.Dados.Gasto.Observacao} na API Pedido - Folha de Serviço para a fatura {faturaDto.IdentificadorFatura}.\nErro:{e.Message}");
                await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());
                throw new Exception($"Erro ao validar status do numero de pedido {dadosConsultaApiDto.Dados.Gasto.Observacao}.<br> Erro:{e.Message}");
            }
        }

        private async Task<RetornoIntegracaoTwmDto> ValidarRespostaConsultaApiPedido(RetornoApiDto dadosConsultaApiDto, FaturasEmAndamentoIguatemiDto faturaDto, LogProcessoIntegracaoBuilder logProcessoBuilder, IElasticService elasticService)
        {
            try
            {
                logProcessoBuilder.AdicionarMensagemTrace($"Realizando a validação do retorno obtido pela API Pedido - Folha de Serviço.");
                await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());

                if (dadosConsultaApiDto.Dados.Gasto.Observacao.Contains("Aprovado"))
                {
                    logProcessoBuilder.AdicionarMensagemTrace($"O status do pedido da fatura {faturaDto.IdentificadorFatura} está como 'Aprovado'.");
                    await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());

                    var mensagem = $"Número do Pedido: {dadosConsultaApiDto.Dados.Gasto.Pedido}<br> Status Pedido: {dadosConsultaApiDto.Dados.Gasto.Observacao}";
                    return new RetornoIntegracaoTwmDto(faturaDto.IdFatura, int.Parse(idResponsavelIntegracaoTwm), IntegrationStatus.Sucesso, mensagem);
                }
                else
                {
                    logProcessoBuilder.AdicionarMensagemTrace($"O status do pedido da fatura {faturaDto.IdentificadorFatura} é diferente de 'Sucesso'. Caso for 'Reprovado' iremos lançar como falha.");
                    await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());

                    var statusReprovado = dadosConsultaApiDto.Dados.Gasto.Observacao == "Reprovado";
                    var mensagem = $"Mensagem: {dadosConsultaApiDto.Mensagem}<br> Observação: {dadosConsultaApiDto.Dados.Gasto.Observacao}";

                    if (statusReprovado)
                        return new RetornoIntegracaoTwmDto(faturaDto.IdFatura, int.Parse(idResponsavelIntegracaoTwm), IntegrationStatus.Erro, mensagem);

                    return new RetornoIntegracaoTwmDto(faturaDto.IdFatura, int.Parse(idResponsavelIntegracaoTwm), IntegrationStatus.AguardandoPagamentoPedido, mensagem);
                }
            }
            catch (Exception e)
            {
                logProcessoBuilder.AdicionarMensagemFalha($"Erro ao validar status do numero {dadosConsultaApiDto.Dados.Gasto.Observacao} na API Pedido - Folha de Serviço para a fatura {faturaDto.IdFatura}.\nErro:{e.Message}");
                await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());
                throw new Exception($"Erro ao validar status do numero de pedido {dadosConsultaApiDto.Dados.Gasto.Observacao}.<br> Erro:{e.Message}");
            }
        }

        private string ConverterData(string dataApiIguatemi)
        {
            if (string.IsNullOrEmpty(dataApiIguatemi))
                return string.Empty;

            DateTime data = DateTime.ParseExact(dataApiIguatemi, dataPadraoApiIguatemi, CultureInfo.InvariantCulture);
            return data.ToString(dataPadrao);
        }
    }
}
