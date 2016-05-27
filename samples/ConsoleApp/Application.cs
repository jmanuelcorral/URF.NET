using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using URF.Core.Repositories;
using URF.Core.UnitOfWork;
using ConsoleApp.Models;

namespace ConsoleApp
{
    public class Application
    {
        private readonly IRepositoryAsync<Person> _personRepository;
        private readonly IUnitOfWorkAsync _unitOfWork;

        public Application(IUnitOfWorkAsync unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _personRepository = _unitOfWork.RepositoryAsync<Person>();
        }

        public async Task Run()
        {
            var person = await InsertPersonAsync();

            await PrintPersonsAsync();

            await DeletePersonAsync(person);

            await PrintPersonsAsync();

            Console.ReadKey();
        }

        public async Task<Person> InsertPersonAsync()
        {
            try
            {
                _unitOfWork.BeginTransaction();

                var person = new Person
                {
                    FirstName = "Mike",
                    LastName = "Mazmanyan",
                    BirthDate = DateTime.Now
                };

                _personRepository.Insert(person);

                await _unitOfWork.SaveChangesAsync();

                return person;
            }
            catch
            {
                _unitOfWork.Rollback();
            }

            return null;
        }

        public async Task DeletePersonAsync(Person person)
        {
            _personRepository.Delete(person);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task PrintPersonsAsync()
        {
            Console.WriteLine("Persons Table");

            var persons = await _personRepository.Queryable().ToListAsync();

            if (persons.Any())
            {
                foreach (var person in persons)
                {
                    Console.WriteLine($"{person.Id} {person.FirstName} {person.LastName} {person.BirthDate}");
                }
            }
            else
            {
                Console.WriteLine("Empty");
            }
        }
    }
}
