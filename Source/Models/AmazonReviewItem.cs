namespace Review.Models
{
	public class AmazonReviewItem
	{
		public string? ReviewerID { get; set; }
		public string? Asin { get; set; }
		public string? ReviewerName { get; set; }
		public int[]? Helpful { get; set; }
		public string? ReviewText { get; set; }
		public decimal Overall { get; set; }
		public string? Summary { get; set; }
		public decimal UnixReviewTime { get; set; }
		public string? ReviewTime { get; set; }
	}
}
