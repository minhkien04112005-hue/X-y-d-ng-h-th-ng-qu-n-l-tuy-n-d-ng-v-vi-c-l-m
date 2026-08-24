using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentAPI.Models;
using RecruitmentAPI.Services;
using RecruitmentAPI.ViewModels;

namespace RecruitmentAPI.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly JobService _jobService;

        public HomeController(JobService jobService)
        {
            _jobService = jobService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            List<Job> jobs = await _jobService.GetAllJobsAsync();

            HomeViewModel model = new HomeViewModel
            {
                DisplayName = User.Identity?.Name ?? "Người dùng",
                ActiveJobs = jobs
                    .Where(job => !job.IsDeleted && job.Deadline >= DateTime.Now)
                    .OrderBy(job => job.Deadline)
                    .ToList()
            };

            return View(model);
        }
    }
}
