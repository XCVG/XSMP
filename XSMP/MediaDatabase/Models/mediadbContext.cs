using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace XSMP.MediaDatabase.Models
{
    public partial class mediadbContext : DbContext
    {
        public mediadbContext()
        {
        }

        public mediadbContext(DbContextOptions<mediadbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Album> Album { get; set; }
        public virtual DbSet<Artist> Artist { get; set; }
        public virtual DbSet<ArtistSong> ArtistSong { get; set; }
        public virtual DbSet<Song> Song { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite($"DataSource={System.IO.Path.Combine(Config.LocalDataFolderPath, "mediadb.sqlite")}"); //TODO pass this in instead
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "2.2.6-servicing-10079");

            modelBuilder.Entity<Album>(entity =>
            {
                entity.HasKey(e => new { e.Name, e.ArtistName });

                entity.HasOne(d => d.ArtistNameNavigation)
                    .WithMany(p => p.Album)
                    .HasForeignKey(d => d.ArtistName)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Artist>(entity =>
            {
                entity.Property(e => e.Name).ValueGeneratedNever();
            });

            modelBuilder.Entity<ArtistSong>(entity =>
            {
                entity.HasKey(e => new { e.SongHash, e.ArtistName });

                entity.HasOne(d => d.ArtistNameNavigation)
                    .WithMany(p => p.ArtistSong)
                    .HasForeignKey(d => d.ArtistName)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.SongHashNavigation)
                    .WithMany(p => p.ArtistSong)
                    .HasForeignKey(d => d.SongHash)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Song>(entity =>
            {
                entity.Property(e => e.Hash).ValueGeneratedNever();
            });
        }
    }
}
