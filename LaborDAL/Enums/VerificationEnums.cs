namespace LaborDAL.Enums
{
    /// <summary>
    /// Types of ID documents for verification
    /// </summary>
    public enum IdDocumentType
    {
        Passport,
        NationalId,
        DriversLicense,
        ResidencePermit
    }

    /// <summary>
    /// Status of ID verification request
    /// </summary>
    public enum VerificationStatus
    {
        Pending,
        InReview,
        Approved,
        Rejected,
        RequiresResubmission
    }
}
