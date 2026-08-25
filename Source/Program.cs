using ReviewGenerator.Services.Interfaces;
using ReviewGenerator.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSingleton<IReviewService, ReviewService>();

var app = builder.Build();
var reviewService = app.Services.GetRequiredService<IReviewService>();
reviewService.IngestInitData();

if (!app.Environment.IsDevelopment())
{
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapControllers();
app.MapFallbackToFile("index.html");
app.Run();
