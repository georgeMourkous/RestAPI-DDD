<#
.SYNOPSIS
Creates the AWS infrastructure used by the RestApiDdd API.

.DESCRIPTION
This script provisions a portable test stack for this solution:

- ECR repository for the API image
- IAM task and execution roles with the policies this app needs
- CloudWatch log groups used by the ECS awslogs driver and the app Serilog sink
- Security groups for ALB, ECS tasks, and RDS
- RDS SQL Server instance and subnet group
- Systems Manager Parameter Store SecureString values for the JWT signing key and DB connection string
- Application Load Balancer, target group, and listener
- ECS cluster, task definition, and Fargate service

It is intentionally parameterized around an existing VPC and subnets so you can
recreate the same service shape in another account, region, or VPC without
editing the script.

Prerequisites: AWS CLI v2, AWS credentials with permissions for the services
below, and Docker if you pass -BuildAndPushImage.

.EXAMPLE
.\deploy\provision-aws.ps1 `
  -Region us-east-1 `
  -VpcId vpc-0123456789abcdef0 `
  -PublicSubnetIds subnet-11111111111111111,subnet-22222222222222222 `
  -PrivateSubnetIds subnet-33333333333333333,subnet-44444444444444444 `
  -DbPassword (Read-Host "RDS master password" -AsSecureString) `
  -BuildAndPushImage

.EXAMPLE
.\deploy\provision-aws.ps1 `
  -Profile developer `
  -Region us-east-1 `
  -NamePrefix restapi-ddd-preview `
  -VpcId vpc-0123456789abcdef0 `
  -PublicSubnetIds subnet-11111111111111111,subnet-22222222222222222 `
  -PrivateSubnetIds subnet-33333333333333333,subnet-44444444444444444 `
  -DbPassword (Read-Host "RDS master password" -AsSecureString) `
  -DesiredCount 1
#>

[CmdletBinding()]
param(
    [string]$Profile,

    [Parameter(Mandatory = $true)]
    [string]$Region,

    [string]$NamePrefix = "restapi-ddd",

    [Parameter(Mandatory = $true)]
    [string]$VpcId,

    [Parameter(Mandatory = $true)]
    [string[]]$PublicSubnetIds,

    [Parameter(Mandatory = $true)]
    [string[]]$PrivateSubnetIds,

    [string[]]$EcsSubnetIds,

    [string[]]$DbSubnetIds,

    [string]$AllowedHttpCidr = "0.0.0.0/0",

    [string]$AdminCidrToDb,

    [string]$CertificateArn,

    [string]$RepositoryName,

    [string]$ImageTag = "latest",

    [switch]$BuildAndPushImage,

    [string]$ClusterName,

    [string]$ServiceName,

    [string]$TaskFamily,

    [string]$ContainerName,

    [int]$ContainerPort = 8080,

    [string]$TaskCpu = "1024",

    [string]$TaskMemory = "2048",

    [int]$DesiredCount = 1,

    [string]$ExecutionRoleName,

    [string]$TaskRoleName,

    [string]$LoadBalancerName,

    [string]$TargetGroupName,

    [string]$AlbSecurityGroupName,

    [string]$EcsSecurityGroupName,

    [string]$RdsSecurityGroupName,

    [string]$DbSubnetGroupName,

    [string]$DbInstanceIdentifier,

    [ValidateSet("sqlserver-ex", "sqlserver-web", "sqlserver-se", "sqlserver-ee")]
    [string]$DbEngine = "sqlserver-ex",

    [string]$DbEngineVersion,

    [string]$DbInstanceClass = "db.t3.micro",

    [int]$DbAllocatedStorageGb = 20,

    [string]$DbStorageType = "gp2",

    [string]$DbUsername = "restapiadmin",

    [SecureString]$DbPassword,

    [string]$DatabaseName = "RestApiDdd",

    [int]$DbBackupRetentionDays = 0,

    [switch]$PubliclyAccessibleDb,

    [switch]$DeletionProtection,

    [SecureString]$JwtSigningKey,

    [string]$JwtSigningKeyParameterName,

    [string]$DefaultConnectionParameterName,

    [string]$KmsKeyId,

    [string]$EcsLogGroup,

    [string]$ApiLogGroup,

    [string]$RequestLogGroup,

    [int]$LogRetentionDays = 30,

    [ValidateSet("ENABLED", "DISABLED")]
    [string]$AssignPublicIp = "ENABLED",

    [int]$HealthCheckGracePeriodSeconds = 60,

    [string]$ProjectRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not $DbPassword) {
    $DbPassword = Read-Host "RDS master password" -AsSecureString
}

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

$isOriginalStackName = $NamePrefix -eq "restapi-ddd"

if ([string]::IsNullOrWhiteSpace($RepositoryName)) { $RepositoryName = "$NamePrefix-api" }
if ([string]::IsNullOrWhiteSpace($ClusterName)) { $ClusterName = "$NamePrefix-cluster" }
if ([string]::IsNullOrWhiteSpace($ServiceName)) { $ServiceName = "$NamePrefix-api-service" }
if ([string]::IsNullOrWhiteSpace($TaskFamily)) { $TaskFamily = "$NamePrefix-api-TaskDefination" }
if ([string]::IsNullOrWhiteSpace($ContainerName)) { $ContainerName = "$NamePrefix-api" }
if ([string]::IsNullOrWhiteSpace($ExecutionRoleName)) {
    $ExecutionRoleName = if ($isOriginalStackName) { "ecsTaskExecutionRole" } else { "$NamePrefix-ecs-execution-role" }
}
if ([string]::IsNullOrWhiteSpace($TaskRoleName)) {
    $TaskRoleName = if ($isOriginalStackName) { "RestApiDddEcsTaskRole" } else { "$NamePrefix-ecs-task-role" }
}
if ([string]::IsNullOrWhiteSpace($LoadBalancerName)) { $LoadBalancerName = "$NamePrefix-api-alb" }
if ([string]::IsNullOrWhiteSpace($TargetGroupName)) { $TargetGroupName = "$NamePrefix-api-tg" }
if ([string]::IsNullOrWhiteSpace($AlbSecurityGroupName)) { $AlbSecurityGroupName = "$NamePrefix-alb-sg" }
if ([string]::IsNullOrWhiteSpace($EcsSecurityGroupName)) { $EcsSecurityGroupName = "$NamePrefix-ecs-sg" }
if ([string]::IsNullOrWhiteSpace($RdsSecurityGroupName)) { $RdsSecurityGroupName = "$NamePrefix-rds-sg" }
if ([string]::IsNullOrWhiteSpace($DbSubnetGroupName)) { $DbSubnetGroupName = "$NamePrefix-db-subnet-group" }
if ([string]::IsNullOrWhiteSpace($DbInstanceIdentifier)) { $DbInstanceIdentifier = "$NamePrefix-sqlserver" }
if ([string]::IsNullOrWhiteSpace($JwtSigningKeyParameterName)) {
    $JwtSigningKeyParameterName = if ($isOriginalStackName) { "/RestApiDdd/Production/Jwt/SigningKey" } else { "/$NamePrefix/Production/Jwt/SigningKey" }
}
if ([string]::IsNullOrWhiteSpace($DefaultConnectionParameterName)) {
    $DefaultConnectionParameterName = if ($isOriginalStackName) { "/RestApiDdd/Production/ConnectionStrings/DefaultConnection" } else { "/$NamePrefix/Production/ConnectionStrings/DefaultConnection" }
}
if ([string]::IsNullOrWhiteSpace($EcsLogGroup)) { $EcsLogGroup = "/ecs/$TaskFamily" }
if ([string]::IsNullOrWhiteSpace($ApiLogGroup)) {
    $ApiLogGroup = if ($isOriginalStackName) { "/rest-api-ddd/api" } else { "/$NamePrefix/api" }
}
if ([string]::IsNullOrWhiteSpace($RequestLogGroup)) {
    $RequestLogGroup = if ($isOriginalStackName) { "/rest-api-ddd/api-requests" } else { "/$NamePrefix/api-requests" }
}

if (-not $EcsSubnetIds -or $EcsSubnetIds.Count -eq 0) {
    $EcsSubnetIds = $PublicSubnetIds
}

if (-not $DbSubnetIds -or $DbSubnetIds.Count -eq 0) {
    $DbSubnetIds = $PrivateSubnetIds
}

if ($PublicSubnetIds.Count -lt 2) {
    throw "Pass at least two public subnets in different Availability Zones for the ALB."
}

if ($DbSubnetIds.Count -lt 2) {
    throw "Pass at least two database subnets in different Availability Zones for the RDS subnet group."
}

if ($LoadBalancerName.Length -gt 32) {
    throw "Load balancer name '$LoadBalancerName' is too long. ALB names must be 32 characters or fewer."
}

if ($TargetGroupName.Length -gt 32) {
    throw "Target group name '$TargetGroupName' is too long. Target group names must be 32 characters or fewer."
}

$script:AwsBaseArgs = @("--region", $Region)
if (-not [string]::IsNullOrWhiteSpace($Profile)) {
    $script:AwsBaseArgs += @("--profile", $Profile)
}

function Write-Step {
    param([Parameter(Mandatory = $true)][string]$Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Test-HasAwsValue {
    param([string]$Value)
    return -not [string]::IsNullOrWhiteSpace($Value) -and $Value -ne "None" -and $Value -ne "null"
}

function Invoke-AwsCli {
    param(
        [Parameter(Mandatory = $true, ValueFromRemainingArguments = $true)]
        [string[]]$Arguments
    )

    $allArguments = @($script:AwsBaseArgs) + @($Arguments)
    $output = & aws @allArguments 2>&1
    $exitCode = $LASTEXITCODE
    $text = ($output | ForEach-Object { $_.ToString() }) -join [Environment]::NewLine

    if ($exitCode -ne 0) {
        throw "aws $($Arguments -join ' ') failed with exit code ${exitCode}: $text"
    }

    return $text.Trim()
}

function Try-Invoke-AwsCli {
    param(
        [Parameter(Mandatory = $true, ValueFromRemainingArguments = $true)]
        [string[]]$Arguments
    )

    $allArguments = @($script:AwsBaseArgs) + @($Arguments)
    $output = & aws @allArguments 2>&1
    $exitCode = $LASTEXITCODE
    $text = ($output | ForEach-Object { $_.ToString() }) -join [Environment]::NewLine

    if ($exitCode -ne 0) {
        return $null
    }

    return $text.Trim()
}

function Invoke-AwsCliAllowingDuplicateRule {
    param(
        [Parameter(Mandatory = $true, ValueFromRemainingArguments = $true)]
        [string[]]$Arguments
    )

    $allArguments = @($script:AwsBaseArgs) + @($Arguments)
    $output = & aws @allArguments 2>&1
    $exitCode = $LASTEXITCODE
    $text = ($output | ForEach-Object { $_.ToString() }) -join [Environment]::NewLine

    if ($exitCode -ne 0 -and $text -notmatch "InvalidPermission\.Duplicate") {
        throw "aws $($Arguments -join ' ') failed with exit code ${exitCode}: $text"
    }

    return $text.Trim()
}

function New-TempJsonFile {
    param(
        [Parameter(Mandatory = $true)]$Value,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $path = Join-Path ([System.IO.Path]::GetTempPath()) "$Name-$([guid]::NewGuid().ToString('N')).json"
    $Value | ConvertTo-Json -Depth 30 | Set-Content -Path $path -Encoding utf8
    return $path
}

function ConvertTo-AwsFileUri {
    param([Parameter(Mandatory = $true)][string]$Path)
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if ($fullPath -match "^[A-Za-z]:\\") {
        return "file:///$($fullPath -replace '\\', '/')"
    }

    return "file://$fullPath"
}

function ConvertFrom-SecureStringToPlainText {
    param([Parameter(Mandatory = $true)][SecureString]$Value)

    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Value)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

function New-RandomSecret {
    param([int]$ByteCount = 64)

    $bytes = New-Object byte[] $ByteCount
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $rng.GetBytes($bytes)
        return [Convert]::ToBase64String($bytes)
    }
    finally {
        $rng.Dispose()
    }
}

function Escape-ConnectionStringValue {
    param([Parameter(Mandatory = $true)][string]$Value)

    if ($Value -match "[;=`"']") {
        return '"' + ($Value -replace '"', '""') + '"'
    }

    return $Value
}

function ConvertTo-SsmParameterArn {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$AccountId
    )

    $resourceName = $Name.TrimStart("/")
    return "arn:aws:ssm:${Region}:${AccountId}:parameter/$resourceName"
}

function Ensure-CloudWatchLogGroup {
    param([Parameter(Mandatory = $true)][string]$Name)

    $existing = Try-Invoke-AwsCli logs describe-log-groups `
        --log-group-name-prefix $Name `
        --query "logGroups[?logGroupName=='$Name'].logGroupName | [0]" `
        --output text

    if (-not (Test-HasAwsValue $existing)) {
        Invoke-AwsCli logs create-log-group --log-group-name $Name | Out-Null
    }

    Invoke-AwsCli logs put-retention-policy --log-group-name $Name --retention-in-days $LogRetentionDays | Out-Null
}

function Ensure-EcrRepository {
    $repositoryUri = Try-Invoke-AwsCli ecr describe-repositories `
        --repository-names $RepositoryName `
        --query "repositories[0].repositoryUri" `
        --output text

    if (-not (Test-HasAwsValue $repositoryUri)) {
        $repositoryUri = Invoke-AwsCli ecr create-repository `
            --repository-name $RepositoryName `
            --image-scanning-configuration scanOnPush=true `
            --encryption-configuration encryptionType=AES256 `
            --query "repository.repositoryUri" `
            --output text
    }

    return $repositoryUri
}

function Ensure-IamRole {
    param(
        [Parameter(Mandatory = $true)][string]$RoleName,
        [Parameter(Mandatory = $true)][string]$ServicePrincipal
    )

    $roleArn = Try-Invoke-AwsCli iam get-role `
        --role-name $RoleName `
        --query "Role.Arn" `
        --output text

    if (Test-HasAwsValue $roleArn) {
        return $roleArn
    }

    $assumeRolePolicy = @{
        Version = "2012-10-17"
        Statement = @(
            @{
                Effect = "Allow"
                Principal = @{ Service = $ServicePrincipal }
                Action = "sts:AssumeRole"
            }
        )
    }

    $policyPath = New-TempJsonFile -Value $assumeRolePolicy -Name "$RoleName-assume-role"
    $roleArn = Invoke-AwsCli iam create-role `
        --role-name $RoleName `
        --assume-role-policy-document (ConvertTo-AwsFileUri $policyPath) `
        --query "Role.Arn" `
        --output text

    return $roleArn
}

function Ensure-SecurityGroup {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Description
    )

    $groupId = Try-Invoke-AwsCli ec2 describe-security-groups `
        --filters "Name=group-name,Values=$Name" "Name=vpc-id,Values=$VpcId" `
        --query "SecurityGroups[0].GroupId" `
        --output text

    if (Test-HasAwsValue $groupId) {
        return $groupId
    }

    return Invoke-AwsCli ec2 create-security-group `
        --group-name $Name `
        --description $Description `
        --vpc-id $VpcId `
        --query "GroupId" `
        --output text
}

function Add-SecurityGroupIngress {
    param(
        [Parameter(Mandatory = $true)][string]$GroupId,
        [Parameter(Mandatory = $true)]$IpPermissions,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $path = New-TempJsonFile -Value $IpPermissions -Name $Name
    Invoke-AwsCliAllowingDuplicateRule ec2 authorize-security-group-ingress `
        --group-id $GroupId `
        --ip-permissions (ConvertTo-AwsFileUri $path) | Out-Null
}

function Ensure-DbSubnetGroup {
    $existing = Try-Invoke-AwsCli rds describe-db-subnet-groups `
        --db-subnet-group-name $DbSubnetGroupName `
        --query "DBSubnetGroups[0].DBSubnetGroupName" `
        --output text

    if (Test-HasAwsValue $existing) {
        $modifyArgs = @(
            "rds", "modify-db-subnet-group",
            "--db-subnet-group-name", $DbSubnetGroupName,
            "--db-subnet-group-description", "Subnets for $NamePrefix SQL Server",
            "--subnet-ids"
        ) + $DbSubnetIds
        Invoke-AwsCli @modifyArgs | Out-Null
        return
    }

    $createArgs = @(
        "rds", "create-db-subnet-group",
        "--db-subnet-group-name", $DbSubnetGroupName,
        "--db-subnet-group-description", "Subnets for $NamePrefix SQL Server",
        "--subnet-ids"
    ) + $DbSubnetIds
    Invoke-AwsCli @createArgs | Out-Null
}

function Ensure-RdsInstance {
    param(
        [Parameter(Mandatory = $true)][string]$RdsSecurityGroupId,
        [Parameter(Mandatory = $true)][string]$PlainDbPassword
    )

    $status = Try-Invoke-AwsCli rds describe-db-instances `
        --db-instance-identifier $DbInstanceIdentifier `
        --query "DBInstances[0].DBInstanceStatus" `
        --output text

    if (-not (Test-HasAwsValue $status)) {
        $args = @(
            "rds", "create-db-instance",
            "--db-instance-identifier", $DbInstanceIdentifier,
            "--db-instance-class", $DbInstanceClass,
            "--engine", $DbEngine,
            "--master-username", $DbUsername,
            "--master-user-password", $PlainDbPassword,
            "--allocated-storage", $DbAllocatedStorageGb.ToString(),
            "--storage-type", $DbStorageType,
            "--license-model", "license-included",
            "--vpc-security-group-ids", $RdsSecurityGroupId,
            "--db-subnet-group-name", $DbSubnetGroupName,
            "--backup-retention-period", $DbBackupRetentionDays.ToString(),
            "--copy-tags-to-snapshot"
        )

        if (Test-HasAwsValue $DbEngineVersion) {
            $args += @("--engine-version", $DbEngineVersion)
        }

        if ($PubliclyAccessibleDb) {
            $args += "--publicly-accessible"
        }
        else {
            $args += "--no-publicly-accessible"
        }

        if ($DeletionProtection) {
            $args += "--deletion-protection"
        }
        else {
            $args += "--no-deletion-protection"
        }

        Invoke-AwsCli @args | Out-Null
    }

    Write-Host "Waiting for RDS instance '$DbInstanceIdentifier' to become available. This can take a while."
    Invoke-AwsCli rds wait db-instance-available --db-instance-identifier $DbInstanceIdentifier | Out-Null

    return Invoke-AwsCli rds describe-db-instances `
        --db-instance-identifier $DbInstanceIdentifier `
        --query "DBInstances[0].Endpoint.Address" `
        --output text
}

function Ensure-LoadBalancer {
    param([Parameter(Mandatory = $true)][string]$AlbSecurityGroupId)

    $loadBalancerArn = Try-Invoke-AwsCli elbv2 describe-load-balancers `
        --names $LoadBalancerName `
        --query "LoadBalancers[0].LoadBalancerArn" `
        --output text

    if (-not (Test-HasAwsValue $loadBalancerArn)) {
        $args = @(
            "elbv2", "create-load-balancer",
            "--name", $LoadBalancerName,
            "--subnets"
        ) + $PublicSubnetIds + @(
            "--security-groups", $AlbSecurityGroupId,
            "--scheme", "internet-facing",
            "--type", "application",
            "--ip-address-type", "ipv4",
            "--query", "LoadBalancers[0].LoadBalancerArn",
            "--output", "text"
        )

        $loadBalancerArn = Invoke-AwsCli @args
    }

    Invoke-AwsCli elbv2 wait load-balancer-available --load-balancer-arns $loadBalancerArn | Out-Null

    $dnsName = Invoke-AwsCli elbv2 describe-load-balancers `
        --load-balancer-arns $loadBalancerArn `
        --query "LoadBalancers[0].DNSName" `
        --output text

    return @{
        Arn = $loadBalancerArn
        DnsName = $dnsName
    }
}

function Ensure-TargetGroup {
    $targetGroupArn = Try-Invoke-AwsCli elbv2 describe-target-groups `
        --names $TargetGroupName `
        --query "TargetGroups[0].TargetGroupArn" `
        --output text

    if (Test-HasAwsValue $targetGroupArn) {
        return $targetGroupArn
    }

    return Invoke-AwsCli elbv2 create-target-group `
        --name $TargetGroupName `
        --protocol HTTP `
        --port $ContainerPort `
        --vpc-id $VpcId `
        --target-type ip `
        --health-check-enabled `
        --health-check-protocol HTTP `
        --health-check-path "/health" `
        --health-check-interval-seconds 30 `
        --health-check-timeout-seconds 5 `
        --healthy-threshold-count 2 `
        --unhealthy-threshold-count 3 `
        --matcher "HttpCode=200-399" `
        --query "TargetGroups[0].TargetGroupArn" `
        --output text
}

function Ensure-Listener {
    param(
        [Parameter(Mandatory = $true)][string]$LoadBalancerArn,
        [Parameter(Mandatory = $true)][string]$TargetGroupArn
    )

    $httpActions = if (Test-HasAwsValue $CertificateArn) {
        @(
            @{
                Type = "redirect"
                RedirectConfig = @{
                    Protocol = "HTTPS"
                    Port = "443"
                    StatusCode = "HTTP_301"
                }
            }
        )
    }
    else {
        @(
            @{
                Type = "forward"
                TargetGroupArn = $TargetGroupArn
            }
        )
    }

    $httpActionsPath = New-TempJsonFile -Value $httpActions -Name "alb-http-actions"
    $httpListenerArn = Try-Invoke-AwsCli elbv2 describe-listeners `
        --load-balancer-arn $LoadBalancerArn `
        --query 'Listeners[?Port==`80`].ListenerArn | [0]' `
        --output text

    if (Test-HasAwsValue $httpListenerArn) {
        Invoke-AwsCli elbv2 modify-listener `
            --listener-arn $httpListenerArn `
            --default-actions (ConvertTo-AwsFileUri $httpActionsPath) | Out-Null
    }
    else {
        Invoke-AwsCli elbv2 create-listener `
            --load-balancer-arn $LoadBalancerArn `
            --protocol HTTP `
            --port 80 `
            --default-actions (ConvertTo-AwsFileUri $httpActionsPath) | Out-Null
    }

    if (Test-HasAwsValue $CertificateArn) {
        $httpsActions = @(
            @{
                Type = "forward"
                TargetGroupArn = $TargetGroupArn
            }
        )
        $httpsActionsPath = New-TempJsonFile -Value $httpsActions -Name "alb-https-actions"
        $httpsListenerArn = Try-Invoke-AwsCli elbv2 describe-listeners `
            --load-balancer-arn $LoadBalancerArn `
            --query 'Listeners[?Port==`443`].ListenerArn | [0]' `
            --output text

        if (Test-HasAwsValue $httpsListenerArn) {
            Invoke-AwsCli elbv2 modify-listener `
                --listener-arn $httpsListenerArn `
                --certificates "CertificateArn=$CertificateArn" `
                --default-actions (ConvertTo-AwsFileUri $httpsActionsPath) | Out-Null
        }
        else {
            Invoke-AwsCli elbv2 create-listener `
                --load-balancer-arn $LoadBalancerArn `
                --protocol HTTPS `
                --port 443 `
                --certificates "CertificateArn=$CertificateArn" `
                --ssl-policy "ELBSecurityPolicy-TLS13-1-2-2021-06" `
                --default-actions (ConvertTo-AwsFileUri $httpsActionsPath) | Out-Null
        }
    }
}

function Ensure-EcsCluster {
    $clusterArn = Try-Invoke-AwsCli ecs describe-clusters `
        --clusters $ClusterName `
        --query "clusters[?status=='ACTIVE'].clusterArn | [0]" `
        --output text

    if (Test-HasAwsValue $clusterArn) {
        return $clusterArn
    }

    return Invoke-AwsCli ecs create-cluster `
        --cluster-name $ClusterName `
        --settings name=containerInsights,value=enabled `
        --query "cluster.clusterArn" `
        --output text
}

function Register-TaskDefinition {
    param(
        [Parameter(Mandatory = $true)][string]$ImageUri,
        [Parameter(Mandatory = $true)][string]$TaskRoleArn,
        [Parameter(Mandatory = $true)][string]$ExecutionRoleArn
    )

    $taskDefinition = @{
        family = $TaskFamily
        taskRoleArn = $TaskRoleArn
        executionRoleArn = $ExecutionRoleArn
        networkMode = "awsvpc"
        requiresCompatibilities = @("FARGATE")
        cpu = $TaskCpu
        memory = $TaskMemory
        runtimePlatform = @{
            cpuArchitecture = "X86_64"
            operatingSystemFamily = "LINUX"
        }
        containerDefinitions = @(
            @{
                name = $ContainerName
                image = $ImageUri
                essential = $true
                portMappings = @(
                    @{
                        name = "$ContainerName-$ContainerPort-tcp"
                        containerPort = $ContainerPort
                        hostPort = $ContainerPort
                        protocol = "tcp"
                        appProtocol = "http"
                    }
                )
                environment = @(
                    @{
                        name = "ASPNETCORE_ENVIRONMENT"
                        value = "Production"
                    },
                    @{
                        name = "AwsParameterStore__Region"
                        value = $Region
                    },
                    @{
                        name = "AwsParameterStore__JwtSigningKeyParameterName"
                        value = $JwtSigningKeyParameterName
                    },
                    @{
                        name = "AwsParameterStore__DefaultConnectionParameterName"
                        value = $DefaultConnectionParameterName
                    },
                    @{
                        name = "ProductionCloudWatchLogs__Region"
                        value = $Region
                    },
                    @{
                        name = "ProductionCloudWatchLogs__ApiLogGroup"
                        value = $ApiLogGroup
                    },
                    @{
                        name = "ProductionCloudWatchLogs__RequestLogGroup"
                        value = $RequestLogGroup
                    },
                    @{
                        name = "ProductionCloudWatchLogs__NewLogGroupRetentionInDays"
                        value = $LogRetentionDays.ToString()
                    }
                )
                logConfiguration = @{
                    logDriver = "awslogs"
                    options = @{
                        "awslogs-group" = $EcsLogGroup
                        "awslogs-create-group" = "true"
                        "awslogs-region" = $Region
                        "awslogs-stream-prefix" = "ecs"
                    }
                }
            }
        )
    }

    $path = New-TempJsonFile -Value $taskDefinition -Name "ecs-task-definition"

    return Invoke-AwsCli ecs register-task-definition `
        --cli-input-json (ConvertTo-AwsFileUri $path) `
        --query "taskDefinition.taskDefinitionArn" `
        --output text
}

function Ensure-EcsService {
    param(
        [Parameter(Mandatory = $true)][string]$TaskDefinitionArn,
        [Parameter(Mandatory = $true)][string]$TargetGroupArn,
        [Parameter(Mandatory = $true)][string]$EcsSecurityGroupId
    )

    $networkConfiguration = @{
        awsvpcConfiguration = @{
            subnets = $EcsSubnetIds
            securityGroups = @($EcsSecurityGroupId)
            assignPublicIp = $AssignPublicIp
        }
    }
    $networkConfigurationPath = New-TempJsonFile -Value $networkConfiguration -Name "ecs-network-configuration"
    $loadBalancers = @(
        @{
            targetGroupArn = $TargetGroupArn
            containerName = $ContainerName
            containerPort = $ContainerPort
        }
    )
    $loadBalancersPath = New-TempJsonFile -Value $loadBalancers -Name "ecs-load-balancers"

    $status = Try-Invoke-AwsCli ecs describe-services `
        --cluster $ClusterName `
        --services $ServiceName `
        --query "services[0].status" `
        --output text

    if ($status -eq "ACTIVE") {
        Invoke-AwsCli ecs update-service `
            --cluster $ClusterName `
            --service $ServiceName `
            --task-definition $TaskDefinitionArn `
            --desired-count $DesiredCount `
            --network-configuration (ConvertTo-AwsFileUri $networkConfigurationPath) `
            --load-balancers (ConvertTo-AwsFileUri $loadBalancersPath) | Out-Null
        return
    }

    Invoke-AwsCli ecs create-service `
        --cluster $ClusterName `
        --service-name $ServiceName `
        --task-definition $TaskDefinitionArn `
        --desired-count $DesiredCount `
        --launch-type FARGATE `
        --platform-version LATEST `
        --network-configuration (ConvertTo-AwsFileUri $networkConfigurationPath) `
        --load-balancers (ConvertTo-AwsFileUri $loadBalancersPath) `
        --health-check-grace-period-seconds $HealthCheckGracePeriodSeconds | Out-Null
}

function Publish-Image {
    param([Parameter(Mandatory = $true)][string]$RepositoryUri)

    $dockerfilePath = Join-Path $ProjectRoot "Dockerfile"
    if (-not (Test-Path $dockerfilePath)) {
        throw "Dockerfile was not found at '$dockerfilePath'."
    }

    $registry = $RepositoryUri.Split("/")[0]
    $imageUri = "${RepositoryUri}:${ImageTag}"
    $localImage = "${RepositoryName}:${ImageTag}"

    Write-Step "Building and pushing Docker image"
    $password = Invoke-AwsCli ecr get-login-password
    $password | docker login --username AWS --password-stdin $registry | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "docker login failed."
    }

    & docker build -t $localImage -f $dockerfilePath $ProjectRoot
    if ($LASTEXITCODE -ne 0) {
        throw "docker build failed."
    }

    & docker tag $localImage $imageUri
    if ($LASTEXITCODE -ne 0) {
        throw "docker tag failed."
    }

    & docker push $imageUri
    if ($LASTEXITCODE -ne 0) {
        throw "docker push failed."
    }

    return $imageUri
}

Write-Step "Checking AWS caller identity"
$accountId = Invoke-AwsCli sts get-caller-identity --query "Account" --output text
$plainDbPassword = ConvertFrom-SecureStringToPlainText $DbPassword
$plainJwtSigningKey = if ($JwtSigningKey) {
    ConvertFrom-SecureStringToPlainText $JwtSigningKey
}
else {
    New-RandomSecret
}

Write-Step "Creating ECR repository"
$repositoryUri = Ensure-EcrRepository
$imageUri = "${repositoryUri}:${ImageTag}"
if ($BuildAndPushImage) {
    $imageUri = Publish-Image -RepositoryUri $repositoryUri
}

Write-Step "Creating IAM roles and policies"
$taskRoleArn = Ensure-IamRole -RoleName $TaskRoleName -ServicePrincipal "ecs-tasks.amazonaws.com"
$executionRoleArn = Ensure-IamRole -RoleName $ExecutionRoleName -ServicePrincipal "ecs-tasks.amazonaws.com"

Invoke-AwsCli iam attach-role-policy `
    --role-name $ExecutionRoleName `
    --policy-arn "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy" | Out-Null

$parameterResources = @(
    (ConvertTo-SsmParameterArn -Name $JwtSigningKeyParameterName -AccountId $accountId)
    (ConvertTo-SsmParameterArn -Name $DefaultConnectionParameterName -AccountId $accountId)
)

$kmsResource = if ((Test-HasAwsValue $KmsKeyId) -and $KmsKeyId -match "^arn:aws[a-zA-Z-]*:kms:") { $KmsKeyId } else { "*" }
$taskRolePolicy = @{
    Version = "2012-10-17"
    Statement = @(
        @{
            Effect = "Allow"
            Action = @("ssm:GetParameter", "ssm:GetParameters")
            Resource = $parameterResources
        },
        @{
            Effect = "Allow"
            Action = @("kms:Decrypt")
            Resource = @($kmsResource)
            Condition = @{
                StringEquals = @{
                    "kms:ViaService" = "ssm.$Region.amazonaws.com"
                }
            }
        },
        @{
            Effect = "Allow"
            Action = @(
                "logs:CreateLogGroup",
                "logs:CreateLogStream",
                "logs:DescribeLogGroups",
                "logs:DescribeLogStreams",
                "logs:PutLogEvents"
            )
            Resource = @(
                "arn:aws:logs:${Region}:${accountId}:log-group:${ApiLogGroup}*",
                "arn:aws:logs:${Region}:${accountId}:log-group:${RequestLogGroup}*"
            )
        }
    )
}

$taskRolePolicyPath = New-TempJsonFile -Value $taskRolePolicy -Name "ecs-task-role-policy"
Invoke-AwsCli iam put-role-policy `
    --role-name $TaskRoleName `
    --policy-name "${NamePrefix}-task-parameter-store-and-logs" `
    --policy-document (ConvertTo-AwsFileUri $taskRolePolicyPath) | Out-Null

Write-Host "Waiting briefly for IAM role propagation."
Start-Sleep -Seconds 10

Write-Step "Creating CloudWatch log groups"
Ensure-CloudWatchLogGroup -Name $EcsLogGroup
Ensure-CloudWatchLogGroup -Name $ApiLogGroup
Ensure-CloudWatchLogGroup -Name $RequestLogGroup

Write-Step "Creating security groups"
$albSecurityGroupId = Ensure-SecurityGroup -Name $AlbSecurityGroupName -Description "ALB ingress for $NamePrefix"
$ecsSecurityGroupId = Ensure-SecurityGroup -Name $EcsSecurityGroupName -Description "ECS tasks for $NamePrefix"
$rdsSecurityGroupId = Ensure-SecurityGroup -Name $RdsSecurityGroupName -Description "RDS SQL Server for $NamePrefix"

$albIngress = @(
    @{
        IpProtocol = "tcp"
        FromPort = 80
        ToPort = 80
        IpRanges = @(
            @{
                CidrIp = $AllowedHttpCidr
                Description = "HTTP to ALB"
            }
        )
    }
)

if (Test-HasAwsValue $CertificateArn) {
    $albIngress += @{
        IpProtocol = "tcp"
        FromPort = 443
        ToPort = 443
        IpRanges = @(
            @{
                CidrIp = $AllowedHttpCidr
                Description = "HTTPS to ALB"
            }
        )
    }
}

Add-SecurityGroupIngress -GroupId $albSecurityGroupId -IpPermissions $albIngress -Name "alb-ingress"

$ecsIngress = @(
    @{
        IpProtocol = "tcp"
        FromPort = $ContainerPort
        ToPort = $ContainerPort
        UserIdGroupPairs = @(
            @{
                GroupId = $albSecurityGroupId
                Description = "ALB to ECS"
            }
        )
    }
)
Add-SecurityGroupIngress -GroupId $ecsSecurityGroupId -IpPermissions $ecsIngress -Name "ecs-ingress"

$rdsIngress = @(
    @{
        IpProtocol = "tcp"
        FromPort = 1433
        ToPort = 1433
        UserIdGroupPairs = @(
            @{
                GroupId = $ecsSecurityGroupId
                Description = "ECS to SQL Server"
            }
        )
    }
)

if (Test-HasAwsValue $AdminCidrToDb) {
    $rdsIngress += @{
        IpProtocol = "tcp"
        FromPort = 1433
        ToPort = 1433
        IpRanges = @(
            @{
                CidrIp = $AdminCidrToDb
                Description = "Temporary admin SQL access"
            }
        )
    }
}

Add-SecurityGroupIngress -GroupId $rdsSecurityGroupId -IpPermissions $rdsIngress -Name "rds-ingress"

Write-Step "Creating RDS SQL Server"
Ensure-DbSubnetGroup
$dbEndpoint = Ensure-RdsInstance -RdsSecurityGroupId $rdsSecurityGroupId -PlainDbPassword $plainDbPassword

Write-Step "Writing Parameter Store secrets"
$escapedUser = Escape-ConnectionStringValue $DbUsername
$escapedPassword = Escape-ConnectionStringValue $plainDbPassword
$connectionString = "Server=$dbEndpoint,1433;Database=$DatabaseName;User Id=$escapedUser;Password=$escapedPassword;Encrypt=True;TrustServerCertificate=True;Connection Timeout=15;"

$ssmJwtArgs = @(
    "ssm", "put-parameter",
    "--name", $JwtSigningKeyParameterName,
    "--type", "SecureString",
    "--value", $plainJwtSigningKey,
    "--overwrite"
)
$ssmConnectionArgs = @(
    "ssm", "put-parameter",
    "--name", $DefaultConnectionParameterName,
    "--type", "SecureString",
    "--value", $connectionString,
    "--overwrite"
)
if (Test-HasAwsValue $KmsKeyId) {
    $ssmJwtArgs += @("--key-id", $KmsKeyId)
    $ssmConnectionArgs += @("--key-id", $KmsKeyId)
}
Invoke-AwsCli @ssmJwtArgs | Out-Null
Invoke-AwsCli @ssmConnectionArgs | Out-Null

Write-Step "Creating ALB and target group"
$loadBalancer = Ensure-LoadBalancer -AlbSecurityGroupId $albSecurityGroupId
$targetGroupArn = Ensure-TargetGroup
Ensure-Listener -LoadBalancerArn $loadBalancer.Arn -TargetGroupArn $targetGroupArn

Write-Step "Creating ECS cluster, task definition, and service"
$clusterArn = Ensure-EcsCluster
$taskDefinitionArn = Register-TaskDefinition -ImageUri $imageUri -TaskRoleArn $taskRoleArn -ExecutionRoleArn $executionRoleArn
Ensure-EcsService -TaskDefinitionArn $taskDefinitionArn -TargetGroupArn $targetGroupArn -EcsSecurityGroupId $ecsSecurityGroupId

$scheme = if (Test-HasAwsValue $CertificateArn) { "https" } else { "http" }
$summary = [ordered]@{
    AccountId = $accountId
    Region = $Region
    EcrRepositoryUri = $repositoryUri
    ImageUri = $imageUri
    ClusterArn = $clusterArn
    ServiceName = $ServiceName
    TaskDefinitionArn = $taskDefinitionArn
    LoadBalancerDnsName = $loadBalancer.DnsName
    ApiBaseUrl = "${scheme}://$($loadBalancer.DnsName)"
    RdsEndpoint = $dbEndpoint
    JwtSigningKeyParameterName = $JwtSigningKeyParameterName
    DefaultConnectionParameterName = $DefaultConnectionParameterName
}

Write-Step "Provisioning complete"
$summary | ConvertTo-Json -Depth 5

Write-Host ""
Write-Host "Remember to apply EF Core migrations against the RDS connection string before using package endpoints." -ForegroundColor Yellow
