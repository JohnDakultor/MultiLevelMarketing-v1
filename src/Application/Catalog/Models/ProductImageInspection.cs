namespace modular_mlm.Application.Catalog.Models;

public sealed record ProductImageInspection(
    string ContentType,
    string Extension,
    int Width,
    int Height
);

public static class ProductImageLimits
{
    public const long MaximumContentLength = 5 * 1024 * 1024;
}
