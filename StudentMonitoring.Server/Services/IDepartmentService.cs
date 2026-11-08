using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Models;

namespace StudentMonitoring.Server.Services;

public interface IDepartmentService
{
    Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken cancellationToken);

    Task<Department> CreateAsync(CreateDepartmentRequest request, CancellationToken cancellationToken);
}
