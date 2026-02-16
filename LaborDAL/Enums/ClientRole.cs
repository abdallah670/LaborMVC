using System;

namespace LaborDAL.Enums
{
    /// <summary>
    /// Defines the role of a user (can be combined using flags)
    /// Examples: 
    /// - Worker only: ClientRole.Worker
    /// - Poster only: ClientRole.Poster
    /// - Both Worker and Poster: ClientRole.Worker | ClientRole.Poster
    /// - Admin only: ClientRole.Admin
    /// - Admin + Worker: ClientRole.Admin | ClientRole.Worker
    /// - Admin + Poster: ClientRole.Admin | ClientRole.Poster
    /// - Admin + Worker + Poster: ClientRole.Admin | ClientRole.Worker | ClientRole.Poster
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
        /// Administrator with full system access
        /// </summary>
        Admin = 4
    }
}
