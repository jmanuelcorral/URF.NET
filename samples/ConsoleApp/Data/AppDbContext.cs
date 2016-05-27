using Microsoft.EntityFrameworkCore;
using URF.Core.EFCore;
using ConsoleApp.Models;

namespace ConsoleApp.Data
{
    public class AppDbContext : DataContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        public DbSet<Person> Persons { get; set; }
    }
}
