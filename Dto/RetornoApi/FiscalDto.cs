namespace FunctionApp.Dto.RetornoApi;
public class FiscalDto
{
    public string DataDigitalizacao { get; init; }
    public string DataCriacaoDocContabil { get; init; }
    public string Transacao { get; init; }
    public string NumeroNotaFiscal { get; init; }
    public string NumeroDocumento { get; init; }
    public string NumeroCompensacao { get; init; }
    public string DataPrevistaVencimento { get; init; }
    public string DataLiberadaTesouraria { get; init; }
    public string Observacao { get; init; }
}
