using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhoIsCSharp.lib;

namespace WhoIsCSharp {
    class Program {
        static void FindCultureFormat(string date, string Seperator) {
            DateTime time;
            var matchingCulture = CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(ci => DateTime.TryParse(date, ci, DateTimeStyles.None, out time));

            Console.WriteLine(
                "\nDT:\t\t" + date + "\n" +
                "Culture:\t" + matchingCulture.DisplayName + "\n" +
                "Int:\t\t" + matchingCulture.LCID + "\n" +
                "Format:\t\t" + matchingCulture.DateTimeFormat + "\n");
        }

        static void Main(string[] args) {
            string wiResp = System.IO.File.ReadAllText("WhoisResp.txt");
            var dom = Parser.Parse(wiResp);

            var json = JsonConvert.SerializeObject(dom, Formatting.Indented);
            System.IO.File.WriteAllText("tests/output.json", json);

            Console.ReadKey();
        }
    }
}
