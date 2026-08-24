using System.ComponentModel.DataAnnotations;

namespace RecruitmentAPI.DTOs
{
    public class CreateJobDto
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public string Deadline { get; set; } = string.Empty;
    }
}
