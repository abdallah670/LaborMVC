using System.ComponentModel.DataAnnotations;
using LaborDAL.Enums;

namespace LaborBLL.ModelVM
{
    /// <summary>
    /// ViewModel for editing user roles
    /// </summary>
    public class EditRolesViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Display(Name = "User Name")]
        public string UserName { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Profile Picture")]
        public string? ProfilePictureUrl { get; set; }

        /// <summary>
        /// Worker role - can apply for tasks and perform work
        /// </summary>
        [Display(Name = "Worker")]
        public bool IsWorker { get; set; }

        /// <summary>
        /// Poster role - can post tasks and hire workers
        /// </summary>
        [Display(Name = "Poster")]
        public bool IsPoster { get; set; }

        /// <summary>
        /// Admin role - full system access
        /// </summary>
        [Display(Name = "Administrator")]
        public bool IsAdmin { get; set; }

        /// <summary>
        /// Current role for display purposes
        /// </summary>
        public ClientRole CurrentRole { get; set; }

        /// <summary>
        /// Calculates the new role based on checkbox selections
        /// </summary>
        public ClientRole GetNewRole()
        {
            ClientRole newRole = ClientRole.None;
            if (IsWorker) newRole |= ClientRole.Worker;
            if (IsPoster) newRole |= ClientRole.Poster;
            if (IsAdmin) newRole |= ClientRole.Admin;
            return newRole;
        }
    }
}
