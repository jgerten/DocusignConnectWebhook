#!/bin/bash

# Color codes for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${GREEN}╔═══════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║  DocuSign Webhook API - Development Startup Script   ║${NC}"
echo -e "${GREEN}╚═══════════════════════════════════════════════════════╝${NC}"
echo ""

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}✗ Docker is not running. Please start Docker Desktop first.${NC}"
    exit 1
fi

echo -e "${GREEN}✓${NC} Docker is running"

# Start PostgreSQL and MinIO
echo ""
echo -e "${YELLOW}Starting PostgreSQL and MinIO...${NC}"
docker-compose up -d

# Wait for services to be healthy
echo -e "${YELLOW}Waiting for services to be ready...${NC}"
sleep 5

# Check if services are running
if docker-compose ps | grep -q "postgres.*Up"; then
    echo -e "${GREEN}✓${NC} PostgreSQL is running on port 6432"
else
    echo -e "${RED}✗${NC} PostgreSQL failed to start"
    exit 1
fi

if docker-compose ps | grep -q "minio.*Up"; then
    echo -e "${GREEN}✓${NC} MinIO is running on ports 9000 (API) and 9001 (Console)"
else
    echo -e "${RED}✗${NC} MinIO failed to start"
    exit 1
fi

# Check if appsettings.json has been configured
echo ""
echo -e "${YELLOW}Checking configuration...${NC}"
if grep -q "YOUR_ACCOUNT_ID_HERE\|your_account_id" src/DocuSignWebhook.API/appsettings.json 2>/dev/null; then
    echo -e "${YELLOW}⚠${NC}  Warning: DocuSign credentials not configured in appsettings.json"
    echo -e "   Update src/DocuSignWebhook.API/appsettings.json with your DocuSign credentials"
fi

# Check if ngrok is installed
echo ""
if command -v ngrok &> /dev/null; then
    echo -e "${GREEN}✓${NC} ngrok is installed"
    NGROK_INSTALLED=true
else
    echo -e "${YELLOW}⚠${NC}  ngrok is not installed"
    echo -e "   Install from: https://ngrok.com/download"
    echo -e "   Or run: brew install ngrok (Mac) / snap install ngrok (Linux)"
    NGROK_INSTALLED=false
fi

echo ""
echo -e "${GREEN}════════════════════════════════════════════════════════${NC}"
echo -e "${GREEN}  Infrastructure services are ready!${NC}"
echo -e "${GREEN}════════════════════════════════════════════════════════${NC}"
echo ""
echo -e "Next steps:"
echo ""
echo -e "1. ${YELLOW}Start the API:${NC}"
echo -e "   cd src/DocuSignWebhook.API"
echo -e "   dotnet run"
echo ""
echo -e "2. ${YELLOW}In a separate terminal, start ngrok:${NC}"
if [ "$NGROK_INSTALLED" = true ]; then
    echo -e "   ngrok http 5080"
else
    echo -e "   (Install ngrok first - see above)"
fi
echo ""
echo -e "3. ${YELLOW}Access these URLs:${NC}"
echo -e "   API Swagger:    https://localhost:5081/swagger"
echo -e "   MinIO Console:  http://localhost:9001 (minioadmin/minioadmin)"
echo -e "   ngrok Web UI:   http://localhost:4040 (after starting ngrok)"
echo ""
echo -e "4. ${YELLOW}Configure DocuSign Connect:${NC}"
echo -e "   See NGROK_SETUP.md for detailed instructions"
echo ""
echo -e "${GREEN}════════════════════════════════════════════════════════${NC}"
echo ""
echo "To stop services: docker-compose down"
echo "To view logs: docker-compose logs -f"
echo ""
