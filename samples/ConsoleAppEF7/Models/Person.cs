using System;
using URF.EntityFramework;

namespace ConsoleAppEF7.Models
{
    public class Person : Entity
    {
        public int Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public DateTime BirthDate { get; set; }
    }
}
