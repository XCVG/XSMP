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
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseSqlite($"DataSource={System.IO.Path.Combine(Config.DataFolderPath, "mediadb.sqlite")}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "2.2.6-servicing-10079");

            modelBuilder.Entity<Album>(entity =>
            {
                entity.HasKey(e => new { e.Name, e.ArtistName });

                entity.Property(e => e.Title).IsRequired();

                entity.HasOne(d => d.ArtistNameNavigation)
                    .WithMany(p => p.Album)
                    .HasForeignKey(d => d.ArtistName)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Artist>(entity =>
            {
                entity.HasKey(e => e.Name);

                entity.Property(e => e.Name).ValueGeneratedNever();

                entity.Property(e => e.NiceName).IsRequired();
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
                entity.HasKey(e => e.Hash);

                entity.Property(e => e.Hash).ValueGeneratedNever();

                entity.Property(e => e.AlbumArtistName).IsRequired();

                entity.Property(e => e.AlbumName).IsRequired();

                entity.Property(e => e.Title).IsRequired();
            });
        }
    }
}
