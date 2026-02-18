

namespace LaborDAL.DB
{
    /// <summary>
    /// Application DbContext with Identity support and soft delete functionality
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Bookings DbSet
        /// </summary>
        public DbSet<Booking> Bookings { get; set; }

        /// <summary>
        /// Tasks DbSet
        /// </summary>
        public DbSet<TaskItem> Tasks { get; set; }

        /// <summary>
        /// Task applications DbSet
        /// </summary>
        public DbSet<TaskApplication> TaskApplications { get; set; }


        /// <summary>
        /// Override SaveChanges to implement soft delete and audit functionality
        /// </summary>
        public override int SaveChanges()
        {
            UpdateSoftDeleteStatuses();
            UpdateAuditFields();
            return base.SaveChanges();
        }

        /// <summary>
        /// Override SaveChangesAsync to implement soft delete and audit functionality
        /// </summary>
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateSoftDeleteStatuses();
            UpdateAuditFields();
            return base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Update soft delete status for entities being deleted
        /// </summary>
        private void UpdateSoftDeleteStatuses()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Deleted);

            foreach (var entry in entries)
            {
                // Check if entity has IsDeleted property (soft delete support)
                var property = entry.Entity.GetType().GetProperty("IsDeleted");
                if (property != null)
                {
                    // Instead of deleting, mark as soft deleted
                    entry.State = EntityState.Unchanged;
                    property.SetValue(entry.Entity, true);

                    // Set DeletedAt timestamp
                    var deletedAtProperty = entry.Entity.GetType().GetProperty("DeletedAt");
                    deletedAtProperty?.SetValue(entry.Entity, DateTime.UtcNow);
                }
            }
        }

        /// <summary>
        /// Update audit fields (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
        /// </summary>
        private void UpdateAuditFields()
        {
            var now = DateTime.UtcNow;

            var addedEntries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added);

            foreach (var entry in addedEntries)
            {
                var createdAtProperty = entry.Entity.GetType().GetProperty("CreatedAt");
                createdAtProperty?.SetValue(entry.Entity, now);
            }

            var modifiedEntries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified);

            foreach (var entry in modifiedEntries)
            {
                var updatedAtProperty = entry.Entity.GetType().GetProperty("UpdatedAt");
                updatedAtProperty?.SetValue(entry.Entity, now);
            }
        }

        /// <summary>
        /// Configure the model and apply configurations
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all configurations from the Configuration folder
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // Configure decimal precision for coordinates
            modelBuilder.Entity<AppUser>()
                .Property(u => u.Latitude)
                .HasPrecision(10, 8);

            modelBuilder.Entity<AppUser>()
                .Property(u => u.Longitude)
                .HasPrecision(11, 8);

            // Configure decimal precision for TaskItem coordinates
            modelBuilder.Entity<TaskItem>()
                .Property(t => t.Latitude)
                .HasPrecision(10, 8);

            modelBuilder.Entity<TaskItem>()
                .Property(t => t.Longitude)
                .HasPrecision(11, 8);

            modelBuilder.Entity<TaskItem>()
                .Property(t => t.Budget)
                .HasPrecision(18, 2);

            modelBuilder.Entity<TaskItem>()
                .Property(t => t.EstimatedHours)
                .HasPrecision(10, 2);
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.Poster)
                .WithMany(u => u.PostedTasks)
                .HasForeignKey(t => t.PosterId);
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Task)
                .WithMany(t => t.Bookings)
                .HasForeignKey(b => b.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

            // Configure decimal precision for TaskApplication
            modelBuilder.Entity<TaskApplication>()
                .Property(ta => ta.ProposedBudget)
                .HasPrecision(18, 2);

            modelBuilder.Entity<TaskApplication>()
                .Property(ta => ta.EstimatedHours)
                .HasPrecision(10, 2);

            // Additional Identity configurations can be added here
        }
    }
}