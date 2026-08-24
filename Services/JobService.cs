using Microsoft.Data.SqlClient;
using RecruitmentAPI.Data;
using RecruitmentAPI.DTOs;
using RecruitmentAPI.Models;
using System.Globalization;

namespace RecruitmentAPI.Services
{

    public class JobService
    {
        public const string DeadlineFormat = "dd-MM-yyyy";

        private readonly SqlConnectionFactory _connectionFactory;

        public JobService(SqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        private static bool TryParseDeadline(string? value, out DateTime deadline)
        {
            if (!DateTime.TryParseExact(
                value,
                DeadlineFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime parsedDate))
            {
                deadline = default;
                return false;
            }

            // Deadline chỉ nhận ngày -> coi như còn hạn tới hết ngày đó (23:59:59)
            deadline = parsedDate.Date.AddDays(1).AddSeconds(-1);
            return true;
        }

        private static Job MapJob(SqlDataReader reader)
        {
            return new Job
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Title = reader.GetString(reader.GetOrdinal("Title")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Description")),
                Deadline = reader.GetDateTime(reader.GetOrdinal("Deadline")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted"))
            };
        }


        // Xem tin tuyển dụng theo Id (mặc định chỉ lấy job chưa bị xóa)
        public async Task<Job?> GetJobByIdAsync(int jobId, bool includeDeleted = false)
        {
            string sql = @"
                SELECT Id, Title, Description, Deadline, CreatedAt, IsDeleted
                FROM Jobs
                WHERE Id = @Id" + (includeDeleted ? string.Empty : " AND IsDeleted = 0");

            using SqlConnection connection = _connectionFactory.CreateConnection();
            using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", jobId);

            await connection.OpenAsync();
            using SqlDataReader reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return MapJob(reader);
        }

        // Danh sách tin tuyển dụng
        public async Task<List<Job>> GetAllJobsAsync()
        {
            string sql = @"
                SELECT Id, Title, Description, Deadline, CreatedAt, IsDeleted
                FROM Jobs
                ORDER BY Id DESC";

            List<Job> jobs = new();

            using SqlConnection connection = _connectionFactory.CreateConnection();
            using SqlCommand command = new SqlCommand(sql, connection);

            await connection.OpenAsync();
            using SqlDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                jobs.Add(MapJob(reader));
            }

            return jobs;
        }

        // Tạo tin tuyển dụng mới
        public async Task<ServiceResult<Job>> CreateJobAsync(CreateJobDto dto)
        {
            if (!TryParseDeadline(dto.Deadline, out DateTime deadline))
            {
                return new ServiceResult<Job>
                {
                    Success = false,
                    Message = $"Deadline không đúng định dạng '{DeadlineFormat}', ví dụ: 24-08-2026.",
                    ErrorCode = ErrorCodes.InvalidDeadlineFormat,
                    ErrorType = ServiceErrorType.BadRequest
                };
            }

            string sql = @"
                INSERT INTO Jobs (Title, Description, Deadline, CreatedAt, IsDeleted)
                OUTPUT INSERTED.Id, INSERTED.Title, INSERTED.Description,
                       INSERTED.Deadline, INSERTED.CreatedAt, INSERTED.IsDeleted
                VALUES (@Title, @Description, @Deadline, GETDATE(), 0)";

            using SqlConnection connection = _connectionFactory.CreateConnection();
            using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Title", dto.Title);
            command.Parameters.AddWithValue("@Description", (object?)dto.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@Deadline", deadline);

            await connection.OpenAsync();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();

            return new ServiceResult<Job> { Success = true, Data = MapJob(reader) };
        }


        // Sửa tin tuyển dụng
        public async Task<ServiceResult<Job>> UpdateJobAsync(int jobId, UpdateJobDto dto)
        {
            if (!TryParseDeadline(dto.Deadline, out DateTime deadline))
            {
                return new ServiceResult<Job>
                {
                    Success = false,
                    Message = $"Deadline không đúng định dạng '{DeadlineFormat}', ví dụ: 24-08-2026.",
                    ErrorCode = ErrorCodes.InvalidDeadlineFormat,
                    ErrorType = ServiceErrorType.BadRequest
                };
            }

            using SqlConnection connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            string updateSql = @"
                UPDATE Jobs
                SET Title = @Title,
                    Description = @Description,
                    Deadline = @Deadline
                OUTPUT INSERTED.Id, INSERTED.Title, INSERTED.Description,
                       INSERTED.Deadline, INSERTED.CreatedAt, INSERTED.IsDeleted
                WHERE Id = @Id AND IsDeleted = 0";

            using SqlCommand updateCommand = new SqlCommand(updateSql, connection);
            updateCommand.Parameters.AddWithValue("@Title", dto.Title);
            updateCommand.Parameters.AddWithValue("@Description", (object?)dto.Description ?? DBNull.Value);
            updateCommand.Parameters.AddWithValue("@Deadline", deadline);
            updateCommand.Parameters.AddWithValue("@Id", jobId);

            using SqlDataReader reader = await updateCommand.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return new ServiceResult<Job>
                {
                    Success = false,
                    Message = "Không tìm thấy tin tuyển dụng hoặc tin đã bị xóa.",
                    ErrorCode = ErrorCodes.JobNotFound,
                    ErrorType = ServiceErrorType.NotFound
                };
            }

            return new ServiceResult<Job> { Success = true, Data = MapJob(reader) };
        }


        // Xóa tin tuyển dụng
        public async Task<ServiceResult<bool>> DeleteJobAsync(int jobId)
        {
            string sql = @"
                UPDATE Jobs
                SET IsDeleted = 1
                OUTPUT INSERTED.Id
                WHERE Id = @Id AND IsDeleted = 0";

            using SqlConnection connection = _connectionFactory.CreateConnection();
            using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", jobId);

            await connection.OpenAsync();
            using SqlDataReader reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                // Phân biệt "không tồn tại" và "đã bị xóa trước đó" để trả thông báo rõ ràng hơn
                var existing = await GetJobByIdAsync(jobId, includeDeleted: true);

                if (existing == null)
                {
                    return new ServiceResult<bool>
                    {
                        Success = false,
                        Message = "Không tìm thấy tin tuyển dụng.",
                        ErrorCode = ErrorCodes.JobNotFound,
                        ErrorType = ServiceErrorType.NotFound
                    };
                }

                return new ServiceResult<bool>
                {
                    Success = false,
                    Message = "Tin tuyển dụng đã bị xóa trước đó.",
                    ErrorCode = ErrorCodes.JobAlreadyDeleted,
                    ErrorType = ServiceErrorType.Conflict
                };
            }

            return new ServiceResult<bool> { Success = true, Data = true };
        }
    }
}
