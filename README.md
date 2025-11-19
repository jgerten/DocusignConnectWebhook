# DocuSign Webhook API

## Features

- **DocuSign Connect Integration**: Receives and validates webhook events with HMAC signature verification
- **Automatic Document Download**: Downloads completed envelope documents from DocuSign API
- **MinIO Storage**: Stores documents in MinIO (S3-compatible) object storage
- **PostgreSQL Database**: Tracks envelopes, documents, and webhook events
- **Clean Architecture**: Follows Domain-Driven Design with clear separation of concerns
- **Background Processing**: Asynchronous webhook processing with retry logic
- **Comprehensive Logging**: Serilog integration with console and file outputs
- **Swagger/OpenAPI**: Full API documentation and testing interface

## Architecture

The solution follows Clean Architecture principles with four main layers:

```
DocuSignWebhook/
├── Domain/           # Core business entities (no dependencies)
│   └── Entities/     # Envelope, Document, WebhookEvent
├── Application/      # Business logic and interfaces
│   ├── Interfaces/   # Service contracts
│   └── Services/     # Business logic implementation
├── Infrastructure/   # External concerns (DB, APIs)
│   ├── Data/         # EF Core DbContext
│   └── Services/     # DocuSign & MinIO implementations
└── API/              # HTTP endpoints and configuration
    └── Controllers/  # REST API controllers
```

## Prerequisites

- .NET 8 SDK
- PostgreSQL 15+ (or Docker)
- MinIO (or Docker)
- DocuSign Developer Account

## Quick Start with Docker

The easiest way to get started is using Docker Compose:

```bash
# Start PostgreSQL and MinIO
docker-compose up -d

# Run database migrations
cd src/DocuSignWebhook.API
dotnet ef database update

# Start the API
dotnet run
```

API will be available at: `https://localhost:5081/swagger`

## Configuration

### 1. Update `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=6432;Database=docusign_webhook;Username=postgres;Password=your_password"
  },
  "DocuSign": {
    "AccountId": "your_docusign_account_id",
    "AccessToken": "your_jwt_access_token",
    "BasePath": "https://demo.docusign.net/restapi",
    "HmacSecret": "your_connect_hmac_secret"
  },
  "MinIO": {
    "Endpoint": "localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin",
    "UseSSL": false,
    "DefaultBucket": "docusign-documents"
  }
}
```

### 2. DocuSign Setup

1. Create a DocuSign Developer Account at https://developers.docusign.com
2. Create an application and note the **Integration Key**
3. Generate a **JWT Access Token** (or use OAuth 2.0)
4. Get your **Account ID** from the Admin panel
5. Configure **DocuSign Connect**:
   - Go to Settings → Connect
   - Add new configuration
   - Set URL to: `https://your-domain.com/api/docusignwebhook`
   - Enable HMAC signature and save the secret
   - Select events to send (e.g., "Envelope Completed")

### 3. Environment Variables (Alternative to appsettings.json)

For production, use environment variables:

```bash
export ConnectionStrings__DefaultConnection="Host=..."
export DocuSign__AccountId="your_account_id"
export DocuSign__AccessToken="your_token"
export DocuSign__HmacSecret="your_secret"
export MinIO__Endpoint="minio:9000"
export MinIO__AccessKey="your_key"
export MinIO__SecretKey="your_secret"
```

## Database Migrations

```bash
# Create a new migration
cd src/DocuSignWebhook.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../DocuSignWebhook.API

# Apply migrations
dotnet ef database update --startup-project ../DocuSignWebhook.API
```

## API Endpoints

### Webhook Endpoint

- **POST** `/api/docusignwebhook` - Receives DocuSign Connect webhooks
  - Validates HMAC signature
  - Stores webhook event
  - Triggers async processing

### Management Endpoints

- **GET** `/api/docusignwebhook/{id}` - Get webhook event status
- **GET** `/api/docusignwebhook/health` - Health check
- **GET** `/api/envelopes` - List all envelopes
- **GET** `/api/envelopes/{id}` - Get envelope details
- **GET** `/api/envelopes/docusign/{docusignEnvelopeId}` - Get envelope by DocuSign ID
- **GET** `/api/envelopes/{id}/documents` - List envelope documents

## Workflow

1. **DocuSign sends webhook** → `POST /api/docusignwebhook`
2. **API validates HMAC** signature
3. **Webhook event saved** to PostgreSQL with status "Pending"
4. **Background processor**:
   - Fetches envelope details from DocuSign API
   - Downloads all documents
   - Computes MD5 hashes
   - Uploads to MinIO
   - Updates database with document metadata
5. **Status updates** to "Completed" or "Failed"

## Development

### Build the Solution

```bash
dotnet build DocuSignWebhook.sln
```

### Run Tests (when added)

```bash
dotnet test
```

### Run Locally

```bash
cd src/DocuSignWebhook.API
dotnet run
```

### Docker Build

```bash
# Build API image
docker build -t docusign-webhook-api -f src/DocuSignWebhook.API/Dockerfile .

# Run container
docker run -p 5081:8080 \
  -e ConnectionStrings__DefaultConnection="Host=postgres;..." \
  -e DocuSign__AccountId="..." \
  docusign-webhook-api
```

## MinIO Access

MinIO Console: http://localhost:9001
- Default credentials: `minioadmin` / `minioadmin`
- View uploaded documents in the `docusign-documents` bucket

## Logging

Logs are written to:
- **Console**: Structured JSON logs
- **File**: `logs/docusign-webhook-{Date}.log` (rolling daily)

## Security Considerations

- **HMAC Validation**: Always validate DocuSign webhook signatures in production
- **HTTPS Only**: Use HTTPS for webhook endpoints (DocuSign requirement)
- **Secret Management**: Use Azure Key Vault, AWS Secrets Manager, or similar for secrets
- **Authentication**: Add API authentication (JWT, API keys) for management endpoints
- **CORS**: Restrict CORS policy in production
- **Rate Limiting**: Implement rate limiting for webhook endpoint

## Production Deployment

### Recommended Setup

1. **Background Service**: Replace `Task.Run` with `IHostedService` or message queue (RabbitMQ, Azure Service Bus)
2. **Retry Logic**: Implement exponential backoff for failed webhook processing
3. **Monitoring**: Add Application Insights or similar
4. **Health Checks**: Extend health checks for DocuSign API and MinIO connectivity
5. **Database**: Use managed PostgreSQL (AWS RDS, Azure Database)
6. **Object Storage**: Use production MinIO cluster or AWS S3
7. **Scaling**: Deploy multiple API instances behind load balancer

### Environment-Specific Config

- Development: `appsettings.Development.json`
- Production: Environment variables or Azure App Configuration

## Troubleshooting

### Webhook not received

- Check DocuSign Connect configuration
- Verify webhook URL is publicly accessible via HTTPS
- Check firewall rules
- Review DocuSign Connect logs in admin panel

### Documents not downloading

- Verify DocuSign AccessToken is valid and not expired
- Check AccountId matches the envelope's account
- Ensure API has permission to access envelopes
- Review application logs for specific errors

### MinIO upload failures

- Verify MinIO is running and accessible
- Check credentials in configuration
- Ensure bucket exists (API creates it automatically)
- Review MinIO server logs

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## License

MIT License - see LICENSE file for details

## Support

For issues and questions:
- Create an issue on GitHub
- Check DocuSign API documentation: https://developers.docusign.com
- MinIO documentation: https://min.io/docs

## Roadmap

- [ ] Add unit and integration tests
- [ ] Implement background worker service
- [ ] Add retry queue with dead letter queue
- [ ] Support for multiple DocuSign accounts
- [ ] Document versioning and history
- [ ] Webhook event replay functionality
- [ ] Metrics and monitoring dashboard
- [ ] Support for other cloud storage providers (Azure Blob, AWS S3)
