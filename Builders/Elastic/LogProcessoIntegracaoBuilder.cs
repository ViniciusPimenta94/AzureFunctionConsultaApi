using FunctionApp.Dto.Elastic;
using System;

namespace FunctionApp.Builders.Elastic
{
    public class LogProcessoIntegracaoBuilder
    {
        private LogProcessoIntegracaoDto _logProcessoIntegracao;

        public LogProcessoIntegracaoBuilder(string idTransacaoAplicacao, DateTime dataCriacaoTransacaoAplicacao, string etapaExecucaoAtual)
            => _logProcessoIntegracao = new LogProcessoIntegracaoDto()
            {
                IdTransacaoAplicacao = idTransacaoAplicacao,
                DataCriacaoTransacaoAplicacao = dataCriacaoTransacaoAplicacao,
                EtapaExecucaoAtual = etapaExecucaoAtual
            };

        public LogProcessoIntegracaoBuilder AdicionarMensagemTrace(string mensagemTrace)
        {
            _logProcessoIntegracao.MensagemTrace = mensagemTrace;

            return this;
        }
        public LogProcessoIntegracaoBuilder AdicionarMensagemAlerta(string mensagemAlerta)
        {
            _logProcessoIntegracao.MensagemAlerta = mensagemAlerta;

            return this;
        }
        public LogProcessoIntegracaoBuilder AdicionarMensagemFalha(string mensagemFalha)
        {
            _logProcessoIntegracao.MensagemFalha = mensagemFalha;

            return this;
        }

        public LogProcessoIntegracaoDto Build() => _logProcessoIntegracao;
    }
}
