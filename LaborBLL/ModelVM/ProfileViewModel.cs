
namespace LaborBLL.ModelVM
{
    /// <summary>
    /// ViewModel for user profile display and editing
    /// </summary>
    public class ProfileViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [StringLength(500, ErrorMessage = "Location cannot exceed 500 characters")]
        [Display(Name = "Location")]
        public string? Location { get; set; }

        [StringLength(100, ErrorMessage = "Country cannot exceed 100 characters")]
        [Display(Name = "Country")]
        public string? Country { get; set; }

        [Display(Name = "Location URL")]
        public string? LocationUrl { get; set; }

        [Display(Name = "Latitude")]
        public decimal? Latitude { get; set; }

        [Display(Name = "Longitude")]
        public decimal? Longitude { get; set; }

        [StringLength(2000, ErrorMessage = "Bio cannot exceed 2000 characters")]
        [Display(Name = "Bio")]
        public string? Bio { get; set; }

        [StringLength(1000, ErrorMessage = "Skills cannot exceed 1000 characters")]
        [Display(Name = "Skills")]
        public string? Skills { get; set; }

        [Display(Name = "Profile Picture")]
        public string? ProfilePictureUrl { get; set; }

        [Display(Name = "ID Verified")]
        public bool IDVerified { get; set; }

        [Display(Name = "Average Rating")]
        public decimal? AverageRating { get; set; }

        [Display(Name = "Member Since")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User role (Admin, Worker, Poster, or combinations)
        /// </summary>
        [Display(Name = "Role")]
        public ClientRole Role { get; set; }

        /// <summary>
        /// Returns true if user has Admin role
        /// </summary>
        public bool IsAdmin => Role.HasFlag(ClientRole.Admin);

        /// <summary>
        /// Returns true if user has Worker role
        /// </summary>
        public bool IsWorker => Role.HasFlag(ClientRole.Worker);

        /// <summary>
        /// Returns true if user has Poster role
        /// </summary>
        public bool IsPoster => Role.HasFlag(ClientRole.Poster);
    }
}
