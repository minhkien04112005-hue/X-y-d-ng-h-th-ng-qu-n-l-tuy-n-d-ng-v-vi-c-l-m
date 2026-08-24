namespace RecruitmentAPI.Models
{
    public class Application
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public int CandidateId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public static class ApplicationStatus
    {
        public const string Received = "Received";
        public const string Screening = "Screening";
        public const string Interview = "Interview";
        public const string Passed = "Passed";
        public const string Failed = "Failed";

        public static readonly HashSet<string> All = new()
        {
            Received, Screening, Interview, Passed, Failed
        };

        public static readonly Dictionary<string, HashSet<string>> AllowedTransitions = new()
        {
            [Received] = new HashSet<string> { Screening },
            [Screening] = new HashSet<string> { Interview },
            [Interview] = new HashSet<string> { Passed, Failed },
            [Passed] = new HashSet<string>(),
            [Failed] = new HashSet<string>()
        };

        public static bool IsValidTransition(string currentStatus, string newStatus)
        {
            if (!AllowedTransitions.ContainsKey(currentStatus))
                return false;

            return AllowedTransitions[currentStatus].Contains(newStatus);
        }
    }
}
