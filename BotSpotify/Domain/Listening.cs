using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BotSpotify.Domain.User;

namespace BotSpotify.Domain
{
    public class Listening
    {
        public long Id { get; set; }
        public string Url { get; set; }
        public DateTime DateStart { get; set; }
        public DateTime DateEnd { get; set; }
        public SiteTypes SiteType { get; set; }

        public virtual User User { get; set; }
    }
}
