using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using modular_mlm.Infrastructure.Data;
using modular_mlm.Shared;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString(Services.Database);
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        $"Connection string '{Services.Database}' is required for database migrations."
    );

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)
);
builder.AddAzureBlobContainerClient(Services.DataProtectionContainer);

using var host = builder.Build();
await using var scope = host.Services.CreateAsyncScope();

var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
await database.Database.MigrateAsync();

var keyContainer = scope.ServiceProvider.GetRequiredService<BlobContainerClient>();
var keyBlob = keyContainer.GetBlobClient(Services.DataProtectionKeyBlob);
try
{
    await using var initialKeyRing = new MemoryStream(
        "<?xml version=\"1.0\" encoding=\"utf-8\"?><repository />"u8.ToArray()
    );
    await keyBlob.UploadAsync(
        initialKeyRing,
        new BlobUploadOptions
        {
            Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All },
        }
    );
}
catch (RequestFailedException exception) when (exception.Status == 409 || exception.Status == 412)
{
    // Another migration execution already initialized the shared key-ring blob.
}

Console.WriteLine("Database migrations and shared Data Protection storage are ready.");
