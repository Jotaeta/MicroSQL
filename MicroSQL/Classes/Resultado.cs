using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MicroSQL.Classes
{
    public class Resultado
    {
        public string tipoComando;
        public object data;

        public Resultado(string tipoComando, object data)
        {
            this.tipoComando = tipoComando != null ? tipoComando : "desconocido";
            this.data = data;

        }
    }
}
