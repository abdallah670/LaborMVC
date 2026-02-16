namespace LaborDAL.Enums
{
    /// <summary>
    /// Defines the verification tier of a user
    /// </summary>
    public enum VerificationTier
    {
        /// <summary>
        /// Unverified - limited to $100 tasks
        /// </summary>
        Unverified = 0,

        /// <summary>
        /// Email verified - basic trust level
        /// </summary>
        EmailVerified = 1,

        /// <summary>
        /// Phone verified - medium trust level
        /// </summary>
        PhoneVerified = 2,

        /// <summary>
        /// ID verified - full trust level, no task limits
        /// </summary>
        IDVerified = 3
    }
}
