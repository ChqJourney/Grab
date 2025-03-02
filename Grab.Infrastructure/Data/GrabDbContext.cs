using Grab.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Grab.Infrastructure.Data
{
    public class GrabDbContext : DbContext
    {
        public GrabDbContext(DbContextOptions<GrabDbContext> options)
            : base(options)
        {
        }

        public DbSet<Directory> Directories { get; set; } = null!;
        public DbSet<FileItem> Files { get; set; } = null!;
        public DbSet<Core.Models.Task> Tasks { get; set; } = null!;
        public DbSet<DataExtractRule> DataExtractRules { get; set; } = null!;
        public DbSet<ExtractedData> ExtractedData { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure Directory entity
            modelBuilder.Entity<Directory>()
                .ToTable("Directories")
                .HasKey(d => d.Path);

            modelBuilder.Entity<Directory>()
                .Property(d => d.Status)
                .HasConversion<string>();

            // Configure FileItem entity
            modelBuilder.Entity<FileItem>()
                .ToTable("Files")
                .HasKey(f => f.Path);

            modelBuilder.Entity<FileItem>()
                .HasOne<Directory>()
                .WithMany()
                .HasForeignKey(f => f.DirectoryPath)
                .IsRequired();

            modelBuilder.Entity<FileItem>()
                .Property(f => f.Status)
                .HasConversion<string>();

            // Configure Task entity
            modelBuilder.Entity<Core.Models.Task>()
                .ToTable("Tasks")
                .HasKey(t => t.Id);

            modelBuilder.Entity<Core.Models.Task>()
                .Property(t => t.TargetFileType)
                .HasConversion<string>();

            // Configure DataExtractRule entity
            modelBuilder.Entity<DataExtractRule>()
                .ToTable("DataExtractRules")
                .HasKey(r => r.Id);

            modelBuilder.Entity<DataExtractRule>()
                .Property(r => r.FileType)
                .HasConversion<string>();

            modelBuilder.Entity<DataExtractRule>()
                .HasOne(r => r.Task)
                .WithMany(t => t.ExtractRules)
                .HasForeignKey(r => r.TaskId)
                .IsRequired();

            // Configure ExtractedData entity
            modelBuilder.Entity<ExtractedData>()
                .ToTable("ExtractedData")
                .HasKey(e => e.Id);
        }
    }
}
