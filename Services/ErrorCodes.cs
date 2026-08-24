namespace RecruitmentAPI.Services
{
    public static class ErrorCodes
    {
        public const string JobNotFound = "JOB_NOT_FOUND";
        public const string JobExpired = "JOB_EXPIRED";
        public const string CandidateNotFound = "CANDIDATE_NOT_FOUND";
        public const string AlreadyApplied = "ALREADY_APPLIED";
        public const string ApplicationNotFound = "APPLICATION_NOT_FOUND";
        public const string InvalidStatusValue = "INVALID_STATUS_VALUE";
        public const string InvalidStatusTransition = "INVALID_STATUS_TRANSITION";
        public const string JobAlreadyDeleted = "JOB_ALREADY_DELETED";
        public const string CandidateAlreadyDeleted = "CANDIDATE_ALREADY_DELETED";
        public const string InvalidDeadlineFormat = "INVALID_DEADLINE_FORMAT";
        public const string EmailAlreadyExists = "EMAIL_ALREADY_EXISTS";
        public const string InvalidCredentials = "INVALID_CREDENTIALS";
    }
}
