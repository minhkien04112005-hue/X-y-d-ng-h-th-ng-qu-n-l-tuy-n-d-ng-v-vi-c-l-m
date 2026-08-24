namespace RecruitmentAPI.Services
{
    public enum ServiceErrorType
    {
        None,
        NotFound,
        BadRequest,
        Conflict
    }


    public class ServiceResult<T>
    {
        public required bool Success { get; init; }
        public T? Data { get; init; }
        public string? Message { get; init; }
        public string? ErrorCode { get; init; }
        public ServiceErrorType ErrorType { get; init; }
    }
}
