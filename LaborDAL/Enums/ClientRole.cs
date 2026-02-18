

namespace LaborDAL.Enums
{
    /// <summary>
    /// Defines the role of a user (can be combined using flags)
    /// Examples: 
    /// - Worker only: ClientRole.Worker
    /// - Poster only: ClientRole.Poster
    /// - Both Worker and Poster: ClientRole.Both (or ClientRole.Worker | ClientRole.Poster)
    /// - Admin only: ClientRole.Admin
    /// - Admin + Worker: ClientRole.AdminWorker (or ClientRole.Admin | ClientRole.Worker)
    /// - Admin + Poster: ClientRole.AdminPoster (or ClientRole.Admin | ClientRole.Poster)
    /// - Admin + Worker + Poster: ClientRole.AdminBoth (or ClientRole.Admin | ClientRole.Worker | ClientRole.Poster)
    /// </summary>
    [Flags]
    public enum ClientRole
    {
        /// <summary>
        /// No role assigned
        /// </summary>
        None = 0,

        /// <summary>
        /// Worker - can apply for tasks and perform work
        /// </summary>
        Worker = 1,

        /// <summary>
        /// Poster - can post tasks and hire workers
        /// </summary>
        Poster = 2,

        /// <summary>
        /// Both Worker and Poster - can apply for tasks AND post tasks
        /// </summary>
        Both = Worker | Poster, // 3

        /// <summary>
        /// Administrator with full system access
        /// </summary>
        Admin = 4,

        /// <summary>
        /// Admin + Worker - administrator who can also apply for tasks
        /// </summary>
        AdminWorker = Admin | Worker, // 5

        /// <summary>
        /// Admin + Poster - administrator who can also post tasks
        /// </summary>
        AdminPoster = Admin | Poster, // 6

        /// <summary>
        /// Admin + Worker + Poster - full access to all roles
        /// </summary>
        AdminBoth = Admin | Worker | Poster // 7
    }
}
