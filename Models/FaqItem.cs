using System.ComponentModel.DataAnnotations;

namespace HealthInsuranceManagement.Models
{
    public class FaqItem
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Question is required.")]
        [StringLength(200, MinimumLength = 8, ErrorMessage = "Question must be between 8 and 200 characters.")]
        public string Question { get; set; } = string.Empty;

        [Required(ErrorMessage = "Answer is required.")]
        [StringLength(2000, MinimumLength = 20, ErrorMessage = "Answer must be between 20 and 2000 characters.")]
        public string Answer { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required.")]
        [StringLength(80, MinimumLength = 3, ErrorMessage = "Category must be between 3 and 80 characters.")]
        public string Category { get; set; } = "General";

        [Range(0, 999, ErrorMessage = "Display order must be between 0 and 999.")]
        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Show on public FAQ")]
        public bool IsActive { get; set; } = true;

        public int? CreatedBySupportId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
