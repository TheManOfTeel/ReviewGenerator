using NUnit.Framework;
using NUnit.Framework.Legacy;
using ReviewGenerator.Models;
using ReviewGenerator.Services;
using ReviewGenerator.Services.Interfaces;

namespace ReviewGenerator.UnitTests
{
	[TestFixture]
	public class ReviewServiceTests
	{
		private IReviewService _sut;

		[SetUp]
		public void SetUp()
		{
			CustomerReview.DataDictionary = new Dictionary<string, List<string>>
			{
				{ "very", new List<string>{ "very", "good" } },
				{ "good", new List<string>{ "good", "very" } }
			};
			_sut = new ReviewService();
		}

		[Test]
		[Order(0)]
		public void Generate_SuccessTest()
		{
			CustomerReview customerReview = _sut.Generate();

			ClassicAssert.IsNotNull(customerReview);
			ClassicAssert.IsTrue(customerReview.Rating >= 1 && customerReview.Rating <= 5);
			ClassicAssert.IsTrue(customerReview.Summary.Contains("very"));
			ClassicAssert.IsTrue(customerReview.Summary.Contains("good"));
		}

		[Test]
		[Order(1)]
		public void Generate_FailureTest()
		{
			CustomerReview.DataDictionary = new Dictionary<string, List<string>>
			{
				{ "lol", new List<string>{ "fail" } },
			};
			CustomerReview customerReview = _sut.Generate();

			ClassicAssert.AreEqual(customerReview.Rating, 1);
			ClassicAssert.AreEqual(customerReview.Summary, "I hated this product.");
		}

		[TearDown]
		public void TearDown()
		{
			CustomerReview.DataDictionary = null;
		}
	}
}
