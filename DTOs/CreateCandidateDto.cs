using System.ComponentModel.DataAnnotations;

namespace RecruitmentAPI.DTOs
{
    public class CreateCandidateDto
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }
    }
}
