using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Repositories;

public interface IRoleRepository
{
    Task<IReadOnlyList<RoleResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<bool> NameExistsAsync(string name, int? excludingId, CancellationToken cancellationToken);
    Task<RoleResponse> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken);
    Task<RoleResponse?> UpdateAsync(int id, CreateRoleRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
