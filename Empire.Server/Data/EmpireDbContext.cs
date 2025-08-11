using Microsoft.EntityFrameworkCore;
using Empire.Server.Models;

namespace Empire.Server.Data
{
    public class EmpireDbContext : DbContext
    {
        public EmpireDbContext(DbContextOptions<EmpireDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserDeck> UserDecks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            });

            // Configure UserDeck entity
            modelBuilder.Entity<UserDeck>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DeckName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ArmyCards).IsRequired();
                entity.Property(e => e.CivicCards).IsRequired();
                
                // Foreign key relationship
                entity.HasOne(d => d.User)
                      .WithMany()
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
