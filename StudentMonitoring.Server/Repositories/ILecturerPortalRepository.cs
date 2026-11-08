using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Repositories;

public interface ILecturerPortalRepository
{
    Task<LecturerPortalSummary?> GetSummaryAsync(int lecturerId, CancellationToken cancellationToken);
}
