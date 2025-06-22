var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Urls.Clear();
app.Urls.Add("http://localhost:8063");

app.Run();
