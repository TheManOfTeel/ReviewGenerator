using Newtonsoft.Json;
using ReviewGenerator.Models;
using ReviewGenerator.Services.Interfaces;
using System.IO.Compression;
using System.Text;

namespace ReviewGenerator.Services
{
	public class ReviewService : IReviewService
	{
		private readonly int keySize = 2;
		private readonly int outputSize = 200;

		/// <summary>
		/// Generates a new fake review with a randomized rating(1-5) and constructed description from the ingested dataset.
		/// </summary>
		/// <returns>CustomerReview object</returns>
		public CustomerReview Generate()
		{
			try
			{
				var random = new Random();
				string generatedSummary = CreateReviewSummary();
				return new CustomerReview
				{
					Rating = random.Next(1, 6),
					Summary = generatedSummary
				};
			}
			catch
			{
				return new CustomerReview
				{
					Rating = 1,
					Summary = "I hated this product."
				};
			}
		}

		/// <summary>
		/// Ingest the data from the datasource and retain the ReviewText property to train. Stores this output to the CustomerReview class when completed as a dictionary.
		/// </summary>
		/// <exception cref="ArgumentException"></exception>
		public void IngestInitData()
		{
			try
			{
				var sb = new StringBuilder();
				using (var ss = new FileStream("./DataSet/reviews_Video_Games_5.json.gz", FileMode.Open))
				using (var ms = new MemoryStream())
				{
					using (var gZipStream = new GZipStream(ss, CompressionMode.Decompress))
					{
						gZipStream.CopyTo(ms);
					}
					ms.Seek(0, SeekOrigin.Begin);
					using (var sr = new StreamReader(ms))
					{
						if (sr != null)
						{
							while (!sr.EndOfStream)
							{
								string? line = sr.ReadLine();
								if (line == null)
								{
									break;
								}
								else
								{
									AmazonReviewItem? reviewItem = JsonConvert.DeserializeObject<AmazonReviewItem>(line);
									if (reviewItem?.ReviewText != null)
									{
										sb.Append(reviewItem.ReviewText);
									}
								}
							}
						}
					}
				}

				var words = sb.ToString().Split();
				if (words.Length < outputSize)
				{
					throw new ArgumentException("Output size is out of range");
				}

				var dictionary = new Dictionary<string, List<string>>();
				for (int i = 0; i < words.Length - keySize; i++)
				{
					var key = words.Skip(i).Take(keySize).Aggregate(Join);
					string value;
					if (i + keySize < words.Length)
					{
						value = words[i + keySize];
					}
					else
					{
						value = "";
					}

					if (dictionary.ContainsKey(key))
					{
						dictionary[key].Add(value);
					}
					else
					{
						dictionary.Add(key, new List<string>() { value });
					}
				}

				CustomerReview.DataDictionary = dictionary;
			}
			catch
			{
				Console.WriteLine("Startup data ingestion has failed please check if the file exists.");
				return;
			}
		}

		/// <summary>
		/// Create a new review summary utilizing the data dictionary.
		/// </summary>
		/// <returns></returns>
		private string CreateReviewSummary()
		{
			var dataDictionary = CustomerReview.DataDictionary;

			var random = new Random();
			var output = new List<string>();
			int n = 0;
			int randomNum = random.Next(dataDictionary.Count);
			string prefix = dataDictionary.Keys.Skip(randomNum).Take(1).Single();
			output.AddRange(prefix.Split());

			while (true)
			{
				var suffix = dataDictionary[prefix];
				if (suffix.Count == 1)
				{
					if (suffix[0] == "")
					{
						return output.Aggregate(Join);
					}
					output.Add(suffix[0]);
				}
				else
				{
					randomNum = random.Next(suffix.Count);
					output.Add(suffix[randomNum]);
				}
				if (output.Count >= outputSize)
				{
					return output.Take(outputSize).Aggregate(Join);
				}
				n++;
				prefix = output.Skip(n).Take(keySize).Aggregate(Join);
			}
		}

		/// <summary>
		/// Helper method to join two strings
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns>String of combined input strings</returns>
		static string Join(string a, string b)
		{
			return a + " " + b;
		}
	}
}
