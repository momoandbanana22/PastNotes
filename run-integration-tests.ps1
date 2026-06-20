# 環境変数を設定して統合テストを実行するスクリプト
# 使用方法: .\run-integration-tests.ps1 -InstanceUrl "https://misskey.io" -ApiToken "your-token"

param(
    [Parameter(Mandatory=$true)]
    [string]$InstanceUrl,
    
    [Parameter(Mandatory=$true)]
    [string]$ApiToken
)

$env:MISSKEY_INSTANCE_URL = $InstanceUrl
$env:MISSKEY_API_TOKEN = $ApiToken
dotnet test --filter "FullyQualifiedName~IntegrationTest"
