using System.ComponentModel.DataAnnotations;

namespace RecruitmentAPI.DTOs
{
    public class UpdateJobDto
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// Định dạng: dd-MM-yyyy (ví dụ: 24-08-2026)
        [Required]
        public string Deadline { get; set; } = string.Empty;
    }
}
