using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Repositories;

public interface IStudentPortalRepository
{
    Task<StudentPortalSummary?> GetSummaryAsync(int studentId, CancellationToken cancellationToken);
}
