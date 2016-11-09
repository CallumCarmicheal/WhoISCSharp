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
            DomainRegInfo=3,
            ContactInformation=4,
            NameServers=5,
            ExtraFinalProperties=6,
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
        private static void Set(string o, string e, string v, ref List<string> t) {
            if (o == e) 
                t.Add(v.Trim());
        }

        // Original, Expected, Value, Target
        private static void Set(string o, string e, string[] l, ref List<DStatus> t) {
            if (o == e) {
                string[] x = string.Join(":",l).Split(' ');
                string evt = x[x.Length -2];
                string icn = x[x.Length -1];

                bool exist = false;

                foreach(var i in t) 
                    if (i.Event == x[0])
                        exist = true; 

                if (!exist) {
                    var d = new DStatus();
                    d.Event = evt;
                    d.IcannURl = icn;
                    t.Add(d);
                }
            }
        }

        private static void SetURL(string o, string e, string[] v, ref string t) {
            if(o == e) {
                string url = "";
                for(int x = 1; x < v.Length; x++) 
                    url += ":" + v[x];
                url = url.Substring(1, url.Length-1);
                t = url;
            }
        }

        // Original, Expected, Value, Target
        // if (o == e) v = t
        private static void Set(string o, string e, string v, ref DateTime t) {
            if (o == e) {
                if (v.Contains("T")) {
                    // Contains Timezone
                    string[] x = v.Split('T');

                    // Ignoring what i presume to be the 
                    // timezone T part because i have
                    // no idea how to even parse it nor
                    // use it to modify the dt supplied.
                    t = DateTime.Parse(x[0].Trim());
                } else {
                    t = DateTime.Parse(v.Trim());
                }
            }
            
        }

        public static Domain Parse(string WIResponse) {
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
            System.IO.File.WriteAllLines("tests/debug.var.wiLines", wiLines.ToArray());

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
            bool cont = true;
            for (int i = 0; cont; i++) {
                string   x = wiLRaw[i];
                string[] l = x.Split(':');

                cont = i < wiLines.Count - 1;

                string 
                    opt = l[0],
                    val = l[1];

                System.Console.WriteLine($"Started new Line ({i}): {x}");

                switch ((PStage)(stage)) {
                case PStage.Servers:
                    if (!iServers.Contains(opt)) {
                        i--; stage++; continue;
                    }

                    if (opt == "Server Name") {
                        tServer = new Server();
                        tServer.Name = val;
                    } else {
                        Set(opt, "IP Address",   val, ref tServer.IP);
                        Set(opt, "Registrar",    val, ref tServer.Registrar);
                        Set(opt, "Whois Server", val, ref tServer.WIServer);
                        SetURL(opt, "Referral URL", l, ref tServer.ReferralURL);
                    }
                    break;

                case PStage.DomainData:
                    if (!iSDomain.Contains(opt) && 
                        !opt.StartsWith(">>>")) {
                        i--; stage++; continue;
                    }

                    if (opt == "Domain Name") {
                        tSList.Clear();
                        tDList.Clear();
                    }

                    Set(opt, "Domain Name", val, ref dom.Name);
                    Set(opt, "Registrar", val, ref dom.Registrar.Name);
                    Set(opt, "Sponsoring Registrar IANA ID", val, ref dom.Registrar.ID);
                    Set(opt, "Whois Server", val, ref dom.Registrar.WIServer);
                    SetURL(opt, "Referral URL", l, ref dom.Registrar.ReferralURL);
                    Set(opt, "Name Server", val, ref tSList);
                    
                    // Save our name servers
                    if(opt == "Status" && lOpt == "Name Server") 
                        dom.NameServers = tSList.ToArray<string>();

                    Set(opt, "Status", l, ref tDList);

                    if (opt == "Updated Date")
                        dom.Status = tDList.ToArray();

                    Set(opt, "Updated Date",     val, ref dom.DateUpdated);
                    Set(opt, "Creation Date",    val, ref dom.DateCreated);
                    Set(opt, "Expiration Date",  val, ref dom.DateExpiring);

                    if (opt.StartsWith(">>>")) {
                        string format = "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'";

                        val = "";
                        for (int z = 1; z < l.Length; z++)
                            val += ":" + l[z];
                        val = val.Substring(1, val.Length - 1);
                        val = val.Replace("<<<", "").Trim();
                        DateTime t = DateTime.ParseExact(val, format, null);
                        dom.DateWIUpdated = t;

                        stage++; continue;
                    }

                    break;

                case PStage.DomainRegInfo:
                    Set(opt,    "Registry Domain ID", val, ref dom.Registrar.ID);
                    Set(opt,    "Registrar WHOIS Server", val, ref dom.Registrar.WIServer);
                    SetURL(opt, "Registrar URL", l, ref dom.Registrar.URL);
                    Set(opt,    "Updated Date", val, ref dom.Registrar.WIServer);
                    Set(opt,    "Creation Date", val, ref dom.Registrar.WIServer);
                    Set(opt,    "Registrar Registration Expiration Date", val, ref dom.Registrar.DateRegExp);
                    Set(opt,    "Registrar", val, ref dom.Registrar.Name);
                    Set(opt,    "Registrar IANA ID", val, ref dom.Registrar.IanaID);
                    Set(opt,    "Reseller", val, ref dom.Reseller);
                    
                    if (opt == "Domain Status" && lOpt == "Reseller") {
                        // Clear the temp status list
                        // Add the current events
                        tDList.Clear();
                        
                        if (dom.DomainStatus != null) {
                            if (dom.DomainStatus.Length >= 0)
                                tDList.AddRange(dom.DomainStatus);
                        }
                    }

                    Set(opt, "Domain Status", l, ref tDList);

                    if (opt == "Registry Registrant ID" && 
                        lOpt == "Domain Status") {
                        dom.DomainStatus = tDList.ToArray();

                        // Increase the current stage to Registrant
                        // Decrease the loop to re-run the current value
                        i--; 
                        stage++;
                    } break;

                case PStage.ContactInformation:
                    // Prob a better way of doing this but what the hell..

                    Set(opt, "Registry Registrant ID", val, ref dom.Staff.Registrant.ID);
                    Set(opt, "Registry Admin ID", val, ref dom.Staff.Admin.ID);
                    Set(opt, "Registry Tech ID", val, ref dom.Staff.Tech.ID);

                    Set(opt, "Registrant Name", val, ref dom.Staff.Registrant.Name);
                    Set(opt, "Admin Name", val, ref dom.Staff.Admin.Name);
                    Set(opt, "Tech Name", val, ref dom.Staff.Tech.Name);

                    Set(opt, "Registrant Organization", val, ref dom.Staff.Registrant.Organisation);
                    Set(opt, "Admin Organization", val, ref dom.Staff.Admin.Organisation);
                    Set(opt, "Tech Organization", val, ref dom.Staff.Tech.Organisation);

                    Set(opt, "Registrant Street", val, ref dom.Staff.Registrant.Location.Street);
                    Set(opt, "Admin Street", val, ref dom.Staff.Admin.Location.Street);
                    Set(opt, "Tech Street", val, ref dom.Staff.Tech.Location.Street);

                    Set(opt, "Registrant City", val, ref dom.Staff.Registrant.Location.City);
                    Set(opt, "Admin City", val, ref dom.Staff.Admin.Location.City);
                    Set(opt, "Tech City", val, ref dom.Staff.Tech.Location.City);

                    Set(opt, "Registrant State/Province", val, ref dom.Staff.Registrant.Location.State);
                    Set(opt, "Admin State/Province", val, ref dom.Staff.Admin.Location.State);
                    Set(opt, "Tech State/Province", val, ref dom.Staff.Tech.Location.State);

                    Set(opt, "Registrant Postal Code", val, ref dom.Staff.Registrant.Location.PostalCode);
                    Set(opt, "Admin Postal Code", val, ref dom.Staff.Admin.Location.PostalCode);
                    Set(opt, "Tech Postal Code", val, ref dom.Staff.Tech.Location.PostalCode);

                    Set(opt, "Registrant Country", val, ref dom.Staff.Registrant.Location.Country);
                    Set(opt, "Admin Country", val, ref dom.Staff.Admin.Location.Country);
                    Set(opt, "Tech Country", val, ref dom.Staff.Tech.Location.Country);

                    Set(opt, "Registrant Phone", val, ref dom.Staff.Registrant.Contact.Phone);
                    Set(opt, "Admin Phone", val, ref dom.Staff.Admin.Contact.Phone);
                    Set(opt, "Tech Phone", val, ref dom.Staff.Tech.Contact.Phone);

                    Set(opt, "Registrant Phone Ext", val, ref dom.Staff.Registrant.Contact.PhoneExt);
                    Set(opt, "Admin Phone Ext", val, ref dom.Staff.Admin.Contact.PhoneExt);
                    Set(opt, "Tech Phone Ext", val, ref dom.Staff.Tech.Contact.PhoneExt);

                    Set(opt, "Registrant Fax", val, ref dom.Staff.Registrant.Contact.Fax);
                    Set(opt, "Admin Fax", val, ref dom.Staff.Admin.Contact.Fax);
                    Set(opt, "Tech Fax", val, ref dom.Staff.Tech.Contact.Fax);

                    Set(opt, "Registrant Fax Ext", val, ref dom.Staff.Registrant.Contact.FaxExt);
                    Set(opt, "Admin Fax Ext", val, ref dom.Staff.Admin.Contact.FaxExt);
                    Set(opt, "Tech Fax Ext", val, ref dom.Staff.Tech.Contact.FaxExt);

                    Set(opt, "Registrant Email", val, ref dom.Staff.Registrant.Contact.Email);
                    Set(opt, "Admin Email", val, ref dom.Staff.Admin.Contact.Email);
                    Set(opt, "Tech Email", val, ref dom.Staff.Tech.Contact.Email);

                    if (opt == "Tech Email") 
                        stage++;
                    break;

                case PStage.NameServers:

                    if (lOpt == "Tech Email") 
                        tSList.Clear();

                    Set(opt, "Name Server", val, ref tSList);

                    if (opt != "Name Server") {
                        i--;
                        stage++;
                        continue;
                    } break;

                case PStage.ExtraFinalProperties:

                    Set(opt, "DNSSEC", val, ref dom.DNSSEC);
                    SetURL(opt, "URL of the ICANN WHOIS Data Problem Reporting System", l, ref dom.WhoIs.ErrorReportURL);
                    
                    if (opt == "URL of the ICANN WHOIS Data Problem Reporting System") {
                        cont = false;
                        goto default;
                    }

                    break;

                default:
                    break;
                }

                lOpt = opt;
            }

            return dom;
        }
    }
}
