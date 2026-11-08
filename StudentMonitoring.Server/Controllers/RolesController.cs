using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Services;

namespace StudentMonitoring.Server.Controllers;

[ApiController]
[Authorize(Roles = "superadmin")]
[Route("api/[controller]")]
public sealed class RolesController : ControllerBase
{
    private readonly IRoleService _service;
    public RolesController(IRoleService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoleResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await _service.GetAllAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<RoleResponse>> Create([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var role = await _service.CreateAsync(request, cancellationToken);
            return Created($"/api/roles/{role.Id}", role);
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoleResponse>> Update(int id, [FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var role = await _service.UpdateAsync(id, request, cancellationToken);
            return role is null ? NotFound() : Ok(role);
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try { return await _service.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound(); }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        { return Conflict(new { message = "Այս դերը կապված է օգտատերերի հետ և չի կարող ջնջվել։" }); }
    }
}
