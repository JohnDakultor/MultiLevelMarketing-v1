using modular_mlm.Application.Common.Models;

namespace modular_mlm.Application.Catalog.Commands.UploadProductImage;

public sealed record UploadProductImageCommand(
    Guid OrganizationId,
    Guid ProductId,
    string FileName,
    string ContentType,
    long ContentLength,
    Stream Content
) : IRequest<StoredObject>, IOrganizationAdminRequest;
