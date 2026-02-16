

namespace LaborDAL.DB
{
    /// <summary>
    /// Design-time factory for ApplicationDbContext used by EF Core tools
    /// </summary>
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            
            // Use a connection string for design-time operations
            // This can be overridden by command line arguments
            var connectionString = args.Length > 0 
                ? args[0] 
                : "Server=.;Database=LaborMVC;Integrated Security=SSPI;TrustServerCertificate=True";
            
            optionsBuilder.UseSqlServer(connectionString);
            
            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}