using System.Text.Json;
using ReviewGenerator.Models;
using ReviewGenerator.Services.Interfaces;
using System.IO.Compression;

namespace ReviewGenerator.Services
{
	public class ReviewService : IReviewService
	{
		private const string DatasetFileName = "reviews_Video_Games_5.json.gz";
		private readonly int keySize = 2;
		private readonly int outputSize = 80;
		private readonly int minimumSentenceWords = 12;
		private readonly ILogger<ReviewService>? logger;
		private readonly Random random;
		private Dictionary<string, List<string>>? dataDictionary;
		private static readonly JsonSerializerOptions JsonOptions = new()
		{
			PropertyNameCaseInsensitive = true
		};

		public ReviewService(IWebHostEnvironment environment, ILogger<ReviewService> logger)
		{
			DatasetPath = Path.Combine(environment.ContentRootPath, "DataSet", DatasetFileName);
			this.logger = logger;
			random = Random.Shared;
		}

		public ReviewService(Dictionary<string, List<string>> dataDictionary, Random? random = null)
		{
			DatasetPath = string.Empty;
			this.dataDictionary = dataDictionary;
				this.random = random ?? Random.Shared;
		}

		internal string DatasetPath { get; }
		public ReviewService(string datasetPath, Random? random = null)
		{
			DatasetPath = datasetPath;
			this.random = random ?? Random.Shared;
		}

		/// <summary>
		/// Generates a new fake review with a randomized rating(1-5) and constructed description from the ingested dataset.
		/// </summary>
		/// <returns>CustomerReview object</returns>
		public CustomerReview Generate()
		{
			if (dataDictionary == null)
			{
				throw new InvalidOperationException("The review dataset has not been loaded.");
			}

			return new CustomerReview
			{
				Rating = random.Next(1, 6),
				Summary = CreateReviewSummary(dataDictionary)
			};
		}

		/// <summary>
		/// Ingest the data from the datasource and retain the ReviewText property to train. Stores this output to the CustomerReview class when completed as a dictionary.
		/// </summary>
		/// <exception cref="ArgumentException"></exception>
		public void IngestInitData()
		{
			if (string.IsNullOrEmpty(DatasetPath))
			{
				throw new InvalidOperationException("ReviewService requires a dataset path when used by the application.");
			}

			try
			{
			var dictionary = new Dictionary<string, List<string>>();
			using var stream = File.OpenRead(DatasetPath);
			using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
			using var reader = new StreamReader(gzipStream);
			while (reader.ReadLine() is { } line)
			{
				var reviewItem = JsonSerializer.Deserialize<AmazonReviewItem>(line, JsonOptions);
				if (!string.IsNullOrWhiteSpace(reviewItem?.ReviewText))
				{
					AddReview(dictionary, reviewItem.ReviewText);
				}
			}

			if (dictionary.Count < keySize)
			{
				throw new InvalidOperationException("The review dataset did not contain enough usable reviews.");
			}

			dataDictionary = dictionary;
			logger?.LogInformation("Loaded {KeyCount} review transitions from {DatasetPath}.", dictionary.Count, DatasetPath);
		}
		catch (Exception exception) when (exception is IOException or JsonException or InvalidDataException)
		{
			logger?.LogError(exception, "Unable to load the review dataset from {DatasetPath}.", DatasetPath);
			throw new InvalidOperationException($"Unable to load the review dataset from '{DatasetPath}'.", exception);
		}
		}

		/// <summary>
		/// Create a new review summary utilizing the data dictionary.
		/// </summary>
		/// <returns></returns>
		private string CreateReviewSummary(Dictionary<string, List<string>> dataDictionary)
		{
			if (dataDictionary.Count < keySize)
			{
				throw new ArgumentException("Data dictionary does not contain enough keys for the specified key size.");
			}
			if (outputSize <= 0)
			{
				throw new ArgumentException("Output size must be greater than zero.");
			}
			if (outputSize < keySize)
			{
				throw new ArgumentException("Output size must be greater than or equal to the key size.");
			}

			var sentenceEndings = new[] { '.', '!', '?' };
			var output = new List<string>();
			var randomNum = random.Next(dataDictionary.Count);
			string prefix = dataDictionary.Keys.Skip(randomNum).Take(1).Single();
			output.AddRange(prefix.Split());

			while (output.Count < outputSize)
			{
				if (!dataDictionary.TryGetValue(prefix, out var suffix) || suffix.Count == 0)
				{
					break;
				}

				var nextWord = suffix[random.Next(suffix.Count)];
				if (string.IsNullOrWhiteSpace(nextWord))
				{
					break;
				}

				output.Add(nextWord);
				prefix = string.Join(' ', output.TakeLast(Math.Min(keySize, output.Count)));
				if (output.Count >= minimumSentenceWords && sentenceEndings.Contains(nextWord[^1]))
				{
					break;
				}
			}

			var res = string.Join(' ', output.Take(outputSize));
			res = NormalizeReview(res);
			if (!string.IsNullOrEmpty(res) && !sentenceEndings.Contains(res[^1]))
			{
				res += sentenceEndings[random.Next(sentenceEndings.Length)];
			}
			return res;
		}

		private void AddReview(Dictionary<string, List<string>> dictionary, string reviewText)
		{
			var words = reviewText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			for (var index = 0; index < words.Length - keySize; index++)
			{
				var key = string.Join(' ', words.Skip(index).Take(keySize));
				if (!dictionary.TryGetValue(key, out var suffixes))
				{
					suffixes = new List<string>();
					dictionary[key] = suffixes;
				}
				suffixes.Add(words[index + keySize]);
			}

			if (words.Length >= keySize)
			{
				var finalKey = string.Join(' ', words.TakeLast(keySize));
				if (!dictionary.TryGetValue(finalKey, out var suffixes))
				{
					suffixes = new List<string>();
					dictionary[finalKey] = suffixes;
				}
				suffixes.Add(string.Empty);
			}
		}

		private static string NormalizeReview(string review)
		{
			var normalized = string.Join(' ', review.Split(' ', StringSplitOptions.RemoveEmptyEntries));
			normalized = normalized.Replace(" .", ".").Replace(" !", "!").Replace(" ?", "?")
				.Replace(" ,", ",").Replace(" ;", ";").Replace(" :", ":");
			return string.IsNullOrEmpty(normalized)
				? normalized
				: char.ToUpperInvariant(normalized[0]) + normalized[1..];
		}

		/// <summary>
		/// Helper method to join two strings
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns>String of combined input strings</returns>
	}
}
