# ngrok Setup Guide for DocuSign Connect Webhook Testing

This guide walks you through setting up ngrok to expose your local DocuSign Webhook API to the internet for testing with DocuSign Connect.

## Why You Need ngrok

DocuSign Connect sends webhook events to a publicly accessible HTTPS URL. Since your development API runs on `localhost`, DocuSign cannot reach it. ngrok creates a secure tunnel from a public URL to your local machine.

## Step 1: Install ngrok

### Option A: Download from Website (Recommended)

1. Go to https://ngrok.com/download
2. Create a free account (required for authentication)
3. Download the appropriate version for your OS:
   - **Linux**: `wget https://bin.equinox.io/c/bNyj1mQVY4c/ngrok-v3-stable-linux-amd64.tgz`
   - **Mac**: Download the macOS version
   - **Windows**: Download the Windows ZIP

4. Extract and install:
   ```bash
   # Linux/Mac
   tar -xvzf ngrok-v3-stable-linux-amd64.tgz
   sudo mv ngrok /usr/local/bin/

   # Verify installation
   ngrok version
   ```

### Option B: Package Manager

```bash
# Mac (Homebrew)
brew install ngrok/ngrok/ngrok

# Linux (Snap)
snap install ngrok

# Windows (Chocolatey)
choco install ngrok
```

## Step 2: Authenticate ngrok

1. Sign up at https://dashboard.ngrok.com/signup (free tier is fine)
2. Get your authtoken from https://dashboard.ngrok.com/get-started/your-authtoken
3. Configure ngrok with your token:

```bash
ngrok config add-authtoken YOUR_AUTHTOKEN_HERE
```

## Step 3: Start Your API

First, ensure all dependencies are running:

```bash
# Start PostgreSQL and MinIO
docker-compose up -d

# Navigate to API project
cd src/DocuSignWebhook.API

# Run the API
dotnet run
```

The API should start on:
- HTTP: http://localhost:5080
- HTTPS: https://localhost:5081

## Step 4: Start ngrok Tunnel

### For HTTP (Port 5080)

Open a **new terminal window** and run:

```bash
ngrok http 5080
```

### For HTTPS (Port 5081)

If you prefer to tunnel the HTTPS endpoint:

```bash
ngrok http https://localhost:5081
```

### Expected Output

You should see something like:

```
ngrok

Session Status                online
Account                       your-email@example.com
Version                       3.x.x
Region                        United States (us)
Latency                       20ms
Web Interface                 http://127.0.0.1:4040
Forwarding                    https://abc123.ngrok-free.app -> http://localhost:5080

Connections                   ttl     opn     rt1     rt5     p50     p90
                              0       0       0.00    0.00    0.00    0.00
```

**Important**: Copy the `Forwarding` URL (e.g., `https://abc123.ngrok-free.app`)

## Step 5: Test the ngrok Tunnel

Test that your webhook endpoint is accessible:

```bash
# Replace with your actual ngrok URL
curl https://abc123.ngrok-free.app/api/docusignwebhook/health
```

Expected response:
```json
{
  "status": "healthy",
  "timestamp": "2024-11-19T10:30:00Z",
  "service": "DocuSign Webhook API"
}
```

If you get a response, your tunnel is working!

## Step 6: Configure DocuSign Connect

Now configure DocuSign to send webhooks to your ngrok URL:

1. Log into DocuSign Admin:
   - Demo account: https://admindemo.docusign.com
   - Production: https://admin.docusign.com

2. Navigate to: **Settings → Connect → Add Configuration**

3. Configure the webhook:
   ```
   Configuration Name: Local Development Webhook
   URL to Publish: https://abc123.ngrok-free.app/api/docusignwebhook

   Include Data: XML or JSON (choose JSON for easier testing)

   Events to Send:
   ✅ Envelope Sent
   ✅ Envelope Delivered
   ✅ Envelope Completed
   ✅ Envelope Declined
   ✅ Envelope Voided

   Enable HMAC: ✅ Yes
   HMAC Secret: [Generate a strong secret and save it]
   ```

4. **Copy the HMAC secret** and add it to your `appsettings.json`:

```json
{
  "DocuSign": {
    "AccountId": "your_account_id",
    "AccessToken": "your_access_token",
    "BasePath": "https://demo.docusign.net/restapi",
    "HmacSecret": "YOUR_HMAC_SECRET_HERE"
  }
}
```

5. **Restart your API** after updating the HMAC secret

## Step 7: Test the Complete Flow

### Option 1: Send a Test Event from DocuSign

1. In DocuSign Connect configuration, click **Test Connection**
2. DocuSign will send a test webhook to your ngrok URL
3. Check your API logs:
   ```bash
   tail -f logs/docusign-webhook-*.log
   ```

### Option 2: Create a Test Envelope

1. Create and send a test envelope in DocuSign
2. Sign the envelope
3. When completed, DocuSign will send a webhook to your API
4. Check the logs and database:

```bash
# View logs
tail -f logs/docusign-webhook-*.log

# Query database
docker exec -it docusign-postgres psql -U postgres -d docusign_webhook -c "SELECT * FROM \"WebhookEvents\" ORDER BY \"CreatedAt\" DESC LIMIT 5;"
```

### Option 3: Manual Test with curl

```bash
# Send a test webhook (without HMAC validation)
curl -X POST https://abc123.ngrok-free.app/api/docusignwebhook \
  -H "Content-Type: application/json" \
  -d '{
    "event": "envelope-completed",
    "data": {
      "envelopeId": "test-12345",
      "envelopeSummary": {
        "status": "completed"
      }
    }
  }'
```

## ngrok Web Interface

ngrok provides a web interface at http://localhost:4040 where you can:
- See all HTTP requests and responses
- Inspect webhook payloads from DocuSign
- Replay requests for debugging
- View connection statistics

This is **extremely useful** for debugging webhook issues!

## Important Considerations

### Free Tier Limitations

- **Random URL**: Each time you restart ngrok, you get a new URL (e.g., `abc123.ngrok-free.app`)
- **60 requests/minute**: Rate limit on free tier
- **Expires on restart**: URL changes when you restart ngrok
- **Warning banner**: ngrok shows a warning page on first visit (can skip)

**Impact**: You'll need to update the DocuSign Connect URL each time you restart ngrok.

### Paid Tier Benefits ($8-10/month)

- **Static domain**: Get a permanent subdomain (e.g., `myapp.ngrok.io`)
- **No warning page**: Direct access without interstitial
- **Higher limits**: More concurrent connections and requests
- **Reserved domains**: Custom domain support

For frequent development, the paid tier is worth it to avoid constantly updating DocuSign Connect.

## Using a Static Domain (Paid Tier)

If you have a paid ngrok account:

```bash
# Reserve a domain in ngrok dashboard
# Then use it in your tunnel
ngrok http 5080 --domain=myapp.ngrok.io
```

Configure DocuSign Connect once with:
```
https://myapp.ngrok.io/api/docusignwebhook
```

No need to update it every time!

## Alternative: ngrok Configuration File

Create a configuration file for easier management:

```bash
# Create/edit ngrok config
ngrok config edit
```

Add this configuration:

```yaml
version: "2"
authtoken: YOUR_AUTHTOKEN_HERE
tunnels:
  docusign:
    proto: http
    addr: 5080
    inspect: true
  docusign-https:
    proto: http
    addr: https://localhost:5081
    inspect: true
```

Then start the tunnel with:

```bash
ngrok start docusign
```

## Troubleshooting

### Issue: "ERR_NGROK_108" - Session limit exceeded

**Solution**: Free tier allows 1 tunnel at a time. Make sure you don't have multiple ngrok instances running.

```bash
# Kill all ngrok processes
pkill ngrok

# Start fresh
ngrok http 5080
```

### Issue: ngrok shows "502 Bad Gateway"

**Solution**: Your API is not running or not listening on the specified port.

```bash
# Verify API is running
curl http://localhost:5080/api/docusignwebhook/health

# Check if something else is using port 5080
lsof -i :5080
```

### Issue: DocuSign webhook returns 401 Unauthorized

**Solution**: HMAC signature validation is failing.

1. Verify the HMAC secret in `appsettings.json` matches DocuSign Connect configuration
2. Check the API logs for HMAC validation errors
3. Temporarily disable HMAC validation for testing (not recommended for production):

```csharp
// In DocuSignWebhookController.cs (for testing only!)
if (Request.Headers.TryGetValue("X-DocuSign-Signature-1", out var signature))
{
    // Comment out or skip validation during testing
    // if (!_webhookProcessor.ValidateHmacSignature(rawPayload, signature!))
    // {
    //     return Unauthorized("Invalid signature");
    // }
}
```

### Issue: ngrok tunnel works but DocuSign can't reach it

**Solution**:
1. Check firewall rules on your machine
2. Ensure your API is listening on 0.0.0.0, not just 127.0.0.1
3. Test the ngrok URL from an external device/network

### Issue: Warning page when accessing ngrok URL

**Solution**: This is expected on free tier. Click "Visit Site" to continue. DocuSign webhooks will work fine despite this.

## Production Alternatives to ngrok

For production deployments, **do not use ngrok**. Instead:

1. **Cloud Deployment**: Deploy to Azure App Service, AWS, or similar
2. **Static Public IP**: Use a VPS or dedicated server
3. **Cloudflare Tunnel**: Free alternative to ngrok for production
4. **VS Code Port Forwarding**: Built into VS Code (requires GitHub login)
5. **localtunnel**: Open-source alternative, but less reliable

## Best Practices

1. **Keep ngrok running**: Don't stop/restart unnecessarily to avoid URL changes
2. **Use the web interface**: Monitor incoming webhooks at http://localhost:4040
3. **Check logs**: Always monitor your API logs for errors
4. **Test thoroughly**: Use ngrok's replay feature to test edge cases
5. **Secure your webhook**: Always validate HMAC signatures in production
6. **Document your URL**: Keep track of your current ngrok URL during development

## Quick Reference Commands

```bash
# Start ngrok tunnel (HTTP)
ngrok http 5080

# Start ngrok tunnel (HTTPS)
ngrok http https://localhost:5081

# Start with custom subdomain (paid tier)
ngrok http 5080 --domain=myapp.ngrok.io

# Start with config file
ngrok start docusign

# View ngrok status
ngrok status

# Kill ngrok
pkill ngrok
```

## Testing Checklist

- [ ] PostgreSQL running (`docker-compose ps`)
- [ ] MinIO running (`docker-compose ps`)
- [ ] API running (`dotnet run` in src/DocuSignWebhook.API)
- [ ] ngrok tunnel active (`ngrok http 5080`)
- [ ] ngrok URL accessible (`curl https://your-ngrok-url.ngrok-free.app/api/docusignwebhook/health`)
- [ ] DocuSign Connect configured with ngrok URL
- [ ] HMAC secret updated in appsettings.json
- [ ] Test webhook sent and received successfully
- [ ] Logs showing successful processing
- [ ] Documents uploaded to MinIO

## Next Steps

After successfully testing with ngrok:

1. Test with real DocuSign envelopes
2. Monitor webhook processing in the database
3. Check MinIO for uploaded documents (http://localhost:9001)
4. Review logs for any errors
5. Implement proper error handling and retry logic
6. Plan for production deployment without ngrok

## Support

- ngrok docs: https://ngrok.com/docs
- DocuSign Connect: https://developers.docusign.com/platform/webhooks/connect/
- Issues with this project: Check logs in `logs/docusign-webhook-*.log`

Happy testing!
