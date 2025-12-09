# CoreVault API

## Description
A secure file storage and key-value API with namespace-based access control and API key authentication.

## Features
- **Namespace-based data isolation**: Separate data by namespaces (public, user-specific, admin)
- **Role-based access control**: ReadOnly, ReadWrite, and Admin roles
- **API Key authentication**: Secure API access with database-stored keys
- **Public read access**: Allow public GET requests to 'public' namespace
- **File storage**: Upload, download, and manage files with namespace isolation
- **Entity Framework Core**: SQLite database with migrations
- **RESTful API**: Full CRUD operations for both key-value pairs and files

## Architecture
- **Monorepo structure**: CoreVault.API (main application) + CoreVault.Shared (common library)
- **Middleware**: API key validation and namespace access control
- **Controllers**: RESTful endpoints with authorization
- **Models**: KeyValueItem, ApiKey, and FileStorage entities
- **Database**: SQLite with Entity Framework Core
- **Docker support**: Multi-stage Dockerfile for containerized deployment

## Quick Start

### Prerequisites
- .NET 9.0 SDK
- SQLite
- Docker (optional, for containerized deployment)

### Setup
1. Clone the repository
2. Copy `appsettings.Example.json` to `appsettings.json`
3. Configure your database connection string
4. Run migrations:
   ```bash
   dotnet ef database update --project src/CoreVault.API
   ```
5. Start the API:
   ```bash
   dotnet run --project src/CoreVault.API
   ```

### Docker Setup
1. Build the image:
   ```bash
   docker build -t corevault-api .
   ```
2. Run the container:
   ```bash
   docker run -p 8080:8080 corevault-api
   ```

## API Usage

### Key-Value Storage

#### Public Access (No API Key)
```bash
# Get all public data
curl http://localhost:8080/api/kv/keyvalue

# Get specific public item
curl http://localhost:8080/api/kv/keyvalue/public/welcome
```

#### Authenticated Access (With API Key)
```bash
# Get data from specific namespace
curl -H "X-Api-Key: your-api-key" http://localhost:8080/api/kv/keyvalue?namespace=user123

# Create new item
curl -X POST -H "X-Api-Key: your-api-key" -H "Content-Type: application/json" \
     -d '{"namespace":"user123","key":"test","value":"data"}' \
     http://localhost:8080/api/kv/keyvalue

# Update item
curl -X PUT -H "X-Api-Key: your-api-key" -H "Content-Type: application/json" \
     -d 'new value' \
     http://localhost:8080/api/kv/keyvalue/user123/test

# Delete item
curl -X DELETE -H "X-Api-Key: your-api-key" \
     http://localhost:8080/api/kv/keyvalue/user123/test
```

### File Storage

#### Upload File
```bash
curl -X POST -H "X-Api-Key: your-api-key" \
     -F "file=@/path/to/your/file.jpg" \
     -F "namespace=user123" \
     -F "description=My profile picture" \
     http://localhost:8080/api/storage/file/upload
```

#### List Files
```bash
# Get all files in namespace
curl -H "X-Api-Key: your-api-key" \
     http://localhost:8080/api/storage/file?namespace=user123
```

#### Download File
```bash
# Download by ID
curl -H "X-Api-Key: your-api-key" \
     http://localhost:8080/api/storage/file/{file-id}/download

# View file metadata
curl -H "X-Api-Key: your-api-key" \
     http://localhost:8080/api/storage/file/{file-id}
```

#### Delete File
```bash
curl -X DELETE -H "X-Api-Key: your-api-key" \
     http://localhost:8080/api/storage/file/{file-id}
```

### API Key Management
API keys are stored in the database with the following structure:
- **Key**: Unique API key string
- **Role**: ReadOnly (0), ReadWrite (1), or Admin (2)
- **AllowedNamespaces**: Comma-separated list of accessible namespaces

#### Example API Keys
- **Admin**: Full access to all namespaces
- **User**: Access to specific namespaces only
- **Public**: Read-only access to public namespace

## Security Features
- API key validation for write operations
- Namespace-based data isolation
- Role-based permissions
- Public read access for non-sensitive data
- Configuration files excluded from version control
- File hash verification (SHA-256) for integrity

## Database Schema
- **KeyValueItems**: Stores key-value data with namespace support
- **ApiKeys**: Stores API keys with permissions
- **FileStorage**: Stores file metadata with namespace isolation

## Development
- Run migrations: `dotnet ef database update --project src/CoreVault.API`
- Create new migration: `dotnet ef migrations add MigrationName --project src/CoreVault.API`
- Run tests: `dotnet test tests/CoreVault.Tests`
- Generate API key: Use admin endpoint or database seeding

## Testing
The project includes comprehensive test coverage:
- Unit tests for business logic
- Integration tests for API endpoints
- Test coverage reporting with codecov

## Deployment
- Configure environment variables for production
- Use secure key management (Azure Key Vault, etc.)
- Set up proper logging and monitoring
- Docker containerization support
- Port 8080 for HTTP traffic

## Configuration
The application uses the following configuration structure:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=corevault.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "FileStorage": {
    "UploadPath": "uploads"
  }
}
```

## License
MIT License
