using Microsoft.AspNetCore.Mvc;
using CoreVault.Infrastructure;
using CoreVault.Infrastructure.Application.Services;
using CoreVault.Infrastructure.Application.DTOs;
using CoreVault.Infrastructure.Filters;
using CoreVault.API.Modules.KeyValue.Entities;

namespace CoreVault.API.Modules.KeyValue
{

[ApiController]
[Route("api/kv/[controller]")]
public class KeyValueController : ControllerBase
{
    private readonly IKeyValueService _keyValueService;

    public KeyValueController(IKeyValueService keyValueService)
    {
        _keyValueService = keyValueService;
    }

    [HttpGet("debug")]
    public IActionResult Debug()
    {
        Console.WriteLine("[CONTROLLER] Debug endpoint called");
        Console.Out.Flush();
        return Ok(new { message = "Debug endpoint works", timestamp = DateTime.UtcNow });
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
            Console.WriteLine($"[CONTROLLER] GetByKey called with namespace: '{@namespace}', key: '{key}'");
            Console.Out.Flush();
            var apiKey = GetCurrentApiKey();
            Console.WriteLine($"[CONTROLLER] API Key retrieved: {apiKey?.Key}");
            Console.WriteLine($"[CONTROLLER] API Key Role: {apiKey?.Role}");
            Console.Out.Flush();
            
            var item = await _keyValueService.GetByKeyAsync(@namespace, key, apiKey!);
            Console.WriteLine($"[CONTROLLER] Service returned item: {item != null}");
            Console.Out.Flush();
            
            if (item == null)
            {
                Console.WriteLine($"[CONTROLLER] Returning NotFound");
                Console.Out.Flush();
                return NotFound($"Key '{key}' not found in namespace '{@namespace}'");
            }
            
            Console.WriteLine($"[CONTROLLER] Returning Ok");
            Console.Out.Flush();
            return Ok(item);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CONTROLLER] Exception: {ex.Message}");
            Console.WriteLine($"[CONTROLLER] Stack Trace: {ex.StackTrace}");
            Console.Out.Flush();
            throw;
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
}
