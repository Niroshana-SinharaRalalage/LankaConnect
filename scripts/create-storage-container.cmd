@echo off
echo Getting storage account key...
for /f "delims=" %%i in ('az storage account keys list --account-name lankaconnectstrgaccount --resource-group LankaConnect-Staging --query "[0].value" -o tsv 2^>nul') do set ACCOUNT_KEY=%%i

echo Creating business-images container...
az storage container create --name business-images --account-name lankaconnectstrgaccount --account-key "%ACCOUNT_KEY%" --public-access blob

echo Done!
