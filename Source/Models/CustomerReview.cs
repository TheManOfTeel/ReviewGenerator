namespace ReviewGenerator.Models
{
	public class CustomerReview
	{
		public string? Summary { get; set; }
		public int Rating { get; set; }
		public static Dictionary<string, List<string>>? DataDictionary { get; set; }
	}
}
