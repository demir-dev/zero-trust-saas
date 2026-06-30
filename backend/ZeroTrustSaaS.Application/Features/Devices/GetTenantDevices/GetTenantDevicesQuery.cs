namespace ZeroTrustSaaS.Application.Features.Devices.GetTenantDevices;

public sealed record GetTenantDevicesQuery(
    Guid TenantId,
    int Page = 1,
    int PageSize = 20);
