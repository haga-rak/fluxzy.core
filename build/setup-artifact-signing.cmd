@echo off
setlocal EnableDelayedExpansion
REM ---------------------------------------------------------------------------
REM One-shot setup for Azure Artifact Signing of fluxzy.core releases.
REM Same app registration and certificate profile as the Fluxzy Desktop
REM repository (fluxzy/new-cert.md). Nothing is installed on the runner: the
REM sign tool authenticates with a client secret passed as GitHub secrets.
REM  1. broker-less az login
REM  2. app registration "fluxzy-ci-signing" + service principal (idempotent)
REM  3. "Artifact Signing Certificate Profile Signer" role on fluxzy-public
REM     for the CI app AND for the signed-in user (local signing)
REM  4. a new client secret on the app (appended, other repos keep theirs)
REM  5. GitHub Actions secrets AZURE_CLIENT_ID / AZURE_TENANT_ID / AZURE_CLIENT_SECRET
REM Re-running is safe but adds a client secret each time. Role propagation
REM takes ~10 minutes. Rotate before SECRET_YEARS elapse.
REM ---------------------------------------------------------------------------

set TENANT=2fc58ab3-5b39-4fbb-bc76-8a1b47727063
set SUBSCRIPTION=f1926627-5c3b-4b76-841c-a855c2513022
set APP_NAME=fluxzy-ci-signing
set GITHUB_REPO=haga-rak/fluxzy.core
set SECRET_NAME=github-fluxzy-core
set SECRET_YEARS=2
set ROLE=Artifact Signing Certificate Profile Signer
set PROFILE=/subscriptions/%SUBSCRIPTION%/resourceGroups/Fluxzy/providers/Microsoft.CodeSigning/codeSigningAccounts/fluxzyartifact/certificateProfiles/fluxzy-public

echo.
echo === 1. Azure login (browser window) ===
call az config set core.enable_broker_on_windows=false --only-show-errors
call az login --tenant %TENANT% --output none || goto :fail
call az account set --subscription %SUBSCRIPTION% || goto :fail
for /f "usebackq delims=" %%i in (`az ad signed-in-user show --query userPrincipalName -o tsv`) do set ME_UPN=%%i
for /f "usebackq delims=" %%i in (`az ad signed-in-user show --query id -o tsv`) do set ME_ID=%%i
echo Signed in as %ME_UPN%

echo.
echo === 2. App registration %APP_NAME% ===
set APP_ID=
for /f "usebackq delims=" %%i in (`az ad app list --display-name %APP_NAME% --query "[0].appId" -o tsv`) do set APP_ID=%%i
if "!APP_ID!"=="" (
    for /f "usebackq delims=" %%i in (`az ad app create --display-name %APP_NAME% --query appId -o tsv`) do set APP_ID=%%i
    echo Created app !APP_ID!
) else (
    echo App already exists: !APP_ID!
)
if "!APP_ID!"=="" goto :fail

set SP_ID=
for /f "usebackq delims=" %%i in (`az ad sp list --filter "appId eq '!APP_ID!'" --query "[0].id" -o tsv`) do set SP_ID=%%i
if "!SP_ID!"=="" (
    for /f "usebackq delims=" %%i in (`az ad sp create --id !APP_ID! --query id -o tsv`) do set SP_ID=%%i
    echo Created service principal !SP_ID!
) else (
    echo Service principal already exists: !SP_ID!
)
if "!SP_ID!"=="" goto :fail

echo.
echo === 3. Role "%ROLE%" on fluxzy-public ===
call az role assignment create --assignee-object-id !SP_ID! --assignee-principal-type ServicePrincipal --role "%ROLE%" --scope "%PROFILE%" --output none || goto :fail
echo   granted to %APP_NAME%
call az role assignment create --assignee-object-id %ME_ID% --assignee-principal-type User --role "%ROLE%" --scope "%PROFILE%" --output none || goto :fail
echo   granted to %ME_UPN%

echo.
echo === 4. Client secret "%SECRET_NAME%" (%SECRET_YEARS% years) ===
set CLIENT_SECRET=
for /f "usebackq delims=" %%i in (`az ad app credential reset --id !APP_ID! --append --display-name %SECRET_NAME% --years %SECRET_YEARS% --query password -o tsv`) do set CLIENT_SECRET=%%i
if "!CLIENT_SECRET!"=="" goto :fail
echo   created (not displayed)

echo.
echo === 5. GitHub secrets on %GITHUB_REPO% ===
call gh secret set AZURE_CLIENT_ID --repo %GITHUB_REPO% --body "!APP_ID!" || goto :fail
call gh secret set AZURE_TENANT_ID --repo %GITHUB_REPO% --body "%TENANT%" || goto :fail
call gh secret set AZURE_CLIENT_SECRET --repo %GITHUB_REPO% --body "!CLIENT_SECRET!" || goto :fail
set CLIENT_SECRET=
REM The expired Key Vault certificate secrets are no longer used; uncomment to remove them:
REM for %%s in (AZURE_VAULT_DESCRIPTION_URL AZURE_VAULT_URL AZURE_VAULT_CERTIFICATE AZURE_VAULT_CLIENT_ID AZURE_VAULT_CLIENT_SECRET AZURE_VAULT_TENANT_ID) do call gh secret delete %%s --repo %GITHUB_REPO%

echo.
echo === Done ===
echo   App (client) id : !APP_ID!
echo   Tenant id       : %TENANT%
echo Wait ~10 minutes for the role assignment, then run the "Publish CLI" workflow
echo (NO_SIGN=1 skips signing for a local build).
goto :end

:fail
echo.
echo *** FAILED (see error above) ***
exit /b 1

:end
endlocal
