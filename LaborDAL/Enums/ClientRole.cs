

namespace LaborDAL.Enums
{
    /// <summary>
    /// Defines the role of a client user (can be combined using flags)
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
        /// Both Worker and Poster roles
        /// </summary>
        Both = Worker | Poster
    }
}
