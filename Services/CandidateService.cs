using Microsoft.Data.SqlClient;
using RecruitmentAPI.Data;
using RecruitmentAPI.DTOs;
using RecruitmentAPI.Models;

namespace RecruitmentAPI.Services
{
    /// <summary>
    /// Nghiệp vụ quản lý ứng viên (Candidate): xem, tạo, sửa, xóa mềm.
    /// Xóa ứng viên KHÔNG xóa cứng khỏi database, chỉ đánh dấu IsDeleted = 1.
    /// Mọi truy vấn dùng SQL thuần (SqlCommand + SqlDataReader).
    /// </summary>
    public class CandidateService
    {
        private readonly SqlConnectionFactory _connectionFactory;

        public CandidateService(SqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        private static Candidate MapCandidate(SqlDataReader reader)
        {
            return new Candidate
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                Phone = reader.IsDBNull(reader.GetOrdinal("Phone"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Phone")),
                IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted"))
            };
        }

        // ---------------------------------------------------------
        // Xem ứng viên theo Id (mặc định chỉ lấy ứng viên chưa bị xóa)
        // GET /api/candidates/{candidateId}
        // ---------------------------------------------------------
        public async Task<Candidate?> GetCandidateByIdAsync(int candidateId, bool includeDeleted = false)
        {
            string sql = @"
                SELECT Id, FullName, Email, Phone, IsDeleted
                FROM Candidates
                WHERE Id = @Id" + (includeDeleted ? string.Empty : " AND IsDeleted = 0");

            using SqlConnection connection = _connectionFactory.CreateConnection();
            using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", candidateId);

            await connection.OpenAsync();
            using SqlDataReader reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return MapCandidate(reader);
        }

        // ---------------------------------------------------------
        // Danh sách ứng viên (lấy tất cả, kể cả đã xóa mềm)
        // GET /api/candidates
        // ---------------------------------------------------------
        public async Task<List<Candidate>> GetAllCandidatesAsync()
        {
            string sql = @"
                SELECT Id, FullName, Email, Phone, IsDeleted
                FROM Candidates
                ORDER BY Id DESC";

            List<Candidate> candidates = new();

            using SqlConnection connection = _connectionFactory.CreateConnection();
            using SqlCommand command = new SqlCommand(sql, connection);

            await connection.OpenAsync();
            using SqlDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                candidates.Add(MapCandidate(reader));
            }

            return candidates;
        }

        // ---------------------------------------------------------
        // Tạo ứng viên mới
        // POST /api/candidates
        // ---------------------------------------------------------
        public async Task<ServiceResult<Candidate>> CreateCandidateAsync(CreateCandidateDto dto)
        {
            string sql = @"
                INSERT INTO Candidates (FullName, Email, Phone, IsDeleted)
                OUTPUT INSERTED.Id, INSERTED.FullName, INSERTED.Email, INSERTED.Phone, INSERTED.IsDeleted
                VALUES (@FullName, @Email, @Phone, 0)";

            using SqlConnection connection = _connectionFactory.CreateConnection();
            using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@FullName", dto.FullName);
            command.Parameters.AddWithValue("@Email", dto.Email);
            command.Parameters.AddWithValue("@Phone", (object?)dto.Phone ?? DBNull.Value);

            await connection.OpenAsync();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();

            return new ServiceResult<Candidate> { Success = true, Data = MapCandidate(reader) };
        }

        // ---------------------------------------------------------
        // Sửa thông tin ứng viên
        // PUT /api/candidates/{candidateId}
        // ---------------------------------------------------------
        public async Task<ServiceResult<Candidate>> UpdateCandidateAsync(int candidateId, UpdateCandidateDto dto)
        {
            string sql = @"
                UPDATE Candidates
                SET FullName = @FullName,
                    Email = @Email,
                    Phone = @Phone
                OUTPUT INSERTED.Id, INSERTED.FullName, INSERTED.Email, INSERTED.Phone, INSERTED.IsDeleted
                WHERE Id = @Id AND IsDeleted = 0";

            using SqlConnection connection = _connectionFactory.CreateConnection();
            using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@FullName", dto.FullName);
            command.Parameters.AddWithValue("@Email", dto.Email);
            command.Parameters.AddWithValue("@Phone", (object?)dto.Phone ?? DBNull.Value);
            command.Parameters.AddWithValue("@Id", candidateId);

            await connection.OpenAsync();
            using SqlDataReader reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return new ServiceResult<Candidate>
                {
                    Success = false,
                    Message = "Không tìm thấy ứng viên hoặc ứng viên đã bị xóa.",
                    ErrorCode = ErrorCodes.CandidateNotFound,
                    ErrorType = ServiceErrorType.NotFound
                };
            }

            return new ServiceResult<Candidate> { Success = true, Data = MapCandidate(reader) };
        }

        // ---------------------------------------------------------
        // Xóa ứng viên (XÓA MỀM: chỉ đánh dấu IsDeleted = 1,
        // không xóa cứng khỏi database)
        // DELETE /api/candidates/{candidateId}
        // ---------------------------------------------------------
        public async Task<ServiceResult<bool>> DeleteCandidateAsync(int candidateId)
        {
            string sql = @"
                UPDATE Candidates
                SET IsDeleted = 1
                OUTPUT INSERTED.Id
                WHERE Id = @Id AND IsDeleted = 0";

            using SqlConnection connection = _connectionFactory.CreateConnection();
            using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", candidateId);

            await connection.OpenAsync();
            using SqlDataReader reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                var existing = await GetCandidateByIdAsync(candidateId, includeDeleted: true);

                if (existing == null)
                {
                    return new ServiceResult<bool>
                    {
                        Success = false,
                        Message = "Không tìm thấy ứng viên.",
                        ErrorCode = ErrorCodes.CandidateNotFound,
                        ErrorType = ServiceErrorType.NotFound
                    };
                }

                return new ServiceResult<bool>
                {
                    Success = false,
                    Message = "Ứng viên đã bị xóa trước đó.",
                    ErrorCode = ErrorCodes.CandidateAlreadyDeleted,
                    ErrorType = ServiceErrorType.Conflict
                };
            }

            return new ServiceResult<bool> { Success = true, Data = true };
        }
    }
}
