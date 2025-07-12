using xiquim_api.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddServices();
builder.Services.AddCors(options =>
{
    options.AddPolicy("ChiquinhoAI", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("ChiquinhoAI");
app.AddEndpoints();
app.Run();