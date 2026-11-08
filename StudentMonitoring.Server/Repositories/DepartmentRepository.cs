using Npgsql;
using NpgsqlTypes;
using StudentMonitoring.Server.Models;

namespace StudentMonitoring.Server.Repositories;

public sealed class DepartmentRepository : IDepartmentRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public DepartmentRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, name, description, type::text
            FROM department
            ORDER BY id;
            """;

        var departments = new List<Department>();
        await using var command = _dataSource.CreateCommand(sql);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            departments.Add(MapDepartment(reader));
        }

        return departments;
    }

    public async Task<Department> CreateAsync(Department department, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO department (name, description, type)
            VALUES (@name, @description, CAST(@type AS department_type))
            RETURNING id, name, description, type::text;
            """;

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("name", department.Name);
        command.Parameters.Add("description", NpgsqlDbType.Text).Value =
            (object?)department.Description ?? DBNull.Value;
        command.Parameters.Add("type", NpgsqlDbType.Text).Value =
            (object?)department.Type ?? DBNull.Value;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return MapDepartment(reader);
    }

    private static Department MapDepartment(NpgsqlDataReader reader)
    {
        return new Department
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
            Type = reader.IsDBNull(3) ? null : reader.GetString(3)
        };
    }
}
