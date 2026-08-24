using RecruitmentAPI.Models;

namespace RecruitmentAPI.ViewModels
{
    public class HomeViewModel
    {
        public string DisplayName { get; set; } = string.Empty;
        public List<Job> ActiveJobs { get; set; } = new();
    }
}
