using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Repositories;

namespace StudentMonitoring.Server.Services;

public sealed class CourseService : ICourseService
{
    private readonly ICourseRepository _repository;

    public CourseService(ICourseRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<CourseResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return _repository.GetAllAsync(cancellationToken);
    }

    public async Task<CourseResponse> CreateAsync(CreateCourseRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (name.Length == 0)
        {
            throw new ArgumentException("Course name is required.", nameof(request));
        }

        if (request.DurationYears is <= 0)
        {
            throw new ArgumentException("Duration must be greater than zero.", nameof(request));
        }

        if (request.DepartmentId is int departmentId &&
            !await _repository.DepartmentExistsAsync(departmentId, cancellationToken))
        {
            throw new ArgumentException("The selected department does not exist.", nameof(request));
        }

        var normalizedRequest = new CreateCourseRequest
        {
            Name = name,
            DepartmentId = request.DepartmentId,
            DurationYears = request.DurationYears
        };

        return await _repository.CreateAsync(normalizedRequest, cancellationToken);
    }
}
