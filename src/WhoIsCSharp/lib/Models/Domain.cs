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

        public string
            Name;                   // Domain Name: GOOGLE.COM
        public string
            WIServer,               //
            ReferralURL;            //
        public string[]
            NameServers;            //
        public DStatus[]
            Status;                 //
        public DateTime
            DateUpdated,            //
            DateCreated,            // 
            DateExpiring,           // 
            DateWIUpdated;          // >>> Last update of whois database: Mon, 07 Nov 2016 14:13:04 GMT <<<
        public string 
            Reseller;

        public DStatus              DomainStatus;
        public Registrant           Registrant;
        public Staff                Staff;

        public string               DNSSEC;

        public Registrar            Registrar;
        

        public bool Exists;

        public bool isExpired() {
            // Check if the Date expiring has already passed
            return (DateExpiring <= DateTime.Now);
        }
    }
}
