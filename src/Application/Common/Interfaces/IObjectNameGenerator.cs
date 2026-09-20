using modular_mlm.Application.Organizations.Models;

namespace modular_mlm.Application.Common.Interfaces;

public interface IObjectNameGenerator
{
    string CreateBrandingObjectKey(
        Guid organizationId,
        BrandingAssetKind assetKind,
        string safeExtension
    );
}
