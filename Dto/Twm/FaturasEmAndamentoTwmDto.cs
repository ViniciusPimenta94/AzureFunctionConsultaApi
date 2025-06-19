using System;
using System.Collections.Generic;

namespace FunctionApp.Dto.Twm;

public class FaturasEmAndamentoTwmDto
{
    public int IdFatura { get; set; }
    public int IdStatusIntegracao { get; set; }
    public string IdentificadorFatura { get; set; }
    public DateTime DataUltimoStatus { get; set; }

    public List<CampoCustomizado> CampoCustomizados { get; set; }

    public class CampoCustomizado
    {
        public string DescricaoCampo { get; set; }
        public string Valor { get; set; }
    }
}
