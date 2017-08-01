using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TenantManagement
{
    public class AddReportRequest
    {
        public class Credentials
        {
            public string userid { get; set; }
            public string password { get; set; }
        }

        public string name { get; set; }
        public string report { get; set; }
        public string server { get; set; }
        public string database { get; set; }
        public Credentials credentials { get; set; }
    }
}
