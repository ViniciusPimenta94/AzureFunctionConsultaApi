using Newtonsoft.Json;
using System.Collections.Generic;

namespace FunctionApp.Dto.Twm
{
    public class CampoCustomizadoFaturaApiUpdateDto
    {
        public string NumeroFatura { get; set; }
        public IEnumerable<CampoCustomizadoChaveValor> CamposCustomizadosChaveValor { get; set; }
    }
}