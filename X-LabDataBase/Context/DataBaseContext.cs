using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using X_LabDataBase.Entityes;

namespace X_LabDataBase.Context
{
    public class DataBaseContext : IdentityDbContext<Person>
    {
        public DataBaseContext(DbContextOptions<DataBaseContext> options)
        : base(options) { }

        public DbSet<Person> Perons{ get; set; }
    }
}

