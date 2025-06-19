namespace FunctionApp.Dto.RetornoApi;
public record DadosApiDto
{
    public GastoDto Gasto { get; init; }
    public FiscalDto Fiscal { get; init; }
    public TesouroDto Tesouro { get; init; }
    public object Aprovadores { get; init; }
}
