using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

var builder = WebApplication.CreateBuilder();
var app = builder.Build();

const string FilePath = "airdrops.txt";

// ✅ **Ensure the server binds correctly on Replit**
app.Urls.Add("http://0.0.0.0:8080");

// ✅ **Serve static files correctly**
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Directory.GetCurrentDirectory()),
    RequestPath = ""
});

// ✅ **Ensure index.html is the default file**
app.UseDefaultFiles();
app.UseStaticFiles();

// ✅ **Redirect root requests to `index.html`**
app.MapGet("/", async context =>
{
    context.Response.Redirect("/index.html");
});

// ✅ **API to get airdrops**
app.MapGet("/get-airdrops", async () =>
{
    Console.WriteLine($"Looking for file at path: {FilePath}");
    if (!File.Exists(FilePath))
    {
        Console.WriteLine("File does not exist!");
        return Results.Json(new { airdrops = Array.Empty<string>() });
    }

    var data = await File.ReadAllLinesAsync(FilePath);
    return Results.Json(new { airdrops = data });
});


// ✅ **API to add a new airdrop**
  app.MapPost("/add-airdrop", async (HttpContext context) =>
  {
      try
      {
          using var reader = new StreamReader(context.Request.Body);
          var requestBody = await reader.ReadToEndAsync();
          Console.WriteLine($"📥 Received Raw JSON: {requestBody}");

          // Ensure request body is not empty
          if (string.IsNullOrWhiteSpace(requestBody))
          {
              Console.WriteLine("❌ Empty Request Body Received");
              return Results.BadRequest("Empty request body");
          }

          // Try to deserialize
          try
          {
              var request = JsonSerializer.Deserialize<AirdropEntry>(requestBody, new JsonSerializerOptions
              {
                  PropertyNameCaseInsensitive = true  // ✅ Allows different JSON casing
              });

              if (request == null || string.IsNullOrWhiteSpace(request.Category) || 
                  string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Url))
              {
                  Console.WriteLine("❌ Invalid Data Received");
                  return Results.BadRequest("Invalid data format");
              }

              string newEntry = $"{request.Category.Trim()}, {request.Name.Trim()}, {request.Url.Trim()}\n";
              await File.AppendAllTextAsync(FilePath, newEntry);

              Console.WriteLine("✅ Airdrop Added Successfully!");
              return Results.Json(new { message = "Airdrop added successfully" });
          }
          catch (JsonException ex)
          {
              Console.WriteLine($"❌ JSON Deserialization Error: {ex.Message}");
              return Results.BadRequest("Invalid JSON format");
          }
      }
      catch (Exception ex)
      {
          Console.WriteLine($"❌ Error: {ex.Message}");
          return Results.Problem("An error occurred: " + ex.Message);
      }
  });


// ✅ **API to delete an airdrop**
app.MapDelete("/delete-airdrop", async (HttpContext context) =>
{
    var query = context.Request.Query["index"];
    if (!int.TryParse(query, out int index)) return Results.BadRequest("Invalid index");

    var lines = await File.ReadAllLinesAsync(FilePath);
    if (index < 0 || index >= lines.Length) return Results.NotFound("Airdrop not found");

    var updatedLines = lines.Where((_, i) => i != index).ToArray();
    await File.WriteAllLinesAsync(FilePath, updatedLines);

    return Results.Json(new { message = "Airdrop deleted successfully" });
});

app.Run();

// **Airdrop Entry Model**
record AirdropEntry(string Category, string Name, string Url);
