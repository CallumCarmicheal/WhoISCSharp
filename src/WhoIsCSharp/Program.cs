using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhoIsCSharp.lib;

namespace WhoIsCSharp {
    class Program {
        static void Main(string[] args) {
            string wiResp = System.IO.File.ReadAllText("WhoisResp.txt");

            Parser.Parse(wiResp);

            Console.ReadKey();
        }
    }
}
