using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WASM_To_MC.Parsing
{
    public class ParseException : Exception
    {
        public ParseException(string msg) : base(msg) { }

        public ParseException(string msg, Exception inner) : base(msg, inner) { }
    }
}
