namespace LaborDAL.Enums
{
    /// <summary>
    /// Defines the budget type for a task
    /// </summary>
    public enum BudgetType
    {
        /// <summary>
        /// Fixed price for the entire task
        /// </summary>
        Fixed = 1,

        /// <summary>
        /// Hourly rate
        /// </summary>
        Hourly = 2,

        /// <summary>
        /// Negotiable budget
        /// </summary>
        Negotiable = 3
    }
}
