using Microsoft.EntityFrameworkCore;
using ConsoleAppEF7.Models;
using URF.EntityFramework;

namespace ConsoleAppEF7.Data
{
    public class AppDbContext : DataContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        public DbSet<Person> Persons { get; set; }
    }
}
