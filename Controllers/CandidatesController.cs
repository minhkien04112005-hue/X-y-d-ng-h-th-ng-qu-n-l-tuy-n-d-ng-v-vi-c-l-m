using Microsoft.AspNetCore.Mvc;
using RecruitmentAPI.DTOs;
using RecruitmentAPI.Services;

namespace RecruitmentAPI.Controllers
{
    [ApiController]
    [Route("api/candidates")]
    public class CandidatesController : ControllerBase
    {
        private readonly CandidateService _candidateService;

        public CandidatesController(CandidateService candidateService)
        {
            _candidateService = candidateService;
        }

        //Danh sách tất cả ứng viên.
        [HttpGet]
        public async Task<IActionResult> GetCandidates()
        {
            var candidates = await _candidateService.GetAllCandidatesAsync();
            return Ok(candidates);
        }

        //Xem thông tin ứng viên.
        [HttpGet("{candidateId}")]
        public async Task<IActionResult> GetCandidate(int candidateId)
        {
            var candidate = await _candidateService.GetCandidateByIdAsync(candidateId);

            if (candidate == null)
            {
                return NotFound(new { message = "Không tìm thấy ứng viên." });
            }

            return Ok(candidate);
        }

        // Thêm ứng viên mới.
        [HttpPost]
        public async Task<IActionResult> CreateCandidate([FromBody] CreateCandidateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _candidateService.CreateCandidateAsync(dto);

            return CreatedAtAction(nameof(GetCandidate), new { candidateId = result.Data!.Id }, result.Data);
        }

        // Sửa thông tin ứng viên (không áp dụng cho ứng viên đã bị xóa).
        [HttpPut("{candidateId}")]
        public async Task<IActionResult> UpdateCandidate(int candidateId, [FromBody] UpdateCandidateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _candidateService.UpdateCandidateAsync(candidateId, dto);

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

        // Xóa ứng viên. XÓA MỀM: chỉ đánh dấu IsDeleted = 1,
        [HttpDelete("{candidateId}")]
        public async Task<IActionResult> DeleteCandidate(int candidateId)
        {
            var result = await _candidateService.DeleteCandidateAsync(candidateId);

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

            return Ok(new { message = "Đã xóa ứng viên (xóa mềm)." });
        }
    }
}
