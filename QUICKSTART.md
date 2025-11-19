# Quick Start Guide

Get your DocuSign Webhook API up and running in 10 minutes!

## Step 1: Install Prerequisites

Ensure you have these installed:
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for PostgreSQL and MinIO)

## Step 2: Start Infrastructure Services

```bash
cd /home/user/DocuSignWebhookAPI

# Start PostgreSQL and MinIO
docker-compose up -d

# Verify services are running
docker-compose ps
```

You should see:
- PostgreSQL on port 6432
- MinIO on port 9000 (API) and 9001 (Console)

## Step 3: Configure the Application

Edit `src/DocuSignWebhook.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=6432;Database=docusign_webhook;Username=postgres;Password=postgres123"
  },
  "DocuSign": {
    "AccountId": "YOUR_ACCOUNT_ID_HERE",
    "AccessToken": "YOUR_JWT_TOKEN_HERE",
    "BasePath": "https://demo.docusign.net/restapi",
    "HmacSecret": "YOUR_HMAC_SECRET_HERE"
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

### Getting DocuSign Credentials:

1. Go to https://developers.docusign.com
2. Create a developer account (free)
3. Create an app and get your **Integration Key**
4. Generate a **JWT access token** (good for 1 hour, see below for refresh)
5. Note your **Account ID** from the admin panel

For production, use OAuth 2.0 instead of JWT tokens.

## Step 4: Build and Run the API

```bash
# Navigate to the API project
cd src/DocuSignWebhook.API

# Restore NuGet packages
dotnet restore

# Apply database migrations
dotnet ef database update

# Run the API
dotnet run
```

The API will start at: **https://localhost:5081**

## Step 5: Test the API

Open your browser to: **https://localhost:5081/swagger**

You should see the Swagger UI with these endpoints:

### Test the health check:
```bash
curl https://localhost:5081/api/docusignwebhook/health
```

Expected response:
```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "service": "DocuSign Webhook API"
}
```

## Step 6: Configure DocuSign Connect (Optional - for receiving webhooks)

1. Log into DocuSign Admin: https://admindemo.docusign.com
2. Go to **Settings → Connect**
3. Click **Add Configuration**
4. Configure:
   - **URL**: `https://your-public-url.com/api/docusignwebhook`
   - **Enable HMAC**: Yes → Save the secret to your config
   - **Events to Send**:
     - ✅ Envelope Completed
     - ✅ Envelope Sent
     - ✅ Envelope Voided
5. Save the configuration

**Note**: For local development, use [ngrok](https://ngrok.com) to expose your local API:

```bash
# In a separate terminal
ngrok http https://localhost:5081

# Use the ngrok URL in DocuSign Connect
# Example: https://abc123.ngrok.io/api/docusignwebhook
```

## Step 7: Access MinIO Console

Open: **http://localhost:9001**

Login with:
- Username: `minioadmin`
- Password: `minioadmin`

After processing a webhook, you'll see documents in the `docusign-documents` bucket.

## Testing the Workflow

### Manual Webhook Test

Send a test webhook using curl:

```bash
curl -X POST https://localhost:5081/api/docusignwebhook \
  -H "Content-Type: application/json" \
  -d '{
    "event": "envelope-completed",
    "envelopeId": "test-envelope-123",
    "status": "completed",
    "data": {
      "envelopeId": "test-envelope-123",
      "envelopeSummary": {
        "status": "completed"
      }
    }
  }'
```

This will create a webhook event record (but won't download real documents without a valid DocuSign envelope).

### Check Webhook Event Status

```bash
# Get the webhookEventId from the response above
curl https://localhost:5081/api/docusignwebhook/{webhookEventId}
```

## Common Issues

### Issue: "Connection refused" to PostgreSQL

**Solution**: Ensure Docker is running and PostgreSQL container is up:
```bash
docker-compose ps
docker-compose logs postgres
```

### Issue: "MinIO connection failed"

**Solution**: Check MinIO is running:
```bash
docker-compose logs minio
```

### Issue: "DocuSign API authentication failed"

**Solution**:
1. JWT tokens expire after 1 hour - generate a new one
2. Verify your Account ID matches the envelope's account
3. Check the BasePath (demo vs production)

### Issue: Database migration fails

**Solution**:
```bash
# Reset database
docker-compose down -v
docker-compose up -d

# Re-run migration
cd src/DocuSignWebhook.API
dotnet ef database update
```

## Next Steps

1. **Add Background Processing**: Replace `Task.Run` with `IHostedService` for production
2. **Implement Retry Logic**: Add retry queue for failed webhook processing
3. **Add Authentication**: Secure your API endpoints with JWT or API keys
4. **Set up Monitoring**: Add Application Insights or Serilog to cloud logging
5. **Deploy**: Deploy to Azure App Service, AWS, or your preferred cloud

## Useful Commands

```bash
# View logs
tail -f src/DocuSignWebhook.API/logs/docusign-webhook-*.log

# Stop all services
docker-compose down

# Reset everything (removes data!)
docker-compose down -v

# View database tables
docker exec -it docusign-postgres psql -U postgres -d docusign_webhook -c "\dt"

# Query webhook events
docker exec -it docusign-postgres psql -U postgres -d docusign_webhook -c "SELECT * FROM \"WebhookEvents\";"
```

## Production Checklist

Before deploying to production:

- [ ] Replace JWT tokens with OAuth 2.0
- [ ] Use Azure Key Vault / AWS Secrets Manager for secrets
- [ ] Implement proper background processing (Hangfire, Azure Functions)
- [ ] Add comprehensive error handling and retry logic
- [ ] Set up monitoring and alerting
- [ ] Use managed PostgreSQL (Azure Database, AWS RDS)
- [ ] Use production MinIO cluster or AWS S3
- [ ] Enable HTTPS only
- [ ] Restrict CORS policy
- [ ] Add rate limiting
- [ ] Implement logging aggregation (ELK, Datadog, etc.)
- [ ] Add health checks for all dependencies
- [ ] Set up CI/CD pipeline

## Support

Questions? Check the main [README.md](README.md) for detailed documentation.

Need help?
- DocuSign API Docs: https://developers.docusign.com/docs/esign-rest-api/
- MinIO Docs: https://min.io/docs/
- .NET 8 Docs: https://learn.microsoft.com/dotnet/
