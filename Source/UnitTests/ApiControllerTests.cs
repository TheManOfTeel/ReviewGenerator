using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using ReviewGenerator.Controllers;
using ReviewGenerator.Models;
using ReviewGenerator.Services.Interfaces;

namespace ReviewGenerator.UnitTests
{
	[TestFixture]
	public class ApiControllerTests
	{
		[Test]
		public void Generate_ReturnsReviewFromService()
		{
			var expected = new CustomerReview { Rating = 4, Summary = "A thoughtful review." };
			var controller = new ApiController(new StubReviewService(expected));

			var result = controller.Generate();

			Assert.That(result, Is.SameAs(expected));
		}

		[Test]
		public void Generate_PropagatesServiceFailure()
		{
			var controller = new ApiController(new StubReviewService
				(new InvalidOperationException("The review dataset has not been loaded.")));

			Assert.That(() => controller.Generate(), Throws.InvalidOperationException
				.With.Message.EqualTo("The review dataset has not been loaded."));
		}

		private sealed class StubReviewService : IReviewService
		{
			private readonly CustomerReview? review;
			private readonly Exception? exception;

			public StubReviewService(CustomerReview review)
		{
				this.review = review;
			}

			public StubReviewService(Exception exception)
			{
				this.exception = exception;
			}

			public CustomerReview Generate()
			{
				if (exception != null)
				{
					throw exception;
				}

				return review!;
			}

			public void IngestInitData()
			{
			}
		}
	}
}
