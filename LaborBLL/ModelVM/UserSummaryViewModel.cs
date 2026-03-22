namespace LaborBLL.ModelVM
{
    public class UserSummaryViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? ProfilePicture { get; set; }
        public decimal? Rating { get; set; }
        public bool IsVerified { get; set; }
    }
}
