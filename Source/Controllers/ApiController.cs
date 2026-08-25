using Microsoft.AspNetCore.Mvc;
using ReviewGenerator.Models;
using ReviewGenerator.Services.Interfaces;

namespace ReviewGenerator.Controllers
{
    [ApiController]
	[Route("[controller]")]
	public class ApiController : ControllerBase
	{
		private readonly IReviewService _reviewService;

		public ApiController(IReviewService reviewService)
		{
			_reviewService = reviewService;
		}

		/// <summary>
		/// Generates a new fake review with a randomized rating(1-5) and constructed description from the ingested dataset.
		/// </summary>
		/// <returns>JSON string</returns>
		[HttpGet]
		[Route("generate")]
		public CustomerReview Generate()
		{
			return _reviewService.Generate();
		}
	}
}