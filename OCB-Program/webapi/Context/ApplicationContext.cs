using Microsoft.EntityFrameworkCore;
using webapi.Model;

namespace webapi.Context
{
    public class ApplicationContext:DbContext
    {
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Document> Documents { get; set; } = null!;
        public DbSet<Outlay> Outlays { get; set; } = null!;
        public ApplicationContext()
        {

        }

        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
        {
            Database.EnsureCreated();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //outlay-category
            modelBuilder.Entity<Outlay>()
                .HasOne(o=>o.Category)
                .WithMany(c=>c.Outlays)
                .HasForeignKey(o=>o.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            //outlay-document
            modelBuilder.Entity<Outlay>()
                .HasOne(o => o.Document)
                .WithMany(c => c.Outlays)
                .HasForeignKey(o => o.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
