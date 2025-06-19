namespace FunctionApp.Dto.RetornoApi;
public class TesouroDto
{   
    public string Empresa { get; init; }
    public string NumeroDocumento { get; init; }
    public string NumeroPedido { get; init; }
    public string Item { get; init; }
    public string NumeroNotaFiscal { get; init; }
    public string Texto { get; init; }
    public string DataVencimento { get; init; }
    public string DataAgend { get; init; }
    public string DataPagamento { get; init; }
    public string FormaPagamento { get; init; }
    public string ValorLiquido { get; init; }
    public string ValorBruto { get; init; }
    public string Observacao { get; init; }
}
