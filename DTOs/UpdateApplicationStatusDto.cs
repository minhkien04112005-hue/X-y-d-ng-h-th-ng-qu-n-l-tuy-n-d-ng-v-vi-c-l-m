using System.ComponentModel.DataAnnotations;

namespace RecruitmentAPI.DTOs
{
    public class UpdateApplicationStatusDto
    {
        [Required]
        public string Status { get; set; } = string.Empty;
    }
}
