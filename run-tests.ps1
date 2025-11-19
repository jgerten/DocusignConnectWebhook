# PowerShell script to run tests with code coverage

Write-Host "Running tests with code coverage..." -ForegroundColor Cyan

dotnet test tests/DocuSignWebhook.Tests/DocuSignWebhook.Tests.csproj `
  /p:CollectCoverage=true `
  /p:CoverletOutputFormat=cobertura `
  /p:CoverletOutput=./TestResults/coverage.cobertura.xml `
  /p:Exclude="[xunit.*]*%2c[*.Tests]*" `
  --verbosity normal

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Tests completed successfully!" -ForegroundColor Green

    # Check if reportgenerator is installed
    if (Get-Command reportgenerator -ErrorAction SilentlyContinue) {
        Write-Host ""
        Write-Host "Generating HTML coverage report..." -ForegroundColor Cyan
        reportgenerator `
          -reports:tests/DocuSignWebhook.Tests/TestResults/coverage.cobertura.xml `
          -targetdir:tests/DocuSignWebhook.Tests/TestResults/coverage-report `
          -reporttypes:Html

        Write-Host ""
        Write-Host "Coverage report generated at: tests/DocuSignWebhook.Tests/TestResults/coverage-report/index.html" -ForegroundColor Green
    }
    else {
        Write-Host ""
        Write-Host "To generate HTML coverage reports, install reportgenerator:" -ForegroundColor Yellow
        Write-Host "  dotnet tool install -g dotnet-reportgenerator-globaltool" -ForegroundColor Yellow
    }
}
else {
    Write-Host ""
    Write-Host "Tests failed!" -ForegroundColor Red
    exit 1
}
