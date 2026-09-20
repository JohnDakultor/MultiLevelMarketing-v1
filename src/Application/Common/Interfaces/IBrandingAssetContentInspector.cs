using modular_mlm.Application.Organizations.Models;

namespace modular_mlm.Application.Common.Interfaces;

public interface IBrandingAssetContentInspector
{
    BrandingAssetInspection Inspect(
        ReadOnlyMemory<byte> content,
        string fileName,
        string declaredContentType,
        BrandingAssetKind assetKind
    );
}
