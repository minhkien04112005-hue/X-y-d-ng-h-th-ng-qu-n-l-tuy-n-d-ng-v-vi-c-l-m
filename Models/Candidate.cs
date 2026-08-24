namespace RecruitmentAPI.Models
{
    public class Candidate
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public bool IsDeleted { get; set; }
    }
}
