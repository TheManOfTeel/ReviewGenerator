using ReviewGenerator.Models;

namespace ReviewGenerator.Services.Interfaces
{
    public interface IReviewService
	{
		public CustomerReview Generate();
		public void IngestInitData();
	}
}
