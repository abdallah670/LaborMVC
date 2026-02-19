namespace LaborDAL.Enums
{
    /// <summary>
    /// Defines how a dispute was resolved
    /// </summary>
    public enum ResolutionType
    {
        /// <summary>
        /// Worker wins - full payment released to worker
        /// </summary>
        WorkerWins = 1,

        /// <summary>
        /// Poster wins - full refund to poster
        /// </summary>
        PosterWins = 2,

        /// <summary>
        /// Payment split evenly 50/50 between parties
        /// </summary>
        SplitEvenly = 3,

        /// <summary>
        /// Custom percentage split defined by admin
        /// </summary>
        CustomSplit = 4
    }
}
