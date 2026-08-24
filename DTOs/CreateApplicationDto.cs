using System.ComponentModel.DataAnnotations;

namespace RecruitmentAPI.DTOs
{
    public class CreateApplicationDto
    {
        [Required]
        public int CandidateId { get; set; }
    }
}
