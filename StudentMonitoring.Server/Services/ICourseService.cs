using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Services;

public interface ICourseService
{
    Task<IReadOnlyList<CourseResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<CourseResponse> CreateAsync(CreateCourseRequest request, CancellationToken cancellationToken);
}
