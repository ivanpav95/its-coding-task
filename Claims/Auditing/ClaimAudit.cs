namespace Claims.Auditing
{
    public class ClaimAudit
    {
        public int Id { get; set; }

        public required string ClaimId { get; set; }

        public DateTime Created { get; set; }

        public required string HttpRequestType { get; set; }
    }
}
