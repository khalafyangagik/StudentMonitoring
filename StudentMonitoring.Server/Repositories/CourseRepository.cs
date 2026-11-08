using Npgsql;
using NpgsqlTypes;
using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Repositories;

public sealed class CourseRepository : ICourseRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public CourseRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<IReadOnlyList<CourseResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT c.id, c.name, c.department_id, d.name, c.duration_years
            FROM course AS c
            LEFT JOIN department AS d ON d.id = c.department_id
            ORDER BY c.id;
            """;

        var courses = new List<CourseResponse>();
        await using var command = _dataSource.CreateCommand(sql);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            courses.Add(MapCourse(reader));
        }

        return courses;
    }

    public async Task<bool> DepartmentExistsAsync(int departmentId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM department WHERE id = @departmentId);";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("departmentId", departmentId);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    public async Task<CourseResponse> CreateAsync(CreateCourseRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            WITH inserted AS (
                INSERT INTO course (department_id, name, duration_years)
                VALUES (@departmentId, @name, @durationYears)
                RETURNING id, department_id, name, duration_years
            )
            SELECT i.id, i.name, i.department_id, d.name, i.duration_years
            FROM inserted AS i
            LEFT JOIN department AS d ON d.id = i.department_id;
            """;

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.Add("departmentId", NpgsqlDbType.Integer).Value =
            (object?)request.DepartmentId ?? DBNull.Value;
        command.Parameters.AddWithValue("name", request.Name);
        command.Parameters.Add("durationYears", NpgsqlDbType.Integer).Value =
            (object?)request.DurationYears ?? DBNull.Value;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return MapCourse(reader);
    }

    private static CourseResponse MapCourse(NpgsqlDataReader reader)
    {
        return new CourseResponse
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            DepartmentId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
            DepartmentName = reader.IsDBNull(3) ? null : reader.GetString(3),
            DurationYears = reader.IsDBNull(4) ? null : reader.GetInt32(4)
        };
    }
}
