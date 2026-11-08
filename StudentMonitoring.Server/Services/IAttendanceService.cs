using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Services;

public interface IAttendanceService
{
    Task<IReadOnlyList<AttendanceResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AttendanceResponse>> GetForLecturerAsync(int lecturerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<SessionOptionResponse>> GetSessionsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SessionOptionResponse>> GetSessionsForLecturerAsync(int lecturerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<StudentResponse>> GetStudentsForLecturerAsync(int lecturerId, CancellationToken cancellationToken);
    Task<AttendanceResponse> CreateAsync(CreateAttendanceRequest request, CancellationToken cancellationToken);
    Task<AttendanceResponse> CreateForLecturerAsync(int lecturerId, CreateAttendanceRequest request, CancellationToken cancellationToken);
    Task<AttendanceResponse?> UpdateAsync(int id, CreateAttendanceRequest request, CancellationToken cancellationToken);
    Task<AttendanceResponse?> UpdateForLecturerAsync(int lecturerId, int id, CreateAttendanceRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
    Task<bool> DeleteForLecturerAsync(int lecturerId, int id, CancellationToken cancellationToken);
}
