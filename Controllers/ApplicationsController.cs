using Microsoft.AspNetCore.Mvc;
using RecruitmentAPI.DTOs;
using RecruitmentAPI.Services;

namespace RecruitmentAPI.Controllers
{
    [ApiController]
    [Route("api/applications")]
    public class ApplicationsController : ControllerBase
    {
        private readonly ApplicationService _applicationService;

        public ApplicationsController(ApplicationService applicationService)
        {
            _applicationService = applicationService;
        }


        // Xem thông tin/trạng thái hồ sơ ứng tuyển.
        [HttpGet("{applicationId}")]
        public async Task<IActionResult> GetApplication(int applicationId)
        {
            var application = await _applicationService.GetApplicationByIdAsync(applicationId);

            if (application == null)
            {
                return NotFound(new { message = "Không tìm thấy hồ sơ ứng tuyển." });
            }

            return Ok(application);
        }

        // Chuyển trạng thái hồ sơ ứng tuyển theo đúng quy trình:
        //Received -> Screening -> Interview -> Passed/Failed
        [HttpPatch("{applicationId}/status")]
        public async Task<IActionResult> UpdateStatus(int applicationId, [FromBody] UpdateApplicationStatusDto dto)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(dto.Status))
            {
                return BadRequest(new { message = "Trạng thái không hợp lệ." });
            }

            var result = await _applicationService.UpdateApplicationStatusAsync(applicationId, dto.Status);

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
    }
}
