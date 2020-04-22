using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterConnector.Models.Database
{
    public class DBUser : DBData
    {
        private String user_id;
        private String last_active;
        private String phone;
        private String fname;
        private String lname;
        private String email;

        public String User_id { get => user_id; set => user_id = value; }
        public String Last_active { get => last_active; set => last_active = value; }
        public string Phone { get => phone; set => phone = value; }
        public string Fname { get => fname; set => fname = value; }
        public string Lname { get => lname; set => lname = value; }
        public string Email { get => email; set => email = value; }
    }
}
