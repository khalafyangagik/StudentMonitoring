using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Repositories;

public interface ICourseRepository
{
    Task<IReadOnlyList<CourseResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<bool> DepartmentExistsAsync(int departmentId, CancellationToken cancellationToken);

    Task<CourseResponse> CreateAsync(CreateCourseRequest request, CancellationToken cancellationToken);
}
