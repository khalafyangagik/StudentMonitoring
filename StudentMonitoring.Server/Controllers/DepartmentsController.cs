using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Models;
using StudentMonitoring.Server.Services;

namespace StudentMonitoring.Server.Controllers;

[ApiController]
[Authorize(Roles = "superadmin")]
[Route("api/[controller]")]
public sealed class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Department>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Department>>> GetAll(CancellationToken cancellationToken)
    {
        var departments = await _departmentService.GetAllAsync(cancellationToken);
        return Ok(departments);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Department), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Department>> Create(
        [FromBody] CreateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var department = await _departmentService.CreateAsync(request, cancellationToken);
            return Created($"/api/departments/{department.Id}", department);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
