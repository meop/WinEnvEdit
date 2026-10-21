# Microsoft Store release automation

The stable-release pipeline calls `.github/workflows/store.yaml` after publishing
the GitHub release. It verifies and wraps the released x64 and ARM64 portable
binaries, validates their desktop-only manifests, and creates one unsigned MSIX
bundle. Microsoft signs the package after accepting the Store submission.

Automatic submission is disabled until credentials are configured and the
repository variable `STORE_PUBLISH_ENABLED` is set to `true`. Preparation works
without Store credentials. No application version bump is needed to configure it.

## One-time account setup

Verified against [Microsoft's current Store automation instructions](https://learn.microsoft.com/en-us/windows/apps/publish/msstore-dev-cli/github-actions)
on October 21, 2026. **Microsoft Entra ID** is the current name for Azure Active
Directory. The `az ad` CLI command group and Microsoft's example `AZURE_AD_*`
GitHub secret names still use the old abbreviation; this workflow uses those
exact names. No Azure subscription, resource group or Azure Contributor role is
required for these directory operations.

### Install the CLI and sign in

The [official Windows installation command](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli-windows)
is:

```powershell
winget install --exact --id Microsoft.AzureCLI --source winget
```

Open a new PowerShell terminal after installation, then sign in interactively:

```powershell
az login --allow-no-subscriptions --output none
az account show --query '{tenantId:tenantId,account:user.name}' --output json
```

Complete sign-in/MFA yourself. The second command displays identifiers, not
credentials. If your account belongs to multiple tenants, use
`az login --tenant <tenant-id> --allow-no-subscriptions --output none` to choose
the tenant associated with Partner Center. `az login` does not create a tenant
or associate one with Partner Center. A personal Microsoft account alone is not
the publishing application's directory.

### Associate the directory in Partner Center

In Partner Center, use **Account settings > Tenants** to associate an existing
Microsoft Entra ID tenant. If you do not have one, Microsoft's documented route
is [Create a new Microsoft Entra ID tenant in Partner Center](https://learn.microsoft.com/en-us/windows/apps/publish/partner-center/create-new-azure-ad-tenant),
at no additional charge. Then sign into that tenant with the CLI as above.
Do not create an unrelated Azure subscription just to obtain these identifiers.

Partner Center tenant association and Store publishing roles are separate from
Azure resource access. Microsoft's [application-management instructions](https://learn.microsoft.com/en-us/windows/apps/publish/partner-center/manage-azure-ad-applications-in-partner-center)
require a Partner Center Manager account with Global Administrator permissions
in the tenant to manage these applications. Those are setup-user permissions;
do not grant the publishing application Global Administrator.

### Create or reuse the publishing application through the CLI

Run in the same PowerShell session after selecting the correct tenant:

```powershell
$storeTenantId = az account show --query tenantId --output tsv
if ($LASTEXITCODE -ne 0 -or -not $storeTenantId) { throw 'Sign into the associated tenant first.' }

$storeAppsJson = az ad app list --display-name 'WinEnvEdit GitHub publishing' --output json
if ($LASTEXITCODE -ne 0) { throw 'Could not read app registrations in this tenant.' }
$storeApps = @($storeAppsJson | ConvertFrom-Json | Where-Object displayName -EQ 'WinEnvEdit GitHub publishing')
if ($storeApps.Count -gt 1) { throw 'Multiple matching app registrations; select the intended client ID.' }
if ($storeApps.Count -eq 1) {
  $storeClientId = $storeApps[0].appId
} else {
  $storeClientId = az ad app create --display-name 'WinEnvEdit GitHub publishing' --sign-in-audience AzureADMyOrg --query appId --output tsv
  if ($LASTEXITCODE -ne 0 -or -not $storeClientId) { throw 'App registration failed.' }
}
[pscustomobject]@{ TenantId = $storeTenantId; ClientId = $storeClientId }
```

These [Azure CLI commands](https://learn.microsoft.com/en-us/cli/azure/ad/app)
create an app registration, not another Store product. They do not create an
Azure resource deployment or grant Azure subscription roles. The directory's
app-registration policy still applies.

In Partner Center, open **Account settings > User management > Microsoft Entra
applications > Add Microsoft Entra application**. Select this registration and
assign **Manager** under the Windows developer program. This is a Partner Center
role, not an Entra directory role. Azure CLI app creation alone does not grant
Store access.

Also obtain the **Seller ID** from Partner Center's **Account settings >
Developer settings / Identifiers**. It is the publisher account identifier,
not Store product ID `9PF2J3W4WRGH`, the app's publisher certificate string, or
an Azure subscription ID. Azure CLI does not expose the Store seller ID.

### Send credentials directly to GitHub

With GitHub CLI already authenticated, continue in the same PowerShell session.
Only the seller ID is typed; the new client secret goes directly from memory to
GitHub's encrypted environment secret, without printing it or writing a file:

```powershell
$storeSellerId = Read-Host 'Partner Center Seller ID'
if (-not $storeSellerId -or -not $storeTenantId -or -not $storeClientId) { throw 'Missing publishing identifiers.' }
foreach ($storeEntry in @{
  AZURE_AD_TENANT_ID = $storeTenantId
  AZURE_AD_APPLICATION_CLIENT_ID = $storeClientId
  SELLER_ID = $storeSellerId
}.GetEnumerator()) {
  $storeEntry.Value | gh secret set $storeEntry.Key --repo meop/WinEnvEdit --env microsoft-store
  if ($LASTEXITCODE -ne 0) { throw "Could not save $($storeEntry.Key)." }
}
$storeSecretExpiry = [DateTime]::UtcNow.AddMonths(6).ToString('yyyy-MM-ddTHH:mm:ssZ')
try {
  $storeClientSecret = az ad app credential reset --id $storeClientId --append --display-name 'GitHub Store publishing' --end-date $storeSecretExpiry --query password --output tsv --only-show-errors
  if ($LASTEXITCODE -ne 0 -or -not $storeClientSecret) { throw 'Client secret creation failed.' }
  $storeClientSecret | gh secret set AZURE_AD_APPLICATION_SECRET --repo meop/WinEnvEdit --env microsoft-store
  if ($LASTEXITCODE -ne 0) { throw 'Could not save the new client secret to GitHub.' }
} finally {
  Remove-Variable storeClientSecret -ErrorAction SilentlyContinue
}
Write-Host "Rotate the publishing client secret before $storeSecretExpiry"
gh secret list --repo meop/WinEnvEdit --env microsoft-store
```

[`--append`](https://learn.microsoft.com/en-us/cli/azure/ad/app/credential#az-ad-app-credential-reset)
preserves existing credentials; omitting it would remove them. If saving the
secret to GitHub fails, the new Entra credential still exists: retry with a new
credential and remove the unused one by its key ID. Existing secret values
cannot be retrieved later, only replaced. Avoid CLI debug output or PowerShell
tracing while handling secrets.

Alternatively, add the four values manually under
[Settings > Environments > microsoft-store](https://github.com/meop/WinEnvEdit/settings/environments).
The third secret is the client secret **value**, not its identifier. Do not
paste secrets into chat or commit them. Leave `STORE_PUBLISH_ENABLED=false`
until the access check below succeeds. The CLI steps above have been checked
against documentation; authenticated execution remains pending account setup.

## Check access and enable

1. Open Actions > Microsoft Store > Run workflow on `main`. Enter the existing
   release version (`1.1.1` currently) and choose operation **check**. This builds
   the bundle and verifies authenticated product access without creating a submission.
2. Once access succeeds, set repository Actions variable `STORE_PUBLISH_ENABLED`
   to `true`. Future stable releases from the normal pipeline then submit
   automatically. No polling schedule or separate manually created release is needed.
3. Microsoft still certifies every update. A successful submission command is
   not evidence of approval or public availability; check Partner Center status.

The workflow refuses to submit if Partner Center already has a pending submission,
including a listing-only draft or update in certification. Finish that update first.
It does not delete another submission. If a release is blocked, run the workflow
again with **submit** after resolving the existing submission. Use source whose
VERSION matches the released assets; do not resubmit the already published version.

Manual operation **prepare** only builds an artifact; it is the default.
The `microsoft-store` environment only permits deployments from `main`.

Package updates retain the existing Store listing, screenshots and pricing using
Microsoft's CLI. This workflow does not automatically synchronize Markdown/HTML
captions or website changes into the Store. Listing-only edits remain in Partner
Center. Do not edit API-created drafts in Partner Center while automation is
working on them; [Microsoft documents that restriction](https://learn.microsoft.com/en-us/windows/uwp/monetize/create-and-manage-submissions-using-windows-store-services).

## Validation and limitations

The release packager has been exercised against both real 1.1.1 release ZIPs,
including MakeAppx validation and creation of the two-architecture bundle.
Authenticated upload/certification is untested until the credentials are configured.
Manifest validation does not replace the remaining runtime checks in
[COMPATIBILITY.md](COMPATIBILITY.md). The older Store candidate workflow remains
a separate manual diagnostic; its hosted runtime limitation is not treated as a
passing test by this workflow.
