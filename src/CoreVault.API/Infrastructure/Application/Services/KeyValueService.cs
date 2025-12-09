using CoreVault.Infrastructure;
using CoreVault.API.Modules.KeyValue.Entities;
using CoreVault.Infrastructure.Domain.Interfaces;
using CoreVault.Infrastructure.Domain.Services;
using CoreVault.Infrastructure.Application.DTOs;

namespace CoreVault.Infrastructure.Application.Services
{
    public class KeyValueService : IKeyValueService
    {
        private readonly IKeyValueRepository _keyValueRepository;
        private readonly IAuthorizationService _authorizationService;

        public KeyValueService(IKeyValueRepository keyValueRepository, IAuthorizationService authorizationService)
        {
            _keyValueRepository = keyValueRepository;
            _authorizationService = authorizationService;
        }

        public async Task<IEnumerable<KeyValueItem>> GetAllAsync(string? @namespace = null, ApiKey? apiKey = null)
        {
            var query = await _keyValueRepository.GetAllAsync(@namespace);
            
            if (apiKey != null && apiKey.Role != ApiKeyRole.Admin && string.IsNullOrEmpty(@namespace))
            {
                var allowedNamespaces = _authorizationService.GetAllowedNamespaces(apiKey);
                query = query.Where(x => allowedNamespaces.Contains(x.Namespace) || 
                                       allowedNamespaces.Contains("*"));
            }

            return query;
        }

        public async Task<KeyValueItem?> GetByKeyAsync(string @namespace, string key, ApiKey apiKey)
        {
            if (!_authorizationService.CanAccessNamespace(apiKey, @namespace))
            {
                throw new UnauthorizedAccessException($"Access denied to namespace '{@namespace}'");
            }

            return await _keyValueRepository.GetByKeyAsync(@namespace, key);
        }

        public async Task<KeyValueItem> CreateAsync(CreateKeyValueRequest request, ApiKey apiKey)
        {
            if (!_authorizationService.CanWrite(apiKey))
            {
                throw new UnauthorizedAccessException("Write access required");
            }

            if (!_authorizationService.CanAccessNamespace(apiKey, request.Namespace))
            {
                throw new UnauthorizedAccessException($"Access denied to namespace '{request.Namespace}'");
            }

            if (await _keyValueRepository.ExistsAsync(request.Namespace, request.Key))
            {
                throw new InvalidOperationException($"Key '{request.Key}' already exists in namespace '{request.Namespace}'");
            }

            var item = new KeyValueItem
            {
                Namespace = request.Namespace,
                Key = request.Key,
                Value = request.Value
            };

            return await _keyValueRepository.CreateAsync(item);
        }

        public async Task<KeyValueItem> UpdateAsync(string @namespace, string key, UpdateKeyValueRequest request, ApiKey apiKey)
        {
            if (!_authorizationService.CanWrite(apiKey))
            {
                throw new UnauthorizedAccessException("Write access required");
            }

            if (!_authorizationService.CanAccessNamespace(apiKey, @namespace))
            {
                throw new UnauthorizedAccessException($"Access denied to namespace '{@namespace}'");
            }

            var item = await _keyValueRepository.GetByKeyAsync(@namespace, key);
            if (item == null)
            {
                throw new KeyNotFoundException($"Key '{key}' not found in namespace '{@namespace}'");
            }

            item.Value = request.Value;
            return await _keyValueRepository.UpdateAsync(item);
        }

        public async Task DeleteAsync(string @namespace, string key, ApiKey apiKey)
        {
            if (!_authorizationService.CanWrite(apiKey))
            {
                throw new UnauthorizedAccessException("Write access required");
            }

            if (!_authorizationService.CanAccessNamespace(apiKey, @namespace))
            {
                throw new UnauthorizedAccessException($"Access denied to namespace '{@namespace}'");
            }

            await _keyValueRepository.DeleteAsync(@namespace, key);
        }
    }
}
