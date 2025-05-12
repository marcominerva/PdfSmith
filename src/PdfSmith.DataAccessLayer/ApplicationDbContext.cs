using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PdfSmith.DataAccessLayer.Entities;

namespace PdfSmith.DataAccessLayer;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Subscription> Subscriptions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
