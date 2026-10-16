#requires -Version 7.2
[CmdletBinding()]
param(
    [ValidateSet('retry','payload','isolation','new-order')][string]$Scenario = 'retry',
    [ValidateRange(1024,65535)][int]$Port = 5100,
    [string]$Customer = "demo-$([guid]::NewGuid().ToString('N'))"
)
. (Join-Path $PSScriptRoot 'Common.ps1')
$base = "http://127.0.0.1:$Port"
$key = [guid]::NewGuid().ToString('N')
$headers = @{ 'X-Customer-Id' = $Customer; 'Idempotency-Key' = $key }
$body = @{ sku = 'DEMO-KEYBOARD'; quantity = 1 } | ConvertTo-Json -Compress
function Send-Order {
    param([hashtable]$RequestHeaders, [string]$Json)
    $response = Invoke-WebRequest "$base/api/orders" -Method Post -Headers $RequestHeaders `
        -ContentType 'application/json' -Body $Json -SkipHttpErrorCheck
    $content = if ($response.Content -is [byte[]]) {
        [System.Text.Encoding]::UTF8.GetString($response.Content)
    } else { $response.Content }
    [pscustomobject]@{ Status = [int]$response.StatusCode; Body = $content | ConvertFrom-Json }
}
$first = Send-Order $headers $body
if ($first.Status -ne 201) { throw "Initial create expected 201, got $($first.Status): $($first.Body | ConvertTo-Json -Compress)" }
switch ($Scenario) {
    'retry' {
        $second = Send-Order $headers $body
        if ($second.Status -ne 200 -or $first.Body.id -ne $second.Body.id) {
            throw "Retry invariant failed. Initial ID=$($first.Body.id); retry status=$($second.Status), ID=$($second.Body.id)"
        }
        $orders = @(Invoke-RestMethod "$base/api/orders" -Headers $headers)
        if ($orders.Count -ne 1) { throw "Expected one order for fresh customer; found $($orders.Count)." }
    }
    'payload' {
        $second = Send-Order $headers (@{ sku='DEMO-KEYBOARD'; quantity=2 } | ConvertTo-Json -Compress)
        $errorCode = $second.Body.PSObject.Properties['code']
        if ($second.Status -ne 409 -or $null -eq $errorCode -or $errorCode.Value -ne 'idempotency_conflict') {
            throw "Payload conflict expected 409/idempotency_conflict; got $($second.Status): $($second.Body | ConvertTo-Json -Compress)."
        }
    }
    'isolation' {
        $otherHeaders = @{ 'X-Customer-Id' = "$Customer-other"; 'Idempotency-Key' = $key }
        $second = Send-Order $otherHeaders $body
        if ($second.Status -ne 201 -or $second.Body.id -eq $first.Body.id) { throw 'Customer isolation failed.' }
        $crossRead = Invoke-WebRequest "$base/api/orders/$($first.Body.id)" -Headers $otherHeaders -SkipHttpErrorCheck
        if ([int]$crossRead.StatusCode -ne 404) { throw "Cross-customer read expected 404; got $($crossRead.StatusCode)." }
    }
    'new-order' {
        $headers['Idempotency-Key'] = [guid]::NewGuid().ToString('N')
        $second = Send-Order $headers $body
        if ($second.Status -ne 201 -or $second.Body.id -eq $first.Body.id) { throw 'New intent did not create a new order.' }
    }
}
[pscustomobject]@{ Scenario=$Scenario; Result='PASS'; Customer=$Customer; FirstStatus=$first.Status; SecondStatus=$second.Status; OrderId=$first.Body.id }
