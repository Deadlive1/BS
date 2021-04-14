using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSpotify.Domain
{
    public class User
    {
        public long Id { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public SiteTypes SiteType { get; set; }
        [NotMapped]
        public string IsAuthorizedText { get { return IsAuthorized ? "Да" : "Нет"; } }
        public bool IsAuthorized { get; set; }
        public string Status { get; set; }
        public bool IsWork { get; set; }

        public enum SiteTypes { Spotify, Deezer, Teedal }

        public virtual ICollection<MyCookie> Cookies { get; set; }
        public virtual ICollection<UserUrl> UserUrls { get; set; }
        public virtual ICollection<Listening> Listenings { get; set; }

        public User()
        {
            Cookies = new List<MyCookie>();
            UserUrls = new List<UserUrl>();
            Listenings = new List<Listening>();
        }
    }
}
