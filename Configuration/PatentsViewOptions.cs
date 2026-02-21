namespace PatentsViewAPI.Configuration
{
    public class PatentsViewOptions
    {
        public const string SectionName = "PatentsView";

        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxResults { get; set; } = 50;
    }
}
