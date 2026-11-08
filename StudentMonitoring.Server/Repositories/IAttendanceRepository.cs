using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Repositories;

public interface IAttendanceRepository
{
    Task<IReadOnlyList<AttendanceResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AttendanceResponse>> GetForLecturerAsync(int lecturerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<SessionOptionResponse>> GetSessionsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SessionOptionResponse>> GetSessionsForLecturerAsync(int lecturerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<StudentResponse>> GetStudentsForLecturerAsync(int lecturerId, CancellationToken cancellationToken);
    Task<bool> StudentCanAttendSessionAsync(int studentId, int sessionId, CancellationToken cancellationToken);
    Task<bool> StudentCanAttendLecturerSessionAsync(int lecturerId, int studentId, int sessionId, CancellationToken cancellationToken);
    Task<bool> LecturerOwnsAttendanceAsync(int lecturerId, int attendanceId, CancellationToken cancellationToken);
    Task<bool> AttendanceExistsAsync(int studentId, int sessionId, int? excludingId, CancellationToken cancellationToken);
    Task<AttendanceResponse> CreateAsync(CreateAttendanceRequest request, CancellationToken cancellationToken);
    Task<AttendanceResponse?> UpdateAsync(int id, CreateAttendanceRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
