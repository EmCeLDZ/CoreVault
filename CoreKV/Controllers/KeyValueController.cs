using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CoreKV.Models;
using CoreKV.Data;
using CoreKV.Filters;

[ApiController]
[Route("api/[controller]")]
public class KeyValueController : ControllerBase
{
    private readonly CoreKVContext _context;

    public KeyValueController(CoreKVContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<KeyValueItem>>> GetAll(string? @namespace = null)
    {
        var query = _context.KeyValueItems.AsQueryable();
        
        // Filter by namespace if specified
        if (!string.IsNullOrEmpty(@namespace))
        {
            query = query.Where(x => x.Namespace == @namespace);
        }
        // For non-admin users, show only their namespaces
        else if (HttpContext.Items["UserRole"] is not ApiKeyRole.Admin)
        {
            var allowedNamespaces = HttpContext.Items["AllowedNamespaces"] as List<string> ?? new();
            query = query.Where(x => allowedNamespaces.Contains(x.Namespace) || 
                                   allowedNamespaces.Contains("*"));
        }

        return await query.ToListAsync();
    }

    [HttpGet("{namespace}/{key}")]
    public async Task<ActionResult<KeyValueItem>> GetByKey(string @namespace, string key)
    {
        var item = await _context.KeyValueItems
            .FirstOrDefaultAsync(x => x.Namespace == @namespace && x.Key == key);
        
        if (item == null)
        {
            return NotFound($"Key '{key}' not found in namespace '{@namespace}'");
        }
        
        return Ok(item);
    }

    [HttpPost]
    [RequireNamespace]
    public async Task<ActionResult<KeyValueItem>> Create([FromBody] KeyValueItem item)
    {
        // Check write permissions
        if (HttpContext.Items["UserRole"] is not (ApiKeyRole.Admin or ApiKeyRole.ReadWrite))
        {
            return Forbid("Write access required");
        }

        // Check if key already exists
        if (await _context.KeyValueItems.AnyAsync(x => x.Namespace == item.Namespace && x.Key == item.Key))
        {
            return BadRequest($"Key '{item.Key}' already exists in namespace '{item.Namespace}'");
        }

        item.CreatedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        
        _context.KeyValueItems.Add(item);
        await _context.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetByKey), new { @namespace = item.Namespace, key = item.Key }, item);
    }

    [HttpPut("{namespace}/{key}")]
    [RequireNamespace]
    public async Task<ActionResult<KeyValueItem>> Update(string @namespace, string key, [FromBody] string value)
    {
        // Check write permissions
        if (HttpContext.Items["UserRole"] is not (ApiKeyRole.Admin or ApiKeyRole.ReadWrite))
        {
            return Forbid("Write access required");
        }

        var item = await _context.KeyValueItems
            .FirstOrDefaultAsync(x => x.Namespace == @namespace && x.Key == key);
            
        if (item == null)
        {
            return NotFound($"Key '{key}' not found in namespace '{@namespace}'");
        }

        item.Value = value;
        item.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return Ok(item);
    }

    [HttpDelete("{namespace}/{key}")]
    [RequireNamespace]
    public async Task<IActionResult> Delete(string @namespace, string key)
    {
        // Check write permissions
        if (HttpContext.Items["UserRole"] is not (ApiKeyRole.Admin or ApiKeyRole.ReadWrite))
        {
            return Forbid("Write access required");
        }

        var item = await _context.KeyValueItems
            .FirstOrDefaultAsync(x => x.Namespace == @namespace && x.Key == key);
            
        if (item == null)
        {
            return NotFound($"Key '{key}' not found in namespace '{@namespace}'");
        }

        _context.KeyValueItems.Remove(item);
        await _context.SaveChangesAsync();
        
        return NoContent();
    }
}