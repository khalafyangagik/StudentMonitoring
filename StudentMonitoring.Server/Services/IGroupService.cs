using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Services;

public interface IGroupService
{
    Task<IReadOnlyList<GroupResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<GroupResponse>> GetForLecturerAsync(int lecturerId, CancellationToken cancellationToken);

    Task<GroupResponse> CreateAsync(CreateGroupRequest request, CancellationToken cancellationToken);

    Task<GroupResponse?> UpdateAsync(int groupId, CreateGroupRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int groupId, CancellationToken cancellationToken);
}
