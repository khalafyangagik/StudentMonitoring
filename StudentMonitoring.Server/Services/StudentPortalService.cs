using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Repositories;

namespace StudentMonitoring.Server.Services;

public sealed class StudentPortalService(IStudentPortalRepository repository)
{
    public Task<StudentPortalSummary?> GetSummaryAsync(int studentId, CancellationToken cancellationToken) =>
        repository.GetSummaryAsync(studentId, cancellationToken);
}
