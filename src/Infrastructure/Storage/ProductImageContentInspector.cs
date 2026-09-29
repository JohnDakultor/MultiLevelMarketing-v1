using modular_mlm.Application.Catalog.Models;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Organizations.Models;

namespace modular_mlm.Infrastructure.Storage;

public sealed class ProductImageContentInspector(IBrandingAssetContentInspector imageInspector)
    : IProductImageContentInspector
{
    public ProductImageInspection Inspect(
        ReadOnlyMemory<byte> content,
        string fileName,
        string declaredContentType
    )
    {
        if (content.IsEmpty || content.Length > ProductImageLimits.MaximumContentLength)
            throw new InvalidDataException("Product image size is invalid.");

        var inspected = imageInspector.Inspect(
            content,
            fileName,
            declaredContentType,
            BrandingAssetKind.Logo
        );
        if (inspected.ContentType is not ("image/png" or "image/jpeg"))
            throw new InvalidDataException("Product images must use PNG or JPEG format.");

        return new ProductImageInspection(
            inspected.ContentType,
            inspected.Extension,
            inspected.Width,
            inspected.Height
        );
    }
}
