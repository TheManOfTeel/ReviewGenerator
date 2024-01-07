using Review.Models;

namespace Review.Services.Interfaces
{
    public interface IReviewService
	{
		public CustomerReview Generate();
		public void IngestInitData();
	}
}
