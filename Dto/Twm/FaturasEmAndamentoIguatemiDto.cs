using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionApp.Dto.Twm
{
    public class FaturasEmAndamentoIguatemiDto
    {
        public int IdFatura { get; set; }
        public int IdStatusIntegracao { get; set; }
        public string IdentificadorFatura { get; set; }
        public DateTime DataUltimoStatus { get; set; }
        public long? NumeroFV60Integracao { get; set; }
        public long? NumeroFolhaServico { get; set; }
        public long? NumeroPedido { get; set; }
        public string Empresa { get; set; }
    }
}
