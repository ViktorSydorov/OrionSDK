# BulkAddAwsAccounts Verb Documentation

# BulkAddAwsAccounts

Adds multiple AWS accounts in a single call. The operation uses **partial-success** semantics: failed accounts are reported in the response while the remaining ones are added successfully.

> **Input limits & validation** (rejected up front with an `ArgumentException` — no accounts are created):
>
> - **Batch size:** at most **100** accounts per call. To add more, split into batches of ≤ 100.
> - **Payload size:** the JSON string must be ≤ **1 MB**.
>
> Per-account validation (reported as a failure entry, other accounts still processed):
>
> - `Name` is required, non-empty, and at most **255** characters.
> - Duplicate `Name` values within the same request (case-insensitive) — the first wins, later ones fail.

## Connecting to SWIS

```powershell
$swis = Connect-Swis -Hostname "orion.example.com" -Username "admin" -Password "password"
```

## Signature

```powershell
Invoke-SwisVerb $swis Orion.Cloud.Aws.Accounts BulkAddAwsAccounts @(, $json)
```

`$json` is a JSON string containing an array of account objects.

## Input structure

### `AwsBulkAddAccountInput`

| Field                      | Type       | Required | Description                                                                                                                                                                         |
| -------------------------- | ---------- | -------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Name`                     | string     | yes      | Unique account name in Orion                                                                                                                                                        |
| `AccessKeyId`              | string     | no¹      | AWS Access Key ID                                                                                                                                                                   |
| `SecretAccessKey`          | string     | no¹      | AWS Secret Access Key                                                                                                                                                               |
| `RoleArn`                  | string     | no       | AssumeRole chain (see below)                                                                                                                                                        |
| `ExistingCredentialsId`    | int        | no       | ID of existing credentials to reuse instead of providing keys                                                                                                                       |
| `AutoMonitoring`           | bool       | no       | Automatically monitor newly discovered resources (default `true`)                                                                                                                   |
| `PollingIntervalInSeconds` | int        | no       | Polling frequency in seconds (default `300`). Recommended range: 60–3600.                                                                                                           |
| `SelectedRegions`          | string\[\] | no       | AWS region system names (e.g.`"us-east-1"`). Null or empty = all regions                                                                                                            |
| `MonitorResourceType`      | int        | no       | Resources monitoring method:`0` = All resources (default), `1` = Monitor selected entities, `2` = Monitor by tags                                                                   |
| `EnableCostManagement`     | bool       | no       | Enable daily AWS Cost polling (default `false`). Independent from `MonitorResourceType`.                                                                                            |
| `RunCostManagementAtTime`  | string     | no       | Daily time to poll AWS Cost data, e.g.`"2024-01-01T14:30:00"`. Only used when `EnableCostManagement = true`. Defaults to the current server time if not specified (same as the UI). |
| `JobSettings`              | object     | no       | Additional scan settings (see below).`Tags` are only applied when `MonitorResourceType = 2`.                                                                                        |
| `AwsPollersSettings`       | object     | no       | Enable/disable individual AWS services (see below). Only used when `MonitorResourceType = 1` — for `0` all services are enabled automatically, for `2` pollers are not set at all.  |

¹ Required unless `ExistingCredentialsId` is provided or an IAM role without keys is used (Instance Profile).

### `RoleArn` format — role chaining

Each hop is separated by a semicolon (`;`). An optional External ID is appended after a pipe (`|`).

```
<ARN1>[|<externalId1>][;<ARN2>[|<externalId2>];...]
```

Examples:

- Single role: `arn:aws:iam::123456789012:role/OrionRole`
- Single role with External ID: `arn:aws:iam::123456789012:role/OrionRole|myExternalSecret`
- Two-hop chain: `arn:aws:iam::111111111111:role/HubRole;arn:aws:iam::222222222222:role/TargetRole`
- Two-hop chain with External IDs: `arn:aws:iam::111111111111:role/HubRole|extId1;arn:aws:iam::222222222222:role/TargetRole|extId2`

### `JobSettings`

| Field                                  | Type | Default | Description                                                         |
| -------------------------------------- | ---- | ------- | ------------------------------------------------------------------- |
| `NetworkScanEnabled`                   | bool | `false` | Enable network scanning                                             |
| `DnsScanEnabled`                       | bool | `false` | Enable DNS scanning                                                 |
| `VirtualNetworkGatewaysPollingEnabled` | bool | `false` | Enable Virtual Network Gateways polling                             |
| `NetworkScanInterval`                  | int  | `240`   | Network scan interval (seconds).`0` or omitted falls back to `240`. |
| `DnsScanInterval`                      | int  | `240`   | DNS scan interval (seconds).`0` or omitted falls back to `240`.     |

### `AwsPollersSettings`

All fields are `bool`. Default value is `false`.

| Field                                | AWS service       |
| ------------------------------------ | ----------------- |
| `AwsSqlDatabasesPollingEnabled`      | RDS               |
| `AwsLoadBalancersPollingEnabled`     | ALB / NLB         |
| `AwsBucketStoragePollingEnabled`     | S3                |
| `AwsDirectConnectPollingEnabled`     | Direct Connect    |
| `AwsDynamoDbPollingEnabled`          | DynamoDB          |
| `AwsElasticKubernetesPollingEnabled` | EKS               |
| `AwsLambdaFunctionPollingEnabled`    | Lambda            |
| `AwsElasticBeanstalkPollingEnabled`  | Elastic Beanstalk |
| `AwsTransitGatewayPollingEnabled`    | Transit Gateway   |

## Response structure

```powershell
$result = Invoke-SwisVerb $swis Orion.Cloud.Aws.Accounts BulkAddAwsAccounts @(, $json)
# $result is an XmlElement:
$result.Succeeded.AwsAccountAddSuccess | Select-Object `
    @{ N = 'Name';      E = { $_.SelectSingleNode('*[local-name()="Name"]').InnerText } },
    @{ N = 'AccountId'; E = { $_.AccountId } }
$result.Succeeded.AwsAccountAddFailure | Select-Object `
    @{ N = 'Name';      E = { $_.SelectSingleNode('*[local-name()="Name"]').InnerText } },
    @{ N = 'ErrorMessage'; E = { $_.ErrorMessage } }
```

Example response (as XML returned by SWIS):

```xml
<BulkAddAwsAccountsResult>
  <Succeeded>
    <AwsAccountAddSuccess>
      <Name>prod-account-1</Name>
      <AccountId>42</AccountId>
    </AwsAccountAddSuccess>
  </Succeeded>
  <Failed>
    <AwsAccountAddFailure>
      <Name>prod-account-2</Name>
      <ErrorMessage>An account with name 'prod-account-2' already exists.</ErrorMessage>
    </AwsAccountAddFailure>
  </Failed>
</BulkAddAwsAccountsResult>
```

---

## Examples

### 1. Minimal — single account, Access Key, all regions

```powershell
$json = '[{"Name":"prod-account-1","AccessKeyId":"AKIAIOSFODNN7EXAMPLE","SecretAccessKey":"wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"}]'

Invoke-SwisVerb $swis Orion.Cloud.Aws.Accounts BulkAddAwsAccounts @(, $json)
```

---

### 2. Multiple accounts — mixed configuration

If the first one fails (e.g. duplicate name), the second is still added.

```powershell
$json = @'
[
  {
    "Name": "prod-account-us",
    "AccessKeyId": "AKIAIOSFODNN7EXAMPLE",
    "SecretAccessKey": "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
    "AutoMonitoring": true,
    "PollingIntervalInSeconds": 300
  },
  {
    "Name": "prod-account-eu",
    "AccessKeyId": "AKIAI44QH8DHBEXAMPLE",
    "SecretAccessKey": "je7MtGbClwBF/2Zp9Utk/h3yCo8nvbEXAMPLEKEY",
    "AutoMonitoring": true,
    "PollingIntervalInSeconds": 600
  }
]
'@

$result = Invoke-SwisVerb $swis Orion.Cloud.Aws.Accounts BulkAddAwsAccounts @(, $json)
$result.Succeeded.AwsAccountAddSuccess | Select-Object `
    @{ N = 'Name';      E = { $_.SelectSingleNode('*[local-name()="Name"]').InnerText } },
    @{ N = 'AccountId'; E = { $_.AccountId } }
$result.Succeeded.AwsAccountAddFailure | Select-Object `
    @{ N = 'Name';      E = { $_.SelectSingleNode('*[local-name()="Name"]').InnerText } },
    @{ N = 'ErrorMessage'; E = { $_.ErrorMessage } }
```

---

### 3. Restricted regions

```powershell
$json = @'
[
  {
    "Name": "prod-account-limited",
    "AccessKeyId": "AKIAIOSFODNN7EXAMPLE",
    "SecretAccessKey": "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
    "AutoMonitoring": true,
    "PollingIntervalInSeconds": 300,
    "SelectedRegions": ["us-east-1", "us-west-2", "eu-west-1", "eu-central-1"]
  }
]
'@

Invoke-SwisVerb $swis Orion.Cloud.Aws.Accounts BulkAddAwsAccounts @(, $json)
```

---

### 4. All resources with cost monitoring

`MonitorResourceType = 0` (default) — all services are enabled automatically. Cost monitoring is off by default; enable it with `EnableCostManagement = true`:

```powershell
$json = '[{"Name":"prod-account-all","AccessKeyId":"AKIAIOSFODNN7EXAMPLE","SecretAccessKey":"wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY","AutoMonitoring":true,"PollingIntervalInSeconds":300,"EnableCostManagement":true,"RunCostManagementAtTime":"2024-01-01T02:00:00Z"}]'

Invoke-SwisVerb $swis Orion.Cloud.Aws.Accounts BulkAddAwsAccounts @(, $json)
```

---

### 5. Monitor selected entities — specific services only

`MonitorResourceType = 1` — only services listed in `AwsPollersSettings` are polled. Cost polling runs daily at 02:00 UTC:

```powershell
$json = @'
[
  {
    "Name": "prod-account-selective",
    "AccessKeyId": "AKIAIOSFODNN7EXAMPLE",
    "SecretAccessKey": "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
    "AutoMonitoring": true,
    "PollingIntervalInSeconds": 300,
    "MonitorResourceType": 1,
    "EnableCostManagement": true,
    "RunCostManagementAtTime": "2024-01-01T02:00:00Z",
    "AwsPollersSettings": {
      "AwsSqlDatabasesPollingEnabled": true,
      "AwsElasticKubernetesPollingEnabled": true
    }
  }
]
'@

Invoke-SwisVerb $swis Orion.Cloud.Aws.Accounts BulkAddAwsAccounts @(, $json)
```

> `MonitorResourceType = 0` (default) enables all services automatically — `AwsPollersSettings` is ignored in that case.

---

### 6. Tag-based monitoring

Monitor only resources tagged `Environment=Production`. Set `MonitorResourceType` to `2` — when tag filtering is active, `AwsPollersSettings` is ignored.

```powershell
$json = @'
[
  {
    "Name": "prod-account-tagged",
    "AccessKeyId": "AKIAIOSFODNN7EXAMPLE",
    "SecretAccessKey": "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
    "AutoMonitoring": true,
    "PollingIntervalInSeconds": 300,
    "MonitorResourceType": 2,
    "JobSettings": {
      "Tags": [
        { "Key": "Environment", "Value": "Production" }
      ]
    }
  }
]
'@

Invoke-SwisVerb $swis Orion.Cloud.Aws.Accounts BulkAddAwsAccounts @(, $json)
```

---

### 7. AssumeRole — single role

```powershell
$json = '[{"Name":"prod-account-assumerole","RoleArn":"arn:aws:iam::123456789012:role/OrionMonitoringRole","AutoMonitoring":true,"PollingIntervalInSeconds":300}]'

Invoke-SwisVerb $swis Orion.Cloud.Aws.Accounts BulkAddAwsAccounts @(, $json)
```

---

### 8. AssumeRole with External ID

```powershell
$json = '[{"Name":"prod-account-external-id","RoleArn":"arn:aws:iam::123456789012:role/OrionMonitoringRole|my-secret-external-id","AutoMonitoring":true,"PollingIntervalInSeconds":300}]'

Invoke-SwisVerb $swis Orion.Cloud.Aws.Accounts BulkAddAwsAccounts @(, $json)
```

---

### 9. Role chaining — Landing Zone / hub-and-spoke

```powershell
$json = '[{"Name":"spoke-account-prod","RoleArn":"arn:aws:iam::111111111111:role/HubRole|hub-external-id;arn:aws:iam::222222222222:role/TargetRole","AutoMonitoring":true,"PollingIntervalInSeconds":300}]'

Invoke-SwisVerb $swis Orion.Cloud.Aws.Accounts BulkAddAwsAccounts @(, $json)
```

---

### 10. Bulk import from AWS Organizations — shared role, different Account IDs

```powershell
$accountIds = @("111111111111", "222222222222", "333333333333")

$accounts = $accountIds | ForEach-Object {
    @{
        Name                    = "org-account-$_"
        RoleArn                 = "arn:aws:iam::${_}:role/OrionMonitoringRole|shared-external-id"
        AutoMonitoring           = $true
        PollingIntervalInSeconds = 300
        MonitorResourceType      = 1
        SelectedRegions          = @("us-east-1", "eu-west-1")
        AwsPollersSettings       = @{
            AwsSqlDatabasesPollingEnabled      = $true
            AwsLoadBalancersPollingEnabled     = $true
            AwsElasticKubernetesPollingEnabled = $true
        }
    }
}

$json = $accounts | ConvertTo-Json -Depth 5

$result = Invoke-SwisVerb $swis Orion.Cloud.Aws.Accounts BulkAddAwsAccounts @(, $json)
$result.Succeeded.AwsAccountAddSuccess | Select-Object `
    @{ N = 'Name';      E = { $_.SelectSingleNode('*[local-name()="Name"]').InnerText } },
    @{ N = 'AccountId'; E = { $_.AccountId } }
$result.Succeeded.AwsAccountAddFailure | Select-Object `
    @{ N = 'Name';      E = { $_.SelectSingleNode('*[local-name()="Name"]').InnerText } },
    @{ N = 'ErrorMessage'; E = { $_.ErrorMessage } }
```

---

### 11. Reusing existing credentials

To find the credential ID, query either by account name or directly from `Orion.Credential`:

```powershell
# Option A — look up from an existing account
$credId = (Get-SwisData $swis "SELECT CredentialId FROM Orion.Cloud.Aws.Accounts WHERE Name = 'existing-account'").CredentialId

# Option B — list all AWS credentials stored in Orion
Get-SwisData $swis "SELECT ID, Name FROM Orion.Credential WHERE CredentialOwner = 'CloudMonitoring' AND CredentialType LIKE '%AwsCredentials%' ORDER BY Name"
$credId = 123  # use the ID from the query above
```

```powershell
$json = @"
[
  {
    "Name": "prod-account-shared-creds-1",
    "ExistingCredentialsId": $credId,
    "RoleArn": "arn:aws:iam::333333333333:role/OrionMonitoringRole",
    "AutoMonitoring": true,
    "PollingIntervalInSeconds": 300
  },
  {
    "Name": "prod-account-shared-creds-2",
    "ExistingCredentialsId": $credId,
    "RoleArn": "arn:aws:iam::444444444444:role/OrionMonitoringRole",
    "AutoMonitoring": true,
    "PollingIntervalInSeconds": 300
  }
]
"@

Invoke-SwisVerb $swis Orion.Cloud.Aws.Accounts BulkAddAwsAccounts @(, $json)
```

---

### 12. Full configuration

```powershell
$json = @'
[
  {
    "Name": "prod-account-full",
    "RoleArn": "arn:aws:iam::111111111111:role/HubRole|hub-ext-id;arn:aws:iam::999999999999:role/TargetRole|target-ext-id",
    "AutoMonitoring": true,
    "PollingIntervalInSeconds": 300,
    "MonitorResourceType": 1,
    "EnableCostManagement": true,
    "RunCostManagementAtTime": "2024-01-01T02:00:00Z",
    "SelectedRegions": ["us-east-1", "us-west-2", "eu-west-1"],
    "JobSettings": {
      "NetworkScanEnabled": true,
      "DnsScanEnabled": true,
      "NetworkScanInterval": 3600,
      "DnsScanInterval": 3600
    },
    "AwsPollersSettings": {
      "AwsSqlDatabasesPollingEnabled": true,
      "AwsLoadBalancersPollingEnabled": true,
      "AwsBucketStoragePollingEnabled": true,
      "AwsElasticKubernetesPollingEnabled": true,
      "AwsLambdaFunctionPollingEnabled": true,
      "AwsTransitGatewayPollingEnabled": true
    }
  }
]
'@

$result = Invoke-SwisVerb $swis Orion.Cloud.Aws.Accounts BulkAddAwsAccounts @(, $json)
$result.Succeeded.AwsAccountAddSuccess | Select-Object `
    @{ N = 'Name';      E = { $_.SelectSingleNode('*[local-name()="Name"]').InnerText } },
    @{ N = 'AccountId'; E = { $_.AccountId } }
$result.Succeeded.AwsAccountAddFailure | Select-Object `
    @{ N = 'Name';      E = { $_.SelectSingleNode('*[local-name()="Name"]').InnerText } },
    @{ N = 'ErrorMessage'; E = { $_.ErrorMessage } }
```

---

## Error handling

The operation uses partial-success semantics — a single failed account does not block the others.

Common `ErrorMessage` values:

- `An account with name '...' already exists.` — duplicate account name
- `Unknown AWS region name(s): ap-southeast-99. Use SystemName values such as 'us-east-1'.` — region does not exist in Orion
- `AWS region(s) not available for monitoring: ap-southeast-99.` — region exists but is disabled in Orion

To retrieve all available region names:

```powershell
Get-SwisData $swis "SELECT SystemName, DisplayName FROM Orion.Cloud.Aws.Regions ORDER BY SystemName"
```
