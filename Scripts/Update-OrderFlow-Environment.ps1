
$ErrorActionPreference = "Stop"

Write-Host "============================================"
Write-Host " OrderFlow Environment Variables Setup"
Write-Host "============================================"

# Unique environment variables across all microservices
$envVars = [ordered]@{
    # Database connections
    "ConnectionStrings__CartDb"      = "Data Source=DESKTOP-AE7G9LP;Initial Catalog=CartDb;Integrated Security=True;TrustServerCertificate=True;"
    "ConnectionStrings__CatalogDb"   = "Data Source=DESKTOP-AE7G9LP;Initial Catalog=ProductCategory;Integrated Security=True;TrustServerCertificate=True;"
    "ConnectionStrings__CategoryDb"  = "Data Source=DESKTOP-AE7G9LP;Initial Catalog=CategoryDb;Integrated Security=True;TrustServerCertificate=True;"
    "ConnectionStrings__InventoryDb" = "Data Source=DESKTOP-AE7G9LP;Initial Catalog=InventoryDb;Integrated Security=True;TrustServerCertificate=True;"
    "ConnectionStrings__OrderDb"     = "Data Source=DESKTOP-AE7G9LP;Initial Catalog=OrderDb;Integrated Security=True;TrustServerCertificate=True;"
    "ConnectionStrings__PaymentDb"   = "Data Source=DESKTOP-AE7G9LP;Initial Catalog=PaymentDb;Integrated Security=True;TrustServerCertificate=True;"
    "ConnectionStrings__UserDb"      = "Data Source=DESKTOP-AE7G9LP;Initial Catalog=UserDb;Integrated Security=True;TrustServerCertificate=True;"

    # Service URLs
    "Services__CatalogService__BaseUrl"  = "https://localhost:7152/"
    "Services__CategoryService__BaseUrl" = "https://localhost:7184/"
    "InventoryService__Url"              = "https://localhost:7163/"
    "CartService__BaseUrl"                = "https://localhost:7191/"
    "CatalogService__BaseUrl"             = "https://localhost:7152/"

    # Service Bus
    "ServiceBus__QueueName"           = "orders"
    "ServiceBus__OrdersQueue"         = "orders"
    "ServiceBus__PaymentResultsQueue" = "payment-results"

    # Both configuration key spellings are preserved
    "ServiceBus__ConnectionStrings" = "PASTE_YOUR_ROTATED_SERVICE_BUS_CONNECTION_STRING_HERE"
    "ServiceBus__ConnectionString"  = "PASTE_YOUR_ROTATED_SERVICE_BUS_CONNECTION_STRING_HERE"

    # JWT
    "Jwt__Issuer"             = "OrderFlow.UserService"
    "Jwt__Audience"           = "OrderFlow"
    "Jwt__AccessTokenMinutes" = "15"
    "Jwt__SigningKey"         = "REPLACE_WITH_A_STRONG_SECRET_KEY"
}

# Step 1: Delete existing values for all variables in this script
Write-Host "`n[1/3] Removing existing variables..."

foreach ($name in $envVars.Keys) {
    [Environment]::SetEnvironmentVariable($name, $null, "User")
    Write-Host "Removed: $name"
}

# Step 2: Create updated variables
Write-Host "`n[2/3] Creating variables..."

foreach ($entry in $envVars.GetEnumerator()) {
    [Environment]::SetEnvironmentVariable(
        $entry.Key,
        $entry.Value,
        "User"
    )

    Write-Host "Created: $($entry.Key)"
}

# Step 3: Verify that every variable was saved
Write-Host "`n[3/3] Verifying variables..."

$failed = 0

foreach ($name in $envVars.Keys) {
    $value = [Environment]::GetEnvironmentVariable($name, "User")

    if ($null -ne $value) {
        Write-Host "[OK] $name"
    }
    else {
        Write-Host "[ERROR] $name"
        $failed++
    }
}

Write-Host "`n============================================"

if ($failed -eq 0) {
    Write-Host "All $($envVars.Count) variables saved successfully."
}
else {
    Write-Host "$failed variable(s) failed verification."
}

Write-Host "Restart Visual Studio or your terminal."
Write-Host "============================================"

Read-Host "Press Enter to close"
