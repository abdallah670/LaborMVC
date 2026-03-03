
namespace LaborDAL.Enums
{
    /// <summary>
    /// Status for Saga instances in the Saga pattern
    /// </summary>
    public enum SagaStatus
    {
        /// <summary>
        /// Saga is created but not started
        /// </summary>
        Created = 0,

        /// <summary>
        /// Saga is currently executing
        /// </summary>
        Running = 1,

        /// <summary>
        /// Saga completed successfully
        /// </summary>
        Completed = 2,

        /// <summary>
        /// Saga failed and compensation is being executed
        /// </summary>
        Compensating = 3,

        /// <summary>
        /// Saga completed with compensation
        /// </summary>
        Compensated = 4,

        /// <summary>
        /// Saga failed and could not be compensated
        /// </summary>
        Failed = 5
    }
}
