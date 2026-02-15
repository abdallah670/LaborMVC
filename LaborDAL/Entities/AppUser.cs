using LaborDAL.Enums;
using Microsoft.AspNetCore.Identity;
using System;

namespace LaborDAL.Entities
{
    /// <summary>
    /// Extended user entity inheriting from ASP.NET Core Identity User
    /// </summary>
    public class AppUser : IdentityUser
    {
        /// <summary>
        /// User's first name
        /// </summary>
        public string? FirstName { get; set; }

        /// <summary>
        /// User's last name
        /// </summary>
        public string? LastName { get; set; }

        /// <summary>
        /// Indicates if the user's ID has been verified
        /// </summary>
        public bool IDVerified { get; set; } = false;

        /// <summary>
        /// User's average rating from reviews
        /// </summary>
        public decimal? AverageRating { get; set; }

        /// <summary>
        /// User's location URL (map link)
        /// </summary>
        public string? LocationUrl { get; set; }

        /// <summary>
        /// User's location (address)
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// User's geographic latitude (-90 to 90)
        /// </summary>
        public decimal? Latitude { get; set; }

        /// <summary>
        /// User's geographic longitude (-180 to 180)
        /// </summary>
        public decimal? Longitude { get; set; }

        /// <summary>
        /// User's country
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// URL to user's profile picture
        /// </summary>
        public string? ProfilePictureUrl { get; set; }

        /// <summary>
        /// User's bio/description
        /// </summary>
        public string? Bio { get; set; }

        /// <summary>
        /// User's skills (comma-separated)
        /// </summary>
        public string? Skills { get; set; }

        /// <summary>
        /// User type (Admin, Client)
        /// </summary>
        public UserType UserType { get; set; } = UserType.Client;

        /// <summary>
        /// Client role type (Worker, Poster, Both) - only applicable when UserType is Client
        /// </summary>
        public ClientRole ClientRole { get; set; } = ClientRole.Worker;

        /// <summary>
        /// Account creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Soft delete flag
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Deletion timestamp
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// ID of user who deleted this record
        /// </summary>
        public string? DeletedBy { get; set; }

        /// <summary>
        /// ID of user who last updated this record
        /// </summary>
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// ID of user who created this record
        /// </summary>
        public string? CreatedBy { get; set; }

        // Navigation properties will be added when Task and TaskApplication entities are created
        // public virtual ICollection<Task> PostedTasks { get; set; } = new List<Task>();
        // public virtual ICollection<TaskApplication> Applications { get; set; } = new List<TaskApplication>();
    }
}
