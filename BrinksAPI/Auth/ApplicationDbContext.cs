using BrinksAPI.Entities;
using BrinksAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BrinksAPI.Auth
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
        public DbSet<User> users { get; set; }
        public DbSet<AuthenticationLevel> AuthenticationLevels { get; set; }

        #region Mapping tables
        public DbSet<Entities.ServiceLevel> serviceLevels { get; set; }
        public DbSet<Entities.DocumentType> documentTypes { get; set; } 
        public DbSet<Entities.ActionType> actionTypes { get; set; } 
        public DbSet<Entities.Site> sites { get; set; } 
        public DbSet<Entities.RiskCodeDescription> riskCodeDescriptions { get; set; } 
        public DbSet<Entities.EventCode> eventCodes { get; set; } 
        #endregion
        public DbSet<Document> documents { get; set; }
    }
}
