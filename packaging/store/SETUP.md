# One-time Store publishing setup

How this repository was given permission to submit Store updates from GitHub
Actions. Do this once. Routine publishing is in [README.md](README.md).

The end state is four secrets in the `microsoft-store` GitHub environment, held
by an Entra application that Partner Center trusts to act on the Store product.

Placeholders: `<tenant>` is the directory's initial domain
(`<something>.onmicrosoft.com`), `<setup-user>` a directory-native administrator,
`<app-name>` the publishing application's display name. Keep the real values in
private notes, not here.

## The personal-account gap

**If you publish as an Individual with a personal Microsoft account, read this
before anything else. It is the only genuinely hard part.**

Microsoft requires a personal account for Individual registration, but the Store
submission API is built around organizations. The API authenticates as an Entra
application belonging to a tenant associated with the publisher account, and
Partner Center's association sign-in accepts **work or school accounts only** —
its authorize URL carries `msafed=0`, which disables Microsoft Account
federation.

A personal account therefore cannot complete the association, and the failure is
misleading. Signing in with the personal address returns "You can't sign in here
with a personal account. Use your work or school account instead", which reads
like a missing permission. It is not. In a directory created from a personal
Microsoft account, the owner exists as a federated `#EXT#` identity
(`issuer: MicrosoftAccount`, `signInType: federated`) and that endpoint refuses
the identity *type*, so granting it Global Administrator changes nothing. Neither
does anything on the Azure CLI side: signing in, holding directory roles, creating
the app registration and configuring GitHub secrets all succeed and none of them
performs the association.

**The fix is to create one ordinary directory-native user in the existing tenant
and associate with that.** The tenant is otherwise fine; it simply has no native
identity, because an account created this way starts with exactly one user and
that user is the federated one. Step 1 does this.

What not to do: convert the publisher to Company, register a second Partner Center
account, buy a Microsoft 365 mailbox, or request extra Store permissions by email.
None addresses an identity-type check, and Individual registration with a personal
account is correct per Microsoft's
[individual onboarding guide](https://learn.microsoft.com/en-us/windows/apps/publish/partner-center/open-a-developer-account?tabs=individual).

This was an open question. A
[September 14, 2026 report](https://learn.microsoft.com/en-gb/answers/questions/6002797/personal-account-windows-publisher-cant-associate)
describes exactly this dead end for an individual publisher and received no
answer. The sequence below is the resolution, carried out and verified against a
live Individual account on October 24, 2026.

## What you need

- A Partner Center account with the Windows and Xbox program active. Individual
  registration is correct and uses a personal Microsoft account; do not convert
  it to Company for API access.
- A Microsoft Entra tenant. One already exists if you have ever signed into Azure
  or Entra with that account, named `<something>.onmicrosoft.com`.
- Azure CLI and GitHub CLI, both signed in. No Azure subscription, resource group
  or Contributor role is needed — these are directory operations only.

```powershell
winget install --exact --id Microsoft.AzureCLI --source winget
az login --allow-no-subscriptions --output none
az account show --query '{tenantId:tenantId,account:user.name}' --output json
```

Use `az login --tenant <tenant-id> --allow-no-subscriptions` when the account
belongs to several tenants. If tenant discovery fails with AADSTS50076 despite
working MFA, a tenant-specific login obtains the session; do not weaken MFA or
tenant security settings to get past it.

## 1. Create a directory-native administrator

This is the step that unblocks everything else, for the reason above. Confirm the
tenant has no native user before assuming otherwise — a directory created from a
personal account holds exactly one, federated, identity:

```powershell
az rest --url 'https://graph.microsoft.com/v1.0/users?$select=userPrincipalName,userType,identities' --output json
```

A `userPrincipalName` containing `#EXT#` with `issuer: MicrosoftAccount` is the
federated owner, and is not usable for association.

Grant the new user Global Administrator. The Tenants page asks for "the global admin
credentials of the other tenant", and the application-management step below
requires the role, even though Microsoft's
[association instructions](https://learn.microsoft.com/en-us/windows/apps/publish/partner-center/associate-existing-azure-ad-tenant-with-partner-center-account)
say association itself does not.

```powershell
$storeUserId = az ad user create --display-name 'Store Automation Admin' --user-principal-name "<setup-user>@<tenant>" --mail-nickname '<setup-user>' --password $storeUserPassword --force-change-password-next-sign-in true --query id --output tsv
if ($LASTEXITCODE -ne 0 -or -not $storeUserId) { throw 'Directory user creation failed.' }

Set-Content -Path $storeRoleBody -Encoding utf8 -Value "{`"principalId`":`"$storeUserId`",`"roleDefinitionId`":`"62e90394-69f5-4237-9190-012177145e10`",`"directoryScopeId`":`"/`"}"
az rest --method POST --url 'https://graph.microsoft.com/v1.0/roleManagement/directory/roleAssignments' --headers 'Content-Type=application/json' --body "@$storeRoleBody"
```

`62e90394-69f5-4237-9190-012177145e10` is the Global Administrator role template.
Pass Graph bodies from a file: the Windows `az` shim mangles inline JSON, and
cuts URLs at `&`. Sign the new user in once to clear the forced password change
and register MFA before using it in Partner Center.

This is a standing privileged account. Keep MFA on it and use it only for the
account steps here and for rotating the client secret later.

## 2. Associate the tenant with Partner Center

Signed into Partner Center as the account owner, open **Account settings >
Organization profile > Tenants**, then **Associate Microsoft Entra ID** from the
overflow menu beside *Create Microsoft Entra ID*. Supply the `<setup-user>`
credentials and select **Confirm**.

The live path is `/dashboard/v2/account-settings/organization/tenant-management`.
The former `/dashboard/account/v3/tenantsetup` returns the not-found page, and a
deep link carrying another program context lands on the "Join a program"
enrollment selector rather than an account error.

The user performing the association receives Partner Center **Account admin**
automatically, so it needs no separate invitation. Association is observable from
the directory side: a **Microsoft Partner Center** service principal
(`fa3d9a0c-3fb0-42cc-9193-47c7ecd2edbd`) appears in the tenant.

If association is ever refused, Microsoft's documented fallback is
[Create a new Microsoft Entra ID tenant in Partner Center](https://learn.microsoft.com/en-us/windows/apps/publish/partner-center/create-new-azure-ad-tenant),
which provisions a tenant with its own global administrator and associates it in
one flow at no additional charge. It costs the existing app registration and every
tenant-scoped secret, so treat it as a fallback. Do not convert the publisher to
Company or buy a mailbox over a sign-in error.

## 3. Register the publishing application

```powershell
$storeTenantId = az account show --query tenantId --output tsv
$storeApps = @(az ad app list --display-name '<app-name>' --output json | ConvertFrom-Json | Where-Object displayName -EQ '<app-name>')
if ($storeApps.Count -gt 1) { throw 'Multiple matching app registrations; select the intended client ID.' }
if ($storeApps.Count -eq 1) {
  $storeClientId = $storeApps[0].appId
} else {
  $storeClientId = az ad app create --display-name '<app-name>' --sign-in-audience AzureADMyOrg --query appId --output tsv
  if ($LASTEXITCODE -ne 0 -or -not $storeClientId) { throw 'App registration failed.' }
}

az ad sp show --id $storeClientId --only-show-errors 2>$null
if ($LASTEXITCODE -ne 0) {
  az ad sp create --id $storeClientId --query '{appId:appId,objectId:id}' --output json
  if ($LASTEXITCODE -ne 0) { throw 'Service principal creation failed.' }
}
```

The second block is not optional. `az ad app create` creates only the application
object; the service principal is a separate object the Azure portal creates
implicitly and the CLI does not. Without it the Partner Center picker in step 4
lists nothing to add, and a client-credentials token fails with
`AADSTS700016 Application not found in the directory`. The client ID is the same
either way, so adding it later does not invalidate existing secrets.

These [commands](https://learn.microsoft.com/en-us/cli/azure/ad/app) create a
directory application, not another Store product or any Azure resource. The
directory's own app-registration policy still applies.

## 4. Authorize the application in Partner Center

Open **Account settings > User management > Microsoft Entra applications >
Add Microsoft Entra application**. User management is reachable only while signed
in with the associated tenant's credentials, not the owner's personal account; the
page offers **Sign in with Microsoft Entra ID** for that.

Choose **Add Microsoft Entra application**, not *Create* — *Create* registers a
second application whose client ID will not match the configured secrets. Select
the registration and assign **Manager** under the Windows developer program. This
is a Partner Center role, not an Entra directory role; CLI app creation alone
grants no Store access. On success the application is listed twice, as *Microsoft
Entra Apps* and as *Service Principal*, both showing Manager(Windows).

Microsoft's
[submission API documentation](https://learn.microsoft.com/en-us/windows/apps/publish/store-submission-api)
specifies Manager. **Developer** ("can upload packages and submit apps") is the
narrower role and may be sufficient; it is a one-checkbox change here plus a
re-run of the access check, worth testing once a real submission has succeeded.

Take the **Seller ID** from **Account settings > Legal info > Developer >
Publisher IDs**. It is the publisher account identifier — not the Store product
ID, not the package publisher certificate string, not an Azure subscription ID.
Azure CLI cannot read it.

## 5. Store the credentials in GitHub

Only the seller ID is typed. The client secret goes from memory straight into an
encrypted environment secret, never printed and never written to a file.

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
```

`AZURE_AD_*` are the secret names from Microsoft's own examples, retained by the
workflow; they are not a product name.
[`--append`](https://learn.microsoft.com/en-us/cli/azure/ad/app/credential#az-ad-app-credential-reset)
preserves existing credentials — omitting it removes them. If the GitHub write
fails the Entra credential still exists: create another and delete the unused one
by its key ID. Secret values cannot be read back later, only replaced. Avoid CLI
debug output and PowerShell tracing while handling them.

The values can also be entered by hand under
[Settings > Environments > microsoft-store](https://github.com/meop/WinEnvEdit/settings/environments).
The third secret is the client secret **value**, not its identifier.

## 6. Verify access, then enable

Run **Actions > Microsoft Store > Run workflow** on `main` with the current
release version and operation **check**. It builds the bundle and proves
authenticated product access without creating a submission. A passing run reports
a working configuration from `msstore reconfigure` and retrieves the product.

Only then set the repository variable `STORE_PUBLISH_ENABLED` to `true`. Stable
releases submit automatically from that point.

## Maintenance

- Rotate the client secret before it expires; step 5's command is re-runnable and
  `--append` keeps the old credential alive until you remove it.
- The setup user is only needed to manage the application or the association. It
  is not used by the workflow and never appears in a secret.
