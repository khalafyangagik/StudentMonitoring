using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Services;

public interface IRoleService
{
    Task<IReadOnlyList<RoleResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<RoleResponse> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken);
    Task<RoleResponse?> UpdateAsync(int id, CreateRoleRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
