using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalendarToSlack.Sql
{
    public class SqlUser
    {
        public string EmailAddress { get; set; }
        public string SlackAuthToken { get; set; }
        public string DefaultStatusText { get; set; }
        public string DefaultStatusEmoji { get; set; }
    }
}
