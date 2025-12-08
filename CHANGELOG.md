# Changelog

All notable changes to CoreVault will be documented in this file.

## [1.0.0] - 2025-12-07

### Added
- Initial release of CoreVault API
- Key-value storage with namespace support
- API key authentication with role-based access control
- File storage module with support for images, videos, audio, documents, and archives
- Public GET access for "public" namespace
- Admin API key generation and management
- File upload, download, and management capabilities
- SHA-256 file hash verification
- SQLite database with Entity Framework Core
- Comprehensive API documentation

### Features
- **Namespace-based data isolation**
- **Role-based access control** (Admin, ReadWrite, ReadOnly)
- **File type validation** and size limits (100MB)
- **Automatic admin key generation** on first startup
- **RESTful API design**
- **Middleware-based authentication**
- **Database migrations support**

### Security
- API key authentication via X-Api-Key header
- Namespace access control
- File type restrictions
- Input validation
- Secure file storage with unique names

### Documentation
- Complete README with setup instructions
- API usage examples
- Contributing guidelines
- MIT License
