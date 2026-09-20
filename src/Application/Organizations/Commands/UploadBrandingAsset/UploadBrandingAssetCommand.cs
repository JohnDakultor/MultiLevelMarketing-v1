using modular_mlm.Application.Common.Models;
using modular_mlm.Application.Organizations.Models;

namespace modular_mlm.Application.Organizations.Commands.UploadBrandingAsset;

public sealed record UploadBrandingAssetCommand(
    Guid OrganizationId,
    BrandingAssetKind AssetKind,
    string FileName,
    string ContentType,
    long ContentLength,
    Stream Content
) : IRequest<StoredObject>, IOrganizationAdminRequest;
