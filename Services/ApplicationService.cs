using Microsoft.Data.SqlClient;
using RecruitmentAPI.Data;
using RecruitmentAPI.Models;

namespace RecruitmentAPI.Services
{
    public class ApplicationService
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly JobService _jobService;
        private readonly CandidateService _candidateService;

        public ApplicationService(
            SqlConnectionFactory connectionFactory,
            JobService jobService,
            CandidateService candidateService)
        {
            _connectionFactory = connectionFactory;
            _jobService = jobService;
            _candidateService = candidateService;
        }


        public Task<Job?> GetJobByIdAsync(int jobId)
        {
            return _jobService.GetJobByIdAsync(jobId);
        }



        // kiểm
        public async Task<bool> HasCandidateAppliedAsync(int jobId, int candidateId)
        {
            string sql = @"
                SELECT COUNT(*)
                FROM Applications
                WHERE JobId = @JobId
                  AND CandidateId = @CandidateId";

            using SqlConnection connection = _connectionFactory.CreateConnection();
            using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@JobId", jobId);
            command.Parameters.AddWithValue("@CandidateId", candidateId);

            await connection.OpenAsync();
            int count = (int)await command.ExecuteScalarAsync();

            return count > 0;
        }

        //    Quy trình: Kiểm tra hạn -> Kiểm tra đã ứng tuyển -> Tạo hồ sơ
        public async Task<ServiceResult<Application>> CreateApplicationAsync(int jobId, int candidateId)
        {
            // 1. Kiểm tra Job có tồn tại, chưa bị xóa và còn hạn
            Job? job = await _jobService.GetJobByIdAsync(jobId);

            if (job == null)
            {
                return new ServiceResult<Application>
                {
                    Success = false,
                    Message = "Không tìm thấy tin tuyển dụng.",
                    ErrorCode = ErrorCodes.JobNotFound,
                    ErrorType = ServiceErrorType.NotFound
                };
            }

            if (job.Deadline < DateTime.Now)
            {
                return new ServiceResult<Application>
                {
                    Success = false,
                    Message = "Tin tuyển dụng đã hết hạn ứng tuyển.",
                    ErrorCode = ErrorCodes.JobExpired,
                    ErrorType = ServiceErrorType.BadRequest
                };
            }

            // 2. Kiểm tra Candidate có tồn tại và chưa bị xóa
            Candidate? candidate = await _candidateService.GetCandidateByIdAsync(candidateId);
            if (candidate == null)
            {
                return new ServiceResult<Application>
                {
                    Success = false,
                    Message = "Không tìm thấy ứng viên.",
                    ErrorCode = ErrorCodes.CandidateNotFound,
                    ErrorType = ServiceErrorType.NotFound
                };
            }

            using SqlConnection connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            // 3. Kiểm tra ứng viên đã ứng tuyển vào Job này chưa
            string checkSql = @"
                SELECT COUNT(*)
                FROM Applications
                WHERE JobId = @JobId
                  AND CandidateId = @CandidateId";

            using (SqlCommand checkCommand = new SqlCommand(checkSql, connection))
            {
                checkCommand.Parameters.AddWithValue("@JobId", jobId);
                checkCommand.Parameters.AddWithValue("@CandidateId", candidateId);

                int existingCount = (int)await checkCommand.ExecuteScalarAsync();
                if (existingCount > 0)
                {
                    return new ServiceResult<Application>
                    {
                        Success = false,
                        Message = "Ứng viên đã ứng tuyển vào tin tuyển dụng này.",
                        ErrorCode = ErrorCodes.AlreadyApplied,
                        ErrorType = ServiceErrorType.Conflict
                    };
                }
            }

            // 4. Tạo hồ sơ ứng tuyển
            string insertSql = @"
                INSERT INTO Applications (JobId, CandidateId, Status, AppliedAt)
                OUTPUT INSERTED.Id, INSERTED.JobId, INSERTED.CandidateId,
                       INSERTED.Status, INSERTED.AppliedAt, INSERTED.UpdatedAt
                VALUES (@JobId, @CandidateId, @Status, GETDATE())";

            using SqlCommand insertCommand = new SqlCommand(insertSql, connection);
            insertCommand.Parameters.AddWithValue("@JobId", jobId);
            insertCommand.Parameters.AddWithValue("@CandidateId", candidateId);
            insertCommand.Parameters.AddWithValue("@Status", ApplicationStatus.Received);

            using SqlDataReader insertReader = await insertCommand.ExecuteReaderAsync();
            await insertReader.ReadAsync();

            Application application = new Application
            {
                Id = insertReader.GetInt32(insertReader.GetOrdinal("Id")),
                JobId = insertReader.GetInt32(insertReader.GetOrdinal("JobId")),
                CandidateId = insertReader.GetInt32(insertReader.GetOrdinal("CandidateId")),
                Status = insertReader.GetString(insertReader.GetOrdinal("Status")),
                AppliedAt = insertReader.GetDateTime(insertReader.GetOrdinal("AppliedAt")),
                UpdatedAt = insertReader.IsDBNull(insertReader.GetOrdinal("UpdatedAt"))
                    ? null
                    : insertReader.GetDateTime(insertReader.GetOrdinal("UpdatedAt"))
            };

            return new ServiceResult<Application>
            {
                Success = true,
                Data = application
            };
        }

        // 4. Xem hồ sơ ứng tuyển
        public async Task<Application?> GetApplicationByIdAsync(int id)
        {
            string sql = @"
                SELECT Id, JobId, CandidateId, Status, AppliedAt, UpdatedAt
                FROM Applications
                WHERE Id = @Id";

            using SqlConnection connection = _connectionFactory.CreateConnection();
            using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            using SqlDataReader reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new Application
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                JobId = reader.GetInt32(reader.GetOrdinal("JobId")),
                CandidateId = reader.GetInt32(reader.GetOrdinal("CandidateId")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                AppliedAt = reader.GetDateTime(reader.GetOrdinal("AppliedAt")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };
        }

        // 5. Chuyển trạng thái hồ sơ
        //    Received -> Screening -> Interview -> Passed/Failed
        public async Task<ServiceResult<Application>> UpdateApplicationStatusAsync(int applicationId, string newStatus)
        {
            if (!ApplicationStatus.All.Contains(newStatus))
            {
                return new ServiceResult<Application>
                {
                    Success = false,
                    Message = $"Trạng thái '{newStatus}' không hợp lệ.",
                    ErrorCode = ErrorCodes.InvalidStatusValue,
                    ErrorType = ServiceErrorType.BadRequest
                };
            }

            using SqlConnection connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            // 1. Lấy trạng thái hiện tại
            string currentSql = "SELECT Status FROM Applications WHERE Id = @Id";
            string? currentStatus = null;

            using (SqlCommand currentCommand = new SqlCommand(currentSql, connection))
            {
                currentCommand.Parameters.AddWithValue("@Id", applicationId);
                using SqlDataReader reader = await currentCommand.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    currentStatus = reader.GetString(reader.GetOrdinal("Status"));
                }
            }

            if (currentStatus == null)
            {
                return new ServiceResult<Application>
                {
                    Success = false,
                    Message = "Không tìm thấy hồ sơ ứng tuyển.",
                    ErrorCode = ErrorCodes.ApplicationNotFound,
                    ErrorType = ServiceErrorType.NotFound
                };
            }

            // 2. Kiểm tra trạng thái mới có hợp lệ so với trạng thái hiện tại
            if (!ApplicationStatus.IsValidTransition(currentStatus, newStatus))
            {
                return new ServiceResult<Application>
                {
                    Success = false,
                    Message = $"Không thể chuyển trạng thái từ {currentStatus} sang {newStatus}.",
                    ErrorCode = ErrorCodes.InvalidStatusTransition,
                    ErrorType = ServiceErrorType.BadRequest
                };
            }

            // 3. Cập nhật trạng thái
            string updateSql = @"
                UPDATE Applications
                SET Status = @Status,
                    UpdatedAt = GETDATE()
                OUTPUT INSERTED.Id, INSERTED.JobId, INSERTED.CandidateId,
                       INSERTED.Status, INSERTED.AppliedAt, INSERTED.UpdatedAt
                WHERE Id = @Id";

            using SqlCommand updateCommand = new SqlCommand(updateSql, connection);
            updateCommand.Parameters.AddWithValue("@Status", newStatus);
            updateCommand.Parameters.AddWithValue("@Id", applicationId);

            using SqlDataReader updateReader = await updateCommand.ExecuteReaderAsync();
            await updateReader.ReadAsync();

            Application application = new Application
            {
                Id = updateReader.GetInt32(updateReader.GetOrdinal("Id")),
                JobId = updateReader.GetInt32(updateReader.GetOrdinal("JobId")),
                CandidateId = updateReader.GetInt32(updateReader.GetOrdinal("CandidateId")),
                Status = updateReader.GetString(updateReader.GetOrdinal("Status")),
                AppliedAt = updateReader.GetDateTime(updateReader.GetOrdinal("AppliedAt")),
                UpdatedAt = updateReader.IsDBNull(updateReader.GetOrdinal("UpdatedAt"))
                    ? null
                    : updateReader.GetDateTime(updateReader.GetOrdinal("UpdatedAt"))
            };

            return new ServiceResult<Application>
            {
                Success = true,
                Data = application
            };
        }
    }
}
