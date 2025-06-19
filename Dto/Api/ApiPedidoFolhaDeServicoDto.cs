using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FunctionApp.Dto
{
    public class ApiPedidoFolhaDeServicoDto
    {
        [JsonPropertyName("numero")]
        public long Numero { get; init; }

        [JsonPropertyName("usuario")]
        public string Usuario { get; init; }
    }
}
