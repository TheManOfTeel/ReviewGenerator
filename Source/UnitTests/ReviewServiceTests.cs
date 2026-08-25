using System.IO.Compression;
using System.Text.Json;
using NUnit.Framework;
using ReviewGenerator.Models;
using ReviewGenerator.Services;

namespace ReviewGenerator.UnitTests
{
	[TestFixture]
	public class ReviewServiceTests
	{
		[Test]
		public void Generate_ReturnsReviewWithValidRatingAndSummary()
		{
			var service = CreateService(new Dictionary<string, List<string>>
			{
				{ "very good", new List<string> { "very" } },
				{ "good very", new List<string> { "good" } }
			});

			var review = service.Generate();

			Assert.That(review.Rating, Is.InRange(1, 5));
			Assert.That(review.Summary, Is.Not.Null.And.Not.Empty);
			Assert.That(review.Summary, Does.Contain("very"));
			Assert.That(review.Summary, Does.Contain("good"));
		}

		[Test]
		public void Generate_NormalizesCapitalizationAndPunctuation()
		{
			var service = CreateService(new Dictionary<string, List<string>>
			{
				{ "hello world", new List<string> { "amazing" } },
				{ "world amazing", new List<string> { "game." } }
			});

			var review = service.Generate();

			Assert.That(review.Summary, Is.EqualTo("Hello world amazing game."));
		}

		[Test]
		public void Generate_DoesNotExceedConfiguredWordLimit()
		{
			var transitions = Enumerable.Range(0, 100)
				.ToDictionary(index => $"word{index} word{index + 1}", index => new List<string> { $"word{index + 2}" });
			var service = CreateService(transitions);

			var review = service.Generate();

			Assert.That(review.Summary!.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length, Is.LessThanOrEqualTo(80));
		}

		[Test]
		public void Generate_WithSameSeedProducesSameReview()
		{
			var transitions = new Dictionary<string, List<string>>
			{
				{ "one two", new List<string> { "three", "four" } },
				{ "two three", new List<string> { "four" } },
				{ "two four", new List<string> { "three" } }
			};

			var first = CreateService(transitions, 123).Generate();
			var second = CreateService(transitions, 123).Generate();

			Assert.That(second.Rating, Is.EqualTo(first.Rating));
			Assert.That(second.Summary, Is.EqualTo(first.Summary));
		}

		[Test]
		public void Generate_WithSparseModelReturnsBoundedReview()
		{
			var service = CreateService(new Dictionary<string, List<string>>
			{
				{ "short model", new List<string> { string.Empty } },
				{ "another model", new List<string> { string.Empty } }
			});

			var review = service.Generate();

			Assert.That(review.Summary, Does.Match(@"[.!?]$"));
			Assert.That(review.Summary!.Split(' ').Length, Is.EqualTo(2));
		}

		[Test]
		public void Generate_BeforeIngestionThrowsClearException()
		{
			var service = new ReviewService(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".gz"));

			Assert.That(() => service.Generate(), Throws.InvalidOperationException
				.With.Message.EqualTo("The review dataset has not been loaded."));
		}

		[Test]
		public void IngestInitData_PreservesReviewBoundaries()
		{
			var datasetPath = CreateDataset(
				new AmazonReviewItem { ReviewText = "alpha beta gamma." },
				new AmazonReviewItem { ReviewText = "delta epsilon zeta." });

			try
			{
				var service = new ReviewService(datasetPath, new Random(1));
				service.IngestInitData();

				var review = service.Generate().Summary!;

				var containsFirstReview = review.Contains("Alpha", StringComparison.Ordinal);
				var containsSecondReview = review.Contains("Delta", StringComparison.Ordinal);
				Assert.That(containsFirstReview ^ containsSecondReview, Is.True);
			}
			finally
			{
				File.Delete(datasetPath);
			}
		}

		[Test]
		public void IngestInitData_WhenFileIsMissingThrowsHelpfulException()
		{
			var datasetPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".gz");
			var service = new ReviewService(datasetPath);

			Assert.That(() => service.IngestInitData(), Throws.InvalidOperationException
				.With.Message.Contains(datasetPath));
		}

		[Test]
		public void IngestInitData_WhenJsonIsMalformedThrowsHelpfulException()
		{
			var datasetPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".gz");
			try
			{
				using (var file = File.Create(datasetPath))
				using (var gzip = new GZipStream(file, CompressionMode.Compress))
				using (var writer = new StreamWriter(gzip))
				{
					writer.WriteLine("not valid json");
				}

				var service = new ReviewService(datasetPath);

				Assert.That(() => service.IngestInitData(), Throws.InvalidOperationException
					.With.Message.Contains(datasetPath));
			}
			finally
			{
				File.Delete(datasetPath);
			}
		}

		private static ReviewService CreateService(Dictionary<string, List<string>> transitions, int seed = 42)
		{
			return new ReviewService(transitions, new Random(seed));
		}

		private static string CreateDataset(params AmazonReviewItem[] reviews)
		{
			var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json.gz");
			using var file = File.Create(path);
			using var gzip = new GZipStream(file, CompressionMode.Compress);
			using var writer = new StreamWriter(gzip);
			foreach (var review in reviews)
			{
				writer.WriteLine(JsonSerializer.Serialize(review));
			}
			return path;
		}
	}
}
