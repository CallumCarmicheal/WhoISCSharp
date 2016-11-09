using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhoIsCSharp.lib.Models.Common {
    class StaffMember {
        public string
            ID,
            Name,
            Organisation;
        public Common.Location
            Location;
        public Common.Contact
            Contact;

        public StaffMember() {
            Location = new Location();
            Contact = new Contact();
        }
    }
}
