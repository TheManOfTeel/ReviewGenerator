using System.Text.Json;
using System.IO.Compression;
using System;
using System.Text.RegularExpressions;
using ReviewGenerator.Models;
using ReviewGenerator.Services.Interfaces;

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
		/// Generate a new customer review.
		/// </summary>
		/// <returns>CustomerReview object</returns>
		/// <exception cref="InvalidOperationException"></exception>
		public CustomerReview Generate()
		{
			if (dataDictionary == null)
			{
				throw new InvalidOperationException("The review dataset has not been loaded.");
			}

			var review = CreateReviewSummary(dataDictionary);
			return new CustomerReview
			{
				Rating = PredictRating(review),
				Summary = review
			};
		}

		/// <summary>
		/// Ingest the init data data from the datasource and retain the ReviewText property to train.
		/// Stores this output to the CustomerReview class when completed as a dictionary.
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
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
		/// <param name="dataDictionary">Dictionary from init data</param>
		/// <returns>Newly generated review</returns>
		/// <exception cref="ArgumentException"></exception>
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

		/// <summary>
		/// Analyzes the sentiment of a review text and returns a rating from 1 to 5.
		/// </summary>
		/// <param name="reviewText">Raw text of the review from the dataset.</param>
		/// <returns>Int review rating</returns>
		public static int PredictRating(string reviewText)
		{
			if (string.IsNullOrWhiteSpace(reviewText))
			{
				return 3; // Neutral default for empty/null input
			}

			// Standardized lexical lookups with associated polarity weights
			var lexicon = new (string Word, double Weight)[]
			{
				// Highly Positive (+2.0)
				("excellent", 2.0), ("amazing", 2.0), ("outstanding", 2.0), ("fantastic", 2.0),
				("perfect", 2.0), ("superb", 2.0), ("love", 2.0), ("loved", 2.0),
				
				// Positive (+1.0)
				("good", 1.0), ("great", 1.0), ("nice", 1.0), ("enjoyed", 1.0),
				("decent", 1.0), ("helpful", 1.0), ("satisfied", 1.0), ("recommend", 1.0),
				
				// Negative (-1.0)
				("bad", -1.0), ("poor", -1.0), ("slow", -1.0), ("disappointed", -1.0),
				("flawed", -1.0), ("annoying", -1.0), ("subpar", -1.0), ("overpriced", -1.0),
				
				// Highly Negative (-2.0)
				("terrible", -2.0), ("horrible", -2.0), ("awful", -2.0), ("worst", -2.0),
				("waste", -2.0), ("useless", -2.0), ("hate", -2.0), ("hated", -2.0)
			};

			var negations = new[] { "not", "no", "never", "n't", "neither", "barely", "hardly" };
			var intensifiers = new (string Word, double Multiplier)[]
			{
				("very", 1.5), ("extremely", 2.0), ("really", 1.5), ("so", 1.4), ("absolutely", 2.0)
			};

			// Tokenize into lower-case words
			string cleanText = Regex.Replace(reviewText.ToLowerInvariant(), @"[^\w\s']", " ");
			string[] tokens = cleanText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

			double totalScore = 0.0;
			int sentimentWordsCount = 0;

			for (int i = 0; i < tokens.Length; i++)
			{
				string token = tokens[i];

				foreach (var (word, weight) in lexicon)
				{
					if (token == word)
					{
						double currentWeight = weight;

						// Check preceding tokens (window size of 2) for negations or intensifiers
						bool isNegated = false;
						double intensityMultiplier = 1.0;

						for (int j = Math.Max(0, i - 2); j < i; j++)
						{
							string prevToken = tokens[j];

							foreach (string neg in negations)
							{
								if (prevToken == neg)
								{
									isNegated = true;
									break;
								}
							}

							foreach (var (intWord, mult) in intensifiers)
							{
								if (prevToken == intWord)
								{
									intensityMultiplier *= mult;
								}
							}
						}

						if (isNegated)
						{
							currentWeight *= -0.8; // Reverse polarity with slight attenuation
						}

						currentWeight *= intensityMultiplier;
						totalScore += currentWeight;
						sentimentWordsCount++;
						break;
					}
				}
			}

			// Exclamation mark booster edge case (signals heightened emotional intensity)
			int exclamationCount = Regex.Matches(reviewText, @"!").Count;
			if (totalScore > 0) totalScore += exclamationCount * 0.2;
			if (totalScore < 0) totalScore -= exclamationCount * 0.2;

			// Normalize aggregated score using hyperbolic tangent hyperbolic normalization
			// tanh maps (-infinity, +infinity) to (-1.0, +1.0)
			double normalized = Math.Tanh(totalScore / 3.0); 

			// Map normalized sentiment [-1.0, 1.0] onto integer rating scale [1, 5]
			int rating = (int)Math.Round((normalized + 1.0) * 2.0) + 1;

			return Math.Clamp(rating, 1, 5);
		}

		/// <summary>
		/// Add reviews to dictionary
		/// </summary>
		/// <param name="dictionary">Dictionary that we're building to store reviews</param>
		/// <param name="reviewText">Review text from the dataset</param>
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

		/// <summary>
		/// Normalize review to look more natural according to language standards
		/// </summary>
		/// <param name="review">Review that we want to normalize</param>
		/// <returns>Normalized review</returns>
		private static string NormalizeReview(string review)
		{
			var normalized = string.Join(' ', review.Split(' ', StringSplitOptions.RemoveEmptyEntries));
			normalized = normalized.Replace(" .", ".").Replace(" !", "!").Replace(" ?", "?")
				.Replace(" ,", ",").Replace(" ;", ";").Replace(" :", ":");
			return string.IsNullOrEmpty(normalized)
				? normalized
				: char.ToUpperInvariant(normalized[0]) + normalized[1..];
		}
	}
}
