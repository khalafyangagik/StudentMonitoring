using Npgsql;
using NpgsqlTypes;
using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Repositories;

public sealed class RoleRepository : IRoleRepository
{
    private readonly NpgsqlDataSource _dataSource;
    public RoleRepository(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<IReadOnlyList<RoleResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT r.id, r.name, r.description, COUNT(DISTINCT u.id),
                   COUNT(DISTINCT rp.permission_id), r.created_at
            FROM role AS r
            LEFT JOIN "user" AS u ON u.role_id = r.id
            LEFT JOIN role_permission AS rp ON rp.role_id = r.id
            GROUP BY r.id
            ORDER BY lower(r.name), r.id;
            """;
        var rows = new List<RoleResponse>();
        await using var command = _dataSource.CreateCommand(sql);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) rows.Add(Map(reader));
        return rows;
    }

    public async Task<bool> NameExistsAsync(string name, int? excludingId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (SELECT 1 FROM role WHERE lower(name)=lower(@name)
                AND (@excludingId IS NULL OR id<>@excludingId));
            """;
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("name", name);
        command.Parameters.Add("excludingId", NpgsqlDbType.Integer).Value = (object?)excludingId ?? DBNull.Value;
        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    public async Task<RoleResponse> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO role (name, description)
            VALUES (@name, @description)
            RETURNING id, name, description, 0::bigint, 0::bigint, created_at;
            """;
        await using var command = _dataSource.CreateCommand(sql);
        AddParameters(command, request);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return Map(reader);
    }

    public async Task<RoleResponse?> UpdateAsync(int id, CreateRoleRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE role SET name=@name, description=@description, updated_at=CURRENT_TIMESTAMP
            WHERE id=@id
            RETURNING id, name, description,
                (SELECT COUNT(*) FROM "user" WHERE role_id=role.id),
                (SELECT COUNT(*) FROM role_permission WHERE role_id=role.id), created_at;
            """;
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", id);
        AddParameters(command, request);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand("DELETE FROM role WHERE id=@id;");
        command.Parameters.AddWithValue("id", id);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void AddParameters(NpgsqlCommand command, CreateRoleRequest request)
    {
        command.Parameters.Add("name", NpgsqlDbType.Varchar, 100).Value = request.Name;
        command.Parameters.Add("description", NpgsqlDbType.Text).Value = (object?)request.Description ?? DBNull.Value;
    }

    private static RoleResponse Map(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetInt32(0), Name = reader.GetString(1), Description = reader.IsDBNull(2) ? null : reader.GetString(2),
        UserCount = Convert.ToInt32(reader.GetValue(3)), PermissionCount = Convert.ToInt32(reader.GetValue(4)),
        CreatedAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5)
    };
}
