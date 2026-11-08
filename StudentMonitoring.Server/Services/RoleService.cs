using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Repositories;

namespace StudentMonitoring.Server.Services;

public sealed class RoleService : IRoleService
{
    private readonly IRoleRepository _repository;
    public RoleService(IRoleRepository repository) => _repository = repository;

    public Task<IReadOnlyList<RoleResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        _repository.GetAllAsync(cancellationToken);

    public async Task<RoleResponse> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken) =>
        await _repository.CreateAsync(await ValidateAsync(request, null, cancellationToken), cancellationToken);

    public async Task<RoleResponse?> UpdateAsync(int id, CreateRoleRequest request, CancellationToken cancellationToken)
    {
        return await _repository.UpdateAsync(id, await ValidateAsync(request, id, cancellationToken), cancellationToken);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) => _repository.DeleteAsync(id, cancellationToken);

    private async Task<CreateRoleRequest> ValidateAsync(CreateRoleRequest request, int? id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Դերի անունը պարտադիր է։");
        var name = request.Name.Trim();
        if (await _repository.NameExistsAsync(name, id, cancellationToken))
            throw new ArgumentException("Այս անունով դեր արդեն գոյություն ունի։");
        return new CreateRoleRequest { Name = name, Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim() };
    }
}
