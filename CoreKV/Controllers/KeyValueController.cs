using Microsoft.AspNetCore.Mvc;
using CoreKV.Domain.Entities;
using CoreKV.Application.Services;
using CoreKV.Application.DTOs;
using CoreKV.Filters;

[ApiController]
[Route("api/[controller]")]
public class KeyValueController : ControllerBase
{
    private readonly IKeyValueService _keyValueService;

    public KeyValueController(IKeyValueService keyValueService)
    {
        _keyValueService = keyValueService;
    }

    private ApiKey GetCurrentApiKey()
    {
        var apiKey = HttpContext.Items["ApiKey"] as ApiKey;
        if (apiKey == null)
            throw new UnauthorizedAccessException("API key required");
        
        return apiKey;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<KeyValueItem>>> GetAll(string? @namespace = null)
    {
        try
        {
            var apiKey = GetCurrentApiKey();
            var items = await _keyValueService.GetAllAsync(@namespace, apiKey);
            return Ok(items);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    [HttpGet("{namespace}/{key}")]
    public async Task<ActionResult<KeyValueItem>> GetByKey(string @namespace, string key)
    {
        try
        {
            var apiKey = GetCurrentApiKey();
            var item = await _keyValueService.GetByKeyAsync(@namespace, key, apiKey);
            
            if (item == null)
            {
                return NotFound($"Key '{key}' not found in namespace '{@namespace}'");
            }
            
            return Ok(item);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    [HttpPost]
    [RequireNamespace]
    public async Task<ActionResult<KeyValueItem>> Create([FromBody] CreateKeyValueRequest request)
    {
        try
        {
            var apiKey = GetCurrentApiKey();
            var item = await _keyValueService.CreateAsync(request, apiKey);
            
            return CreatedAtAction(nameof(GetByKey), new { @namespace = item.Namespace, key = item.Key }, item);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{namespace}/{key}")]
    [RequireNamespace]
    public async Task<ActionResult<KeyValueItem>> Update(string @namespace, string key, [FromBody] UpdateKeyValueRequest request)
    {
        try
        {
            var apiKey = GetCurrentApiKey();
            var item = await _keyValueService.UpdateAsync(@namespace, key, request, apiKey);
            
            return Ok(item);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpDelete("{namespace}/{key}")]
    [RequireNamespace]
    public async Task<IActionResult> Delete(string @namespace, string key)
    {
        try
        {
            var apiKey = GetCurrentApiKey();
            await _keyValueService.DeleteAsync(@namespace, key, apiKey);
            
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
