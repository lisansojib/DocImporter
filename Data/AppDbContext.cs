using Microsoft.EntityFrameworkCore;

namespace DocImporter.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public virtual DbSet<CuratedMusic> CuratedMusics { get; set; }
    }
}
