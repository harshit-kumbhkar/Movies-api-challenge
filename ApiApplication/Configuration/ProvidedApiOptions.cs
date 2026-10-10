namespace ApiApplication.Configuration
{
    public class ProvidedApiOptions
    {
        public const string SectionName = "ProvidedApi";

        public string HttpBaseUrl { get; set; } = "http://localhost:7172";
        public string GrpcAddress { get; set; } = "https://localhost:7443";
        public string ApiKey { get; set; } = string.Empty;
        public int CacheExpirationMinutes { get; set; } = 60;
    }
}
