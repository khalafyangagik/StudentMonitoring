using StudentMonitoring.Server.Models;

namespace StudentMonitoring.Server.Repositories;

public interface IDepartmentRepository
{
    Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken cancellationToken);

    Task<Department> CreateAsync(Department department, CancellationToken cancellationToken);
}
