using FunctionApp.Builders.Elastic;
using System;

namespace FunctionApp.Dto.Elastic
{
    public class LogProcessoIntegracaoDto
    {
        const string ProcessoAtual = "Consulta API";

        #region Transação Aplicação
        public string IdTransacaoAplicacao { get; set; }
        public DateTime DataCriacaoTransacaoAplicacao { get; set; }
        #endregion

        public string MensagemTrace { get; set; }
        public string MensagemAlerta { get; set; }
        public string MensagemFalha { get; set; }
        public string EtapaExecucaoAtual { get; set; }

        public string Resultado { get; set; }

        public static LogProcessoIntegracaoBuilder Create() => new LogProcessoIntegracaoBuilder(Guid.NewGuid().ToString(), DateTime.Now, ProcessoAtual);

    }
}
