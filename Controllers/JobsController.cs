using Microsoft.AspNetCore.Mvc;
using RecruitmentAPI.DTOs;
using RecruitmentAPI.Services;

namespace RecruitmentAPI.Controllers
{
    [ApiController]
    [Route("api/jobs")]
    public class JobsController : ControllerBase
    {
        private readonly ApplicationService _applicationService;
        private readonly JobService _jobService;

        public JobsController(ApplicationService applicationService, JobService jobService)
        {
            _applicationService = applicationService;
            _jobService = jobService;
        }

        /// Danh sách tất cả tin tuyển dụng (bao gồm cả tin đã xóa mềm).
        [HttpGet]
        public async Task<IActionResult> GetJobs()
        {
            var jobs = await _jobService.GetAllJobsAsync();
            return Ok(jobs);
        }

        /// Kiểm tra tin tuyển dụng có tồn tại hay không và lấy thông tin Deadline.
        [HttpGet("{jobId}")]
        public async Task<IActionResult> GetJob(int jobId)
        {
            var job = await _jobService.GetJobByIdAsync(jobId);

            if (job == null)
            {
                return NotFound(new { message = "Không tìm thấy tin tuyển dụng." });
            }

            return Ok(job);
        }

        /// Tạo tin tuyển dụng mới.
        [HttpPost]
        public async Task<IActionResult> CreateJob([FromBody] CreateJobDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _jobService.CreateJobAsync(dto);

            if (!result.Success)
            {
                var body = new { errorCode = result.ErrorCode, message = result.Message };
                return result.ErrorType switch
                {
                    ServiceErrorType.NotFound => NotFound(body),
                    ServiceErrorType.Conflict => Conflict(body),
                    _ => BadRequest(body)
                };
            }

            return CreatedAtAction(nameof(GetJob), new { jobId = result.Data!.Id }, result.Data);
        }

        /// Sửa tin tuyển dụng (không áp dụng cho tin đã bị xóa).
        [HttpPut("{jobId}")]
        public async Task<IActionResult> UpdateJob(int jobId, [FromBody] UpdateJobDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _jobService.UpdateJobAsync(jobId, dto);

            if (!result.Success)
            {
                var body = new { errorCode = result.ErrorCode, message = result.Message };
                return result.ErrorType switch
                {
                    ServiceErrorType.NotFound => NotFound(body),
                    ServiceErrorType.Conflict => Conflict(body),
                    _ => BadRequest(body)
                };
            }

            return Ok(result.Data);
        }

        /// Xóa tin tuyển dụng.
        [HttpDelete("{jobId}")]
        public async Task<IActionResult> DeleteJob(int jobId)
        {
            var result = await _jobService.DeleteJobAsync(jobId);

            if (!result.Success)
            {
                var body = new { errorCode = result.ErrorCode, message = result.Message };
                return result.ErrorType switch
                {
                    ServiceErrorType.NotFound => NotFound(body),
                    ServiceErrorType.Conflict => Conflict(body),
                    _ => BadRequest(body)
                };
            }

            return Ok(new { message = "Đã xóa tin tuyển dụng (xóa mềm)." });
        }

        /// Kiểm tra ứng viên đã ứng tuyển vào tin tuyển dụng này hay chưa.
        [HttpGet("{jobId}/applications/check")]
        public async Task<IActionResult> CheckApplied(int jobId, [FromQuery] int candidateId)
        {
            var job = await _applicationService.GetJobByIdAsync(jobId);
            if (job == null)
            {
                return NotFound(new { message = "Không tìm thấy tin tuyển dụng." });
            }

            bool alreadyApplied = await _applicationService.HasCandidateAppliedAsync(jobId, candidateId);

            return Ok(new { jobId, candidateId, alreadyApplied });
        }

        /// Tạo hồ sơ ứng tuyển.>
        [HttpPost("{jobId}/applications")]
        public async Task<IActionResult> Apply(int jobId, [FromBody] CreateApplicationDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _applicationService.CreateApplicationAsync(jobId, dto.CandidateId);

            if (!result.Success)
            {
                var body = new { errorCode = result.ErrorCode, message = result.Message };

                return result.ErrorType switch
                {
                    ServiceErrorType.NotFound => NotFound(body),
                    ServiceErrorType.Conflict => Conflict(body),
                    _ => BadRequest(body)
                };
            }

            return CreatedAtAction(
                nameof(ApplicationsController.GetApplication),
                "Applications",
                new { applicationId = result.Data!.Id },
                result.Data);
        }
    }
}
