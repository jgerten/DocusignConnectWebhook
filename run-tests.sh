#!/bin/bash

# Script to run tests with code coverage

echo "Running tests with code coverage..."

dotnet test tests/DocuSignWebhook.Tests/DocuSignWebhook.Tests.csproj \
  /p:CollectCoverage=true \
  /p:CoverletOutputFormat=cobertura \
  /p:CoverletOutput=./TestResults/coverage.cobertura.xml \
  /p:Exclude="[xunit.*]*%2c[*.Tests]*" \
  --verbosity normal

if [ $? -eq 0 ]; then
    echo ""
    echo "Tests completed successfully!"

    # Check if reportgenerator is installed
    if command -v reportgenerator &> /dev/null; then
        echo ""
        echo "Generating HTML coverage report..."
        reportgenerator \
          -reports:tests/DocuSignWebhook.Tests/TestResults/coverage.cobertura.xml \
          -targetdir:tests/DocuSignWebhook.Tests/TestResults/coverage-report \
          -reporttypes:Html

        echo ""
        echo "Coverage report generated at: tests/DocuSignWebhook.Tests/TestResults/coverage-report/index.html"
    else
        echo ""
        echo "To generate HTML coverage reports, install reportgenerator:"
        echo "  dotnet tool install -g dotnet-reportgenerator-globaltool"
    fi
else
    echo ""
    echo "Tests failed!"
    exit 1
fi
