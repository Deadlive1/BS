using BotSpotify.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotBitcointalk.Domain.DbContext
{
    public class BotContext : System.Data.Entity.DbContext
    {
#if DEBUG
        private const string connectionString = @"Data Source=../../bin/release/BotSpotifyDB.sdf;Persist Security Info=True";
#else
        private const string connectionString = @"Data Source=BotSpotifyDB.sdf;Persist Security Info=True";
#endif

        public BotContext() : base(connectionString)
        {
        }

        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<UserUrl> UserUrls { get; set; }
        public virtual DbSet<Listening> Listenings { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Database.SetInitializer(new CreateDatabaseIfNotExists<BotContext>());
            base.OnModelCreating(modelBuilder);
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            modelBuilder.Entity<User>()
               .HasMany<MyCookie>(x => x.Cookies)
               .WithOptional()
               .WillCascadeOnDelete(true);

            modelBuilder.Entity<User>()
             .HasMany<UserUrl>(x => x.UserUrls)
             .WithOptional()
             .WillCascadeOnDelete(true);

            modelBuilder.Entity<User>()
              .HasMany<Listening>(x => x.Listenings)
              .WithOptional()
              .WillCascadeOnDelete(true);
        }
    }
}
