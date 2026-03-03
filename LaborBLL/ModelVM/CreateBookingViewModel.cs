
using System.ComponentModel.DataAnnotations;

namespace LaborBLL.ModelVM
{
    public class CreateBookingViewModel
    {
        [Required(ErrorMessage = "Agreed rate is required")]
        [Range(0.01, 100000, ErrorMessage = "Agreed rate must be between 0.01 and 100,000")]
        [Display(Name = "Agreed Rate")]
        public decimal AgreedRate { get; set; }

        [Required(ErrorMessage = "Start time is required")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Start Time")]
        public DateTime StartTime { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "End time is required")]
        [DataType(DataType.DateTime)]
        [Display(Name = "End Time")]
        [CustomValidation(typeof(CreateBookingViewModel), nameof(ValidateEndTime))]
        public DateTime EndTime { get; set; } = DateTime.Now.AddHours(1);

        [Required(ErrorMessage = "Task ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid task ID")]
        public int TaskId { get; set; }

        [Required(ErrorMessage = "Poster ID is required")]
        public string PosterId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Worker ID is required")]
        public string WorkerId { get; set; } = string.Empty;

        public static ValidationResult? ValidateEndTime(DateTime endTime, ValidationContext context)
        {
            var instance = (CreateBookingViewModel)context.ObjectInstance;
            if (endTime <= instance.StartTime)
            {
                return new ValidationResult("End time must be after start time");
            }
            return ValidationResult.Success;
        }
    }
}
