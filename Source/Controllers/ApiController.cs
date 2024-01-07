using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Review.Models;
using Review.Services.Interfaces;

namespace Review.Controllers
{
    [ApiController]
	[Route("[controller]")]
	public class ApiController : ControllerBase
	{
		private readonly ILogger<ApiController> _logger;
		private readonly IReviewService _reviewService;

		public ApiController(ILogger<ApiController> logger, IReviewService reviewService)
		{
			_logger = logger;
			_reviewService = reviewService;
		}

		/// <summary>
		/// Generates a new fake review with a randomized rating(1-5) and constructed description from the ingested dataset.
		/// </summary>
		/// <returns>JSON string</returns>
		[HttpGet]
		[Route("generate")]
		public string Generate()
		{
			CustomerReview customerReview = _reviewService.Generate();
			return JsonConvert.SerializeObject(customerReview);
		}
	}
}