using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Services;

public interface ISessionService
{
    Task<IReadOnlyList<SessionResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SessionOptionResponse>> GetOptionsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<LecturerOptionResponse>> GetLecturersAsync(CancellationToken cancellationToken);
    Task<SessionResponse> CreateAsync(CreateSessionRequest request, CancellationToken cancellationToken);
    Task<SessionResponse?> UpdateAsync(int id, CreateSessionRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
