using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSpotify.Domain
{
    public class UserUrl
    {
        public virtual long Id { get; set; }
        public string Value { get; set; }

        public virtual User User { get; set; }
    }
}
