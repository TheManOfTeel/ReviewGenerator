using ReviewGenerator.Services.Interfaces;
using ReviewGenerator.Services;

namespace ReviewGenerator
{
	public class Startup
	{
		public IConfiguration configRoot
		{
			get;
		}

		public Startup(IConfiguration configuration)
		{
			configRoot = configuration;
		}

		public void ConfigureServices(IServiceCollection services)
		{
			services.AddControllers();
			services.AddSingleton<IReviewService, ReviewService>();

			var serviceProvider = services.BuildServiceProvider();
			var reviewService = serviceProvider.GetService<IReviewService>();
			reviewService.IngestInitData();
		}

		public void Configure(WebApplication app, IWebHostEnvironment env)
		{
			// Configure the HTTP request pipeline.
			if (!app.Environment.IsDevelopment())
			{
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.UseStaticFiles();
			app.UseRouting();

			app.MapControllerRoute(
				name: "default",
				pattern: "{controller}/{action=Index}/{id?}");

			app.MapFallbackToFile("index.html"); ;

			app.Run();
		}
	}
}
