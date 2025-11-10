using Bem_vindo.pt.Models;
using Microsoft.EntityFrameworkCore;

namespace Bem_Vindo.pt.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Guide> Guides { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<Reply> Replies { get; set; }
        public DbSet<UserFavoriteGuide> UserFavoriteGuides { get; set; }
        public DbSet<Evento> Eventos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserFavoriteGuide>()
                .HasKey(ufg => new { ufg.UserId, ufg.GuideId });

            modelBuilder.Entity<UserFavoriteGuide>()
                .HasOne(ufg => ufg.User)
                .WithMany(u => u.FavoriteGuides)
                .HasForeignKey(ufg => ufg.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserFavoriteGuide>()
                .HasOne(ufg => ufg.Guide)
                .WithMany(g => g.FavoritedByUsers)
                .HasForeignKey(ufg => ufg.GuideId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Topic>()
                .HasOne(t => t.Author)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reply>()
                .HasOne(r => r.Author)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reply>()
                .HasOne(r => r.ParentTopic)
                .WithMany(t => t.Replies)
                .HasForeignKey(r => r.TopicId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}