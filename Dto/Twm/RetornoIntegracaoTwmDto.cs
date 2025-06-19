using Newtonsoft.Json;

namespace FunctionApp.Dto.Twm
{
    public class RetornoIntegracaoTwmDto
    {
        public RetornoIntegracaoTwmDto(
            int invoiceId,
            int integrationResponsibleUserId,
            IntegrationStatus integrationStatus,
            string message)
        {
            InvoiceId = invoiceId;
            IntegrationResponsibleUserId = integrationResponsibleUserId;
            IntegrationStatus = integrationStatus;
            Message = message;
        }

        [JsonProperty("IdFatura")]
        public int InvoiceId { get; set; }

        [JsonProperty("IdUsuarioResponsavelIntegracao")]
        public int IntegrationResponsibleUserId { get; set; }

        [JsonProperty("StatusIntegracao")]
        public IntegrationStatus IntegrationStatus { get; set; }

        [JsonProperty("Mensagem")]
        public string Message { get; set; }
    }

    public enum IntegrationStatus
    {
        Erro = 0,
        Sucesso = 1,
        AguardandoPagamentoPedido = 14
    }
}