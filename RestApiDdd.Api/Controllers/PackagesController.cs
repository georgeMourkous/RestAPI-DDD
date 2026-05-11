using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestApiDdd.Api.Security;
using RestApiDdd.Api.Versioning;
using RestApiDdd.Service.Abstractions;
using RestApiDdd.Service.Dtos;
using RestApiDdd.Service.Versioning;

namespace RestApiDdd.Api.Controllers;

[ApiController]
[Route("api/{version}/packages/")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[RoleAuthorize("Admin", "PackageManager", "PackageReader")]
[SupportedApiVersions(ApiVersion.v1)]
public sealed class PackagesController(IPackageApplicationService packageService) : ApiControllerBase
{
    [HttpGet]
    [SupportedApiVersions(ApiVersion.v1)]
    [ProducesResponseType(typeof(IReadOnlyList<PackageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PackageDto>>> GetPackages(CancellationToken cancellationToken)
    {
        var packages = await packageService.GetPackagesAsync(cancellationToken);
        return Ok(packages);
    }

    [HttpGet("{id:int}")]
    [SupportedApiVersions(ApiVersion.v1)]
    [ProducesResponseType(typeof(PackageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PackageDto>> GetPackage(int id, CancellationToken cancellationToken)
    {
        var package = await packageService.GetPackageAsync(id, cancellationToken);
        return Ok(package);
    }

    [HttpPost]
    [RoleAuthorize("Admin", "PackageManager")]
    [SupportedApiVersions(ApiVersion.v1)]
    [ProducesResponseType(typeof(PackageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PackageDto>> CreatePackage(
        [FromBody] CreatePackageDto package,
        CancellationToken cancellationToken)
    {
        var created = await packageService.CreatePackageAsync(package, cancellationToken);
        return CreatedAtAction(nameof(GetPackage), new { version = RouteData.Values["version"], id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [RoleAuthorize("Admin", "PackageManager")]
    [SupportedApiVersions(ApiVersion.v1)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdatePackage(
        int id,
        [FromBody] UpdatePackageDto package,
        CancellationToken cancellationToken)
    {
        await packageService.UpdatePackageAsync(id, package, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:int}")]
    [RoleAuthorize("Admin", "PackageManager")]
    [SupportedApiVersions(ApiVersion.v1)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PatchPackage(
        int id,
        [FromBody] PatchPackageDto package,
        CancellationToken cancellationToken)
    {
        await packageService.PatchPackageAsync(id, package, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [RoleAuthorize("Admin")]
    [SupportedApiVersions(ApiVersion.v1)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePackage(int id, CancellationToken cancellationToken)
    {
        await packageService.DeletePackageAsync(id, cancellationToken);
        return NoContent();
    }
}
