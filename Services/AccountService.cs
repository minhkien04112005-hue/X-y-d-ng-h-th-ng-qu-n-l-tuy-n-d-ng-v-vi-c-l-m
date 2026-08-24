using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using RecruitmentAPI.Data;
using RecruitmentAPI.Models;

namespace RecruitmentAPI.Services
{
    public class AccountService
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AccountService(
            SqlConnectionFactory connectionFactory,
            IPasswordHasher<User> passwordHasher)
        {
            _connectionFactory = connectionFactory;
            _passwordHasher = passwordHasher;
        }

        public async Task<ServiceResult<User>> RegisterAsync(
            string fullName,
            string email,
            string password)
        {
            string normalizedEmail = email.Trim().ToLowerInvariant();

            using SqlConnection connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            const string existsSql = @"
                SELECT COUNT(1)
                FROM Users
                WHERE Email = @Email";

            using (SqlCommand existsCommand = new SqlCommand(existsSql, connection))
            {
                existsCommand.Parameters.AddWithValue("@Email", normalizedEmail);
                int count = Convert.ToInt32(await existsCommand.ExecuteScalarAsync());

                if (count > 0)
                {
                    return new ServiceResult<User>
                    {
                        Success = false,
                        Message = "Email này đã được đăng ký.",
                        ErrorCode = ErrorCodes.EmailAlreadyExists,
                        ErrorType = ServiceErrorType.Conflict
                    };
                }
            }

            User user = new User
            {
                FullName = fullName.Trim(),
                Email = normalizedEmail
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            const string insertSql = @"
                INSERT INTO Users (FullName, Email, PasswordHash, CreatedAt)
                OUTPUT INSERTED.Id, INSERTED.FullName, INSERTED.Email,
                       INSERTED.PasswordHash, INSERTED.CreatedAt
                VALUES (@FullName, @Email, @PasswordHash, GETDATE())";

            try
            {
                using SqlCommand insertCommand = new SqlCommand(insertSql, connection);
                insertCommand.Parameters.AddWithValue("@FullName", user.FullName);
                insertCommand.Parameters.AddWithValue("@Email", user.Email);
                insertCommand.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);

                using SqlDataReader reader = await insertCommand.ExecuteReaderAsync();
                await reader.ReadAsync();

                return new ServiceResult<User>
                {
                    Success = true,
                    Data = MapUser(reader)
                };
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627)
            {
                return new ServiceResult<User>
                {
                    Success = false,
                    Message = "Email này đã được đăng ký.",
                    ErrorCode = ErrorCodes.EmailAlreadyExists,
                    ErrorType = ServiceErrorType.Conflict
                };
            }
        }

        public async Task<ServiceResult<User>> LoginAsync(string email, string password)
        {
            string normalizedEmail = email.Trim().ToLowerInvariant();

            const string sql = @"
                SELECT Id, FullName, Email, PasswordHash, CreatedAt
                FROM Users
                WHERE Email = @Email";

            using SqlConnection connection = _connectionFactory.CreateConnection();
            using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Email", normalizedEmail);

            await connection.OpenAsync();
            using SqlDataReader reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return InvalidCredentials();
            }

            User user = MapUser(reader);
            PasswordVerificationResult verifyResult = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                password);

            if (verifyResult == PasswordVerificationResult.Failed)
            {
                return InvalidCredentials();
            }

            if (verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                await reader.CloseAsync();
                user.PasswordHash = _passwordHasher.HashPassword(user, password);

                const string updateHashSql = @"
                    UPDATE Users
                    SET PasswordHash = @PasswordHash
                    WHERE Id = @Id";

                using SqlCommand updateCommand = new SqlCommand(updateHashSql, connection);
                updateCommand.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                updateCommand.Parameters.AddWithValue("@Id", user.Id);
                await updateCommand.ExecuteNonQueryAsync();
            }

            return new ServiceResult<User>
            {
                Success = true,
                Data = user
            };
        }

        private static ServiceResult<User> InvalidCredentials()
        {
            return new ServiceResult<User>
            {
                Success = false,
                Message = "Email hoặc mật khẩu không đúng.",
                ErrorCode = ErrorCodes.InvalidCredentials,
                ErrorType = ServiceErrorType.BadRequest
            };
        }

        private static User MapUser(SqlDataReader reader)
        {
            return new User
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            };
        }
    }
}
