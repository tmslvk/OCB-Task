using Microsoft.EntityFrameworkCore;
using webapi.Model;

namespace webapi.Context
{
    public class OCBContext : DbContext
    {
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Document> Documents { get; set; } = null!;
        public DbSet<Outlay> Outlays { get; set; } = null!;
        public DbSet<ExcelFile> ExcelFiles { get; set; } = null!;
        public OCBContext()
        {
        }

        public OCBContext(DbContextOptions<OCBContext> options) : base(options)
        {
            Database.EnsureCreated();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Outlay>().HasKey(o => o.Id);
            //outlay-category
            modelBuilder.Entity<Outlay>()
                .HasOne(o => o.Category)
                .WithMany(c => c.Outlays)
                .HasForeignKey(o => o.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            //outlay-document
            modelBuilder.Entity<Outlay>()
                .HasOne(o => o.Document)
                .WithMany(c => c.Outlays)
                .HasForeignKey(o => o.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Document>()
                .HasOne(d=>d.ExcelFile)
                .WithOne(ef=>ef.Document)
                .HasForeignKey<Document>(d=>d.ExcelFileId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
