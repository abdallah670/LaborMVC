using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LaborDAL.Enums;

namespace LaborBLL.ModelVM
{
    public class UserProfileDisplayViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? LocationUrl { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Bio { get; set; }
        public string? Skills { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public bool IDVerified { get; set; }
        public bool PhoneNumberVerified { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public VerificationTier VerificationTier { get; set; }
        public decimal? AverageRating { get; set; }
        public int TotalRatingsCount { get; set; }
        public DateTime MemberSince { get; set; }
        public ClientRole Role { get; set; }

        public bool IsWorker => Role.HasFlag(ClientRole.Worker);
        public bool IsPoster => Role.HasFlag(ClientRole.Poster);
        public bool IsAdmin => Role.HasFlag(ClientRole.Admin);
        public bool IsDeleted { get; set; }

        // Statistics
        public int CompletedJobsAsWorker { get; set; }
        public decimal TotalEarnings { get; set; }
        public int TotalHires { get; set; }
        public decimal TotalSpent { get; set; }
        public int TasksPosted { get; set; }

        public List<AllRatingViewModel> RecentRatings { get; set; } = new();
    }

    public class UserProfileUpdateModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Bio { get; set; }

        [StringLength(500)]
        public string? Skills { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        public string? LocationUrl { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
}
