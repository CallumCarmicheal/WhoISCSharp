using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhoIsCSharp.lib.Models {
    class Domain {
        public class DStatus {
            public string
                Event,
                IcannURl;
        }

        public string               Name            = "";
        public string               Reseller        = "";
        public string[]             NameServers     ; 
        public DStatus[]            Status          ;        
        public DateTime             DateUpdated     ;
        public DateTime             DateCreated     ;
        public DateTime             DateExpiring    ;
        public DateTime             DateWIUpdated   ;
        public DStatus[]            DomainStatus    ;
        public Staff                Staff           ;
        public string               DNSSEC          = "unsigned";
        public Common.SimpleWIData  WhoIs           ;
        public Registrar            Registrar       ;
        

        public Domain() {
            Staff = new Staff();
            Registrar = new Registrar();
            WhoIs = new Common.SimpleWIData();
        }
    }
}
