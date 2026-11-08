using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Repositories;

namespace StudentMonitoring.Server.Services;

public sealed class SuperAdminService(ISuperAdminRepository repository)
{
    public Task<SuperAdminOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken) =>
        repository.GetOverviewAsync(cancellationToken);
}
