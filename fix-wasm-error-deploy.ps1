# PowerShell script to fix WASM0005 error and deploy Empire TCG

Write-Host "Fixing WASM0005 error and deploying Empire TCG" -ForegroundColor Green

# Fix the project file that's causing WASM0005 error
Write-Host "Fixing Empire.Client.csproj to remove RuntimeIdentifier..." -ForegroundColor Yellow
Copy-Item "Empire.Client\Empire.Client.csproj.fixed" "Empire.Client\Empire.Client.csproj" -Force

Write-Host "Project file fixed!" -ForegroundColor Green
Write-Host ""
Write-Host "Issues fixed:" -ForegroundColor Cyan
Write-Host "   - Removed RuntimeIdentifier=browser-wasm from project file" -ForegroundColor Green
Write-Host "   - This should resolve the WASM0005 error" -ForegroundColor Green
Write-Host "   - Blazor WebAssembly should now build successfully" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "   1. You'll need to run Docker commands manually to rebuild and deploy" -ForegroundColor Yellow
Write-Host "   2. Or use your existing deployment script with the fixed project file" -ForegroundColor Yellow
Write-Host ""
Write-Host "Manual Docker commands to run:" -ForegroundColor Cyan
Write-Host "   docker-compose down" -ForegroundColor White
Write-Host "   docker system prune -af" -ForegroundColor White
Write-Host "   docker-compose build --no-cache --pull" -ForegroundColor White
Write-Host "   docker-compose up -d" -ForegroundColor White
Write-Host ""
Write-Host "After deployment, test:" -ForegroundColor Cyan
Write-Host "   Website: https://empirecardgame.com" -ForegroundColor White
Write-Host "   Deck builder: https://empirecardgame.com/deckbuilder" -ForegroundColor White
Write-Host "   Lobby: https://empirecardgame.com/lobby" -ForegroundColor White
Write-Host "   API: https://empirecardgame.com/api/deckbuilder/cards" -ForegroundColor White
