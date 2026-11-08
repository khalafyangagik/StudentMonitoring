using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Repositories;

public interface ISuperAdminRepository
{
    Task<SuperAdminOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken);
}
