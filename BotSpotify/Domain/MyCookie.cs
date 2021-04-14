using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSpotify.Domain
{
    public class MyCookie
    {
        public long Id { get; set; }
        public string Domain { get; set; }
        public string Expire { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public string Path { get; set; }

        public virtual User User { get; set; }
    }
}
