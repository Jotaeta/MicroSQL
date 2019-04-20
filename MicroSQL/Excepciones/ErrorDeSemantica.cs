using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MicroSQL.Excepciones
{
    public class ErrorDeSemantica : Exception
    {
        public ErrorDeSemantica()
        {

        }

        public ErrorDeSemantica(string msg) : base(msg)
        {            
        }
        
    }
}
