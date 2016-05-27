using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using URF.Core.DataContext;
using URF.Core.EFCore;
using URF.Core.UnitOfWork;
using URF.Core.Repositories;
using ConsoleApp.Data;
using ConsoleApp.Models;

namespace ConsoleApp
{
    public class Program
    {
        public static IServiceProvider ServiceProvider { get; }
        public static IConfigurationRoot Configuration { get; }

        static Program()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            Configuration = builder.Build();
            
            var services = new ServiceCollection();

            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
            //services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase());

            services.AddScoped<IDataContextAsync, AppDbContext>();
            services.AddScoped<IUnitOfWorkAsync, UnitOfWork>();
            services.AddScoped<IRepositoryAsync<Person>, Repository<Person>>();

            services.AddSingleton<Application>();

            ServiceProvider = services.BuildServiceProvider();
         }

        public static void Main(string[] args)
        {
            try
            {
                var app = ServiceProvider.GetService<Application>();

                app.Run().Wait();
            }
            finally
            {
                ((IDisposable)ServiceProvider).Dispose();
            }
        }
    }
}
