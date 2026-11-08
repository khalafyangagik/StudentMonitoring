using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Models;
using StudentMonitoring.Server.Repositories;

namespace StudentMonitoring.Server.Services;

public sealed class DepartmentService : IDepartmentService
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.Ordinal)
    {
        "academic",
        "administrative"
    };

    private readonly IDepartmentRepository _repository;

    public DepartmentService(IDepartmentRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken cancellationToken)
    {
        return _repository.GetAllAsync(cancellationToken);
    }

    public Task<Department> CreateAsync(CreateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (name.Length == 0)
        {
            throw new ArgumentException("Department name is required.", nameof(request));
        }

        if (request.Type is not null && !AllowedTypes.Contains(request.Type))
        {
            throw new ArgumentException("Type must be 'academic' or 'administrative'.", nameof(request));
        }

        return _repository.CreateAsync(new Department
        {
            Name = name,
            Description = request.Description?.Trim(),
            Type = request.Type
        }, cancellationToken);
    }
}
