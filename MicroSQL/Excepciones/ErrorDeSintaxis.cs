using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MicroSQL.Excepciones
{
    public class ErrorDeSintaxis : Exception
    {
        public ErrorDeSintaxis()
        {

        }

        public ErrorDeSintaxis(string msg) : base(msg)
        {            
        }
        
    }
}
