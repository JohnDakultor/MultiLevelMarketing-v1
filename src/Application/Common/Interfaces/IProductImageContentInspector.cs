using modular_mlm.Application.Catalog.Models;

namespace modular_mlm.Application.Common.Interfaces;

public interface IProductImageContentInspector
{
    ProductImageInspection Inspect(
        ReadOnlyMemory<byte> content,
        string fileName,
        string declaredContentType
    );
}
