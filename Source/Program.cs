using ReviewGenerator;
using ReviewGenerator.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);
var startup = new Startup(builder.Configuration);

startup.ConfigureServices(builder.Services);
var app = builder.Build();
var serviceProvider = app.Services;
var reviewService = serviceProvider.GetService<IReviewService>();
if (reviewService == null)
{
	throw new InvalidOperationException("IReviewService is not registered in the service provider.");
}
startup.Configure(app, builder.Environment, reviewService);
