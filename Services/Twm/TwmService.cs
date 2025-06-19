using FunctionApp.Dto.Twm;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text;
using FunctionApp.Builders.Elastic;
using FunctionApp.Services.Elastic.Interfaces;
using FunctionApp.Services.Twm.Interfaces;

namespace FunctionApp.Services.Twm
{
    public class TwmService : ITwmService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly string urlAtualizarCampoCustomizadoFatura = Environment.GetEnvironmentVariable("Twm:UrlAtualizarCampoCustomizadoFatura");
        private readonly string urlBuscarFaturas = Environment.GetEnvironmentVariable("Twm:UrlBuscarFaturas");
        private readonly string urlSalvarRetorno = Environment.GetEnvironmentVariable("Twm:UrlSalvarRetornoIntegracao");
        private readonly string urlIniciarIntegracaoPedido = Environment.GetEnvironmentVariable("Twm:UrlIniciarIntegracao");

        public TwmService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IList<FaturasEmAndamentoTwmDto>> BuscarFaturasEmAndamento(LogProcessoIntegracaoBuilder logProcessoBuilder, IElasticService elasticService)
        {
            try
            {
                logProcessoBuilder.AdicionarMensagemTrace("Buscando todas as faturas em andamento.");
                await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());

                var client = _httpClientFactory.CreateClient("TWM");
                var response = await client.GetAsync(urlBuscarFaturas);

                if (!response.IsSuccessStatusCode)
                    new Exception($"StatusCode: {response.StatusCode}\nConteúdo resposta: {response.Content.ReadAsStringAsync()}");

                return await response.Content.ReadFromJsonAsync<IList<FaturasEmAndamentoTwmDto>>();
            }
            catch (Exception e)
            {
                var mensagemErro = $"Erro inesperado ao consultar faturas no TWM.\n{e.Message}";
                logProcessoBuilder.AdicionarMensagemFalha(mensagemErro);
                await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());
                throw new Exception(mensagemErro);
            }
        }

        public async Task SalvarRetornoIntegracaoAsync(RetornoIntegracaoTwmDto mensagemRetornoDto, LogProcessoIntegracaoBuilder logProcessoBuilder, IElasticService elasticService)
        {
            try
            {
                logProcessoBuilder.AdicionarMensagemTrace($"Realizando envio do retorno da API para o TWM. Fatura: {mensagemRetornoDto.InvoiceId}.");
                await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());

                var client = _httpClientFactory.CreateClient("TWM");
                var serializedBody = JsonConvert.SerializeObject(new[] { mensagemRetornoDto });
                var content = new StringContent(serializedBody, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(urlSalvarRetorno, content);

                if (!response.IsSuccessStatusCode)
                    new Exception($"StatusCode: {response.StatusCode}\nConteúdo resposta: {response.Content.ReadAsStringAsync()}");

            }
            catch (Exception e)
            {
                var mensagemErro = $"Ocorreu uma falha ao salvar o retorno no histórico de integração da fatura no TWM.\nIdFatura: {mensagemRetornoDto.InvoiceId}.\nErro: {e.Message}";
                logProcessoBuilder.AdicionarMensagemTrace(mensagemErro);
                await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());
                throw new Exception(mensagemErro);
            }
        }

        public async Task IniciarProcessoIntegracao(int idFatura, LogProcessoIntegracaoBuilder logProcessoBuilder, IElasticService elasticService)
        {
            int[] faturaArray = new int[] { idFatura };

            try
            {
                logProcessoBuilder.AdicionarMensagemTrace($"Realizando ínicio de integração pós aprovação do pedido da fatura: {idFatura}.");
                await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());

                var client = _httpClientFactory.CreateClient("TWM");
                var serializedBody = JsonConvert.SerializeObject(faturaArray);
                var content = new StringContent(serializedBody, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(urlIniciarIntegracaoPedido, content);

                if (!response.IsSuccessStatusCode)
                    new Exception($"StatusCode: {response.StatusCode}\nConteúdo resposta: {response.Content.ReadAsStringAsync()}");
            }
            catch (Exception e)
            {
                var mensagemErro = $"Ocorreu uma falha ao ao iniciar o processo de integração da fatura no TWM.\nIdFatura: {idFatura}.\nErro: {e.Message}";
                logProcessoBuilder.AdicionarMensagemFalha(mensagemErro);
                await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());
                throw new Exception(mensagemErro);
            }
        }

        public async Task AtualizarCampoCustomizadoTWMAsync(string identificadorFatura, IList<CampoCustomizadoChaveValor> listaCamposCustomizados, LogProcessoIntegracaoBuilder logProcessoBuilder, IElasticService elasticService)
        {
            try
            {
                logProcessoBuilder.AdicionarMensagemTrace($"Atualizando os campos customizados da fatura {identificadorFatura} no TWM.");
                await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());

                var fatura = new CampoCustomizadoFaturaApiUpdateDto
                {
                    NumeroFatura = identificadorFatura,
                    CamposCustomizadosChaveValor = listaCamposCustomizados
                };

                var json = JsonConvert.SerializeObject(fatura);
                var client = _httpClientFactory.CreateClient("TWM");
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PutAsync(urlAtualizarCampoCustomizadoFatura, content);

                if (!response.IsSuccessStatusCode)
                    new Exception($"StatusCode: {response.StatusCode}\nConteúdo resposta: {response.Content.ReadAsStringAsync()}");

            }
            catch (Exception e)
            {
                var mensagemErro = $"Ocorreu uma falha ao atualizar os campos customizados da fatura {identificadorFatura} no TWM.\nErro: {e.Message}";
                logProcessoBuilder.AdicionarMensagemFalha(mensagemErro);
                await elasticService.InserirLogProcessoIntegracaoAsync(logProcessoBuilder.Build());

                throw new Exception(mensagemErro);
            }
        }

    }
}
