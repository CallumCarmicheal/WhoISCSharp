using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhoIsCSharp.lib.Models {
    class Staff {
        public Common.StaffMember Admin;
        public Common.StaffMember Tech;
        public Common.StaffMember Registrant;

        public Staff() {
            Admin = new Common.StaffMember();
            Tech = new Common.StaffMember();
            Registrant = new Common.StaffMember();
        }
    }
}
