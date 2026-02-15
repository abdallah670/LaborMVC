namespace LaborDAL.Enums
{
    /// <summary>
    /// Defines the type of user in the system
    /// </summary>
    public enum UserType
    {
        /// <summary>
        /// Administrator with full system access
        /// </summary>
        Admin = 1,

        /// <summary>
        /// Regular client user (can be Worker, Poster, or both)
        /// </summary>
        Client = 2
    }
}
