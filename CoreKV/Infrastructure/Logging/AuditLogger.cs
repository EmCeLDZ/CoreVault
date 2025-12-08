using CoreKV.Domain.Entities;

namespace CoreKV.Infrastructure.Logging
{
    public interface IAuditLogger
    {
        void LogApiKeyCreated(ApiKey apiKey, string createdBy);
        void LogKeyValueAccess(string @namespace, string key, string operation, bool success, string? apiKey = null);
        void LogAuthenticationAttempt(string apiKey, bool success, string reason);
        void LogDatabaseOperation(string operation, string table, bool success, long duration);
        Task LogErrorAsync(Exception exception, string correlationId, Dictionary<string, object> context);
    }

    public class AuditLogger : IAuditLogger
    {
        private readonly ILogger<AuditLogger> _logger;

        public AuditLogger(ILogger<AuditLogger> logger)
        {
            _logger = logger;
        }

        public void LogApiKeyCreated(ApiKey apiKey, string createdBy)
        {
            _logger.LogInformation(
                "API Key created - Key: {Key} - Role: {Role} - Namespaces: {Namespaces} - CreatedBy: {CreatedBy}",
                apiKey.Key,
                apiKey.Role,
                apiKey.AllowedNamespaces,
                createdBy);
        }

        public void LogKeyValueAccess(string @namespace, string key, string operation, bool success, string? apiKey = null)
        {
            var logLevel = success ? LogLevel.Information : LogLevel.Warning;
            var maskedKey = apiKey?.Length > 8 ? apiKey[..8] + "..." : apiKey;

            _logger.Log(logLevel,
                "Key-Value operation - Namespace: {Namespace} - Key: {Key} - Operation: {Operation} - Success: {Success} - ApiKey: {ApiKey}",
                @namespace,
                key,
                operation,
                success,
                maskedKey);
        }

        public void LogAuthenticationAttempt(string apiKey, bool success, string reason)
        {
            var maskedKey = apiKey?.Length > 8 ? apiKey[..8] + "..." : apiKey;
            var logLevel = success ? LogLevel.Information : LogLevel.Warning;

            _logger.Log(logLevel,
                "Authentication attempt - ApiKey: {ApiKey} - Success: {Success} - Reason: {Reason}",
                maskedKey,
                success,
                reason);
        }

        public void LogDatabaseOperation(string operation, string table, bool success, long duration)
        {
            var logLevel = !success || duration > 1000 ? LogLevel.Warning : LogLevel.Debug;

            _logger.Log(logLevel,
                "Database operation - Operation: {Operation} - Table: {Table} - Success: {Success} - Duration: {Duration}ms",
                operation,
                table,
                success,
                duration);
        }

        public async Task LogErrorAsync(Exception exception, string correlationId, Dictionary<string, object> context)
        {
            _logger.LogError(exception, 
                "Error logged - CorrelationId: {CorrelationId} - Context: {@Context}",
                correlationId, context);
            
            await Task.CompletedTask;
        }
    }
}
