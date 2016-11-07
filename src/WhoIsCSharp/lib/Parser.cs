using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhoIsCSharp.lib.Models;
using static WhoIsCSharp.lib.Models.Domain;

namespace WhoIsCSharp.lib {
    class Parser {
        enum PStage {
            Servers=1,
            DomainData=2,
            DomainDates=3,
            WhoIsDBDates=4,
            DomainName=5,
            DomainRegInfo=6,
            DomainStatus=7,
            Registrant=8,
            NameServers=9,
            DNSSec=10,
            AbuseInfo=11
        }
        
        // Original, Expected, Value, Target
        private static void Set(string o, string e, string v, ref string t) {
            if (o == e) t = v.Trim();
        }

        // Original, Expected, Value, Target
        private static void Set(string o, string e, string v, ref int t) {
            if (o == e) 
                t = int.Parse(v);
        }

        // Original, Expected, Value, Target
        private static void Set(string o, string e, string v, List<string> t) {
            if (o == e) 
                t.Add(v.Trim());
        }

        // Original, Expected, Value, Target
        private static void Set(string o, string e, string v, List<DStatus> t) {
            if (o == e) {
                string[] x = v.Split(' ');
                var d = new DStatus();
                d.Event = x[0];
                d.IcannURl = x[1];
                t.Add(d);
            }
        }

        // Original, Expected, Value, Target
        private static void SetDMY(string o, string e, string v, DateTime t) {
            if (o == e) 
                t = DateTime.ParseExact(
                    v.Trim(), 
                    "dd-MMM-yyyy", 
                    null);
        }

        public static void Parse(string WIResponse) {
            var wiResp  = WIResponse.Split('\n');
            var wiLines = new List<string>();
            Domain dom = new Domain();

            foreach (string x in wiResp) {
                // Excl all lines that dont contain a 
                // colon!
                
                if (x.Contains(':')) {
                    // Split string by colon
                    string[] c = x.Split(':');
                    string lw = c[0].Trim().ToLower();

                    // Exclude any links such as anything related to
                    // ELUA, TERMS ETC, Text that is not needed for parsing
                    if (lw.EndsWith("http") || lw.EndsWith("https")) continue;
                    else if (lw.EndsWith("terms of use")) continue;
                    else if (lw.EndsWith("notice")) continue;
                    else if (lw.EndsWith("to")) continue;

                    // Keyword Checking
                    switch (lw) {
                    case "to": continue;
                    case "": continue;
                    case "url of the icann whois data problem reporting system":
                        continue;
                    }

                    wiLines.Add(x.Trim());
                }
            }

            var wiLRaw = wiLines.ToArray();

            // Save our wiLines for debugging later
            System.IO.File.WriteAllLines("debug.var.wiLines", wiLines.ToArray());

            /**
             * Who is response data starts with the following flow 
             * 
             * 1) Servers 
             * - Ends at the first occurence of 
             *     "Domain Name"
             * 2) Simple Domain Data
             * 3) Update Date, Creation Date (Domain)
             * 3) WhoIs Database Update Information
             * 4) Domain Name
             * 5) Registry and Registrar Info
             * 6) Update, Creation Date (DNS or Domain or Registr???) [SKIPPED]
             * 7) Registrar Information
             * 8) Domain Status (Copy of Simple Domain Data?)
             * 10) Registrant Information
             * 11) Staff Information
             * 12) DNS Name Servers
             * 13) DNS Sec
             * 14) Registrar Abuse Information
             */

            // First setup our objects
            // I know i can do this in the classes
            // but cbb ;)
            dom.DomainStatus        = new Domain.DStatus();
            dom.Registrant          = new Registrant();
            dom.Registrant.Contact  = new Models.Common.Contact();
            dom.Registrant.Location = new Models.Common.Location();
            dom.Registrant.Registry = new Models.Common.RegistRegistry();
            dom.Staff               = new Staff();
            dom.Staff.Admin         = new Models.Common.StaffMember();
            dom.Staff.Tech          = new Models.Common.StaffMember();
            dom.Registrar           = new Registrar();

            string[] iServers = {
                "Server Name",  "IP Address",
                "Registrar",    "Whois Server",
                "Referral URL"
            };

            string[] iSDomain = {
                "Domain Name",      "Registrar",
                "Sponsoring Registrar IANA ID",
                "Whois Server",     "Referral URL",
                "Name Server",      "Status",
                "Updated Date",     "Creation Date",
                "Expiration Date"
            };

            // Temp object to reuse
            Models.Server tServer = new Server();
            List<string> tSList   = new List<string>();
            List<DStatus> tDList  = new List<DStatus>();

            int stage = (int)PStage.Servers;

            string lOpt = "";
            for (int i = 0; i < wiLines.Count - 1; i++) {
                string   x = wiLRaw[i];
                string[] l = x.Split(':');

                string 
                    opt = l[0],
                    val = l[1];

                System.Console.WriteLine($"Started new Line ({i}): {x}");

                switch ((PStage)(stage)) {
                case PStage.Servers:
                    if (!iServers.Contains(opt)) {
                        stage++; continue;
                    }

                    if (opt == "Server Name") {
                        tServer = new Server();
                        tServer.Name = val;
                    } else {
                        Set(opt, "IP Address",   val, ref tServer.IP);
                        Set(opt, "Registrar",    val, ref tServer.Registrar);
                        Set(opt, "Whois Server", val, ref tServer.WIServer);
                        Set(opt, "Referral URL", val, ref tServer.ReferralURL);
                    }
                    break;

                case PStage.DomainData:
                    if (!iSDomain.Contains(opt) && 
                        !opt.StartsWith(">>>")) {
                        stage++; continue;
                    }

                    if (opt == "Domain Name") {
                        tSList.Clear();
                        tDList.Clear();
                    }

                    Set(opt, "Domain Name", val, ref dom.Name);
                    Set(opt, "Registrar", val, ref dom.Registrar.Name);
                    Set(opt, "Sponsoring Registrar IANA ID", val, ref dom.Registrar.ID);
                    Set(opt, "Whois Server", val, ref dom.Registrar.WIServer);
                    Set(opt, "Referral URL", val, ref dom.Registrar.ReferralURL);
                    Set(opt, "Name Server", val, tSList);
                    
                    // Save our name servers
                    if(opt == "Status" && lOpt == "Name Server") 
                        dom.NameServers = tSList.ToArray<string>();

                    Set(opt, "Status", val, tDList);

                    if (opt == "Updated Date")
                        dom.Status = tDList.ToArray();

                    SetDMY(opt, "Updated Date", val, dom.DateUpdated);
                    SetDMY(opt, "Creation Date", val, dom.DateCreated);
                    SetDMY(opt, "Expiration Date", val, dom.DateExpiring);

                    break;
                default:
                    break;
                }

                lOpt = opt;
            }

            return;
        }
    }
}
