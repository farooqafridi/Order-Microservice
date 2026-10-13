
@echo off
echo ==========================================
echo Deleting OrderFlow Containers By Image
echo ==========================================

for %%I in (
    userservice:latest
    categoryservice:latest
    catalogservice:latest
    cartservice:latest
    orderservice:latest
    inventoryservice:latest
    paymentservice:latest
) do (
    echo.
    echo Removing containers created from %%I...

    for /f "tokens=*" %%C in ('docker ps -aq --filter "ancestor=%%I"') do (
        docker rm -f %%C
    )
)

echo.
echo ==========================================
echo Remaining Containers
echo ==========================================
docker ps -a

echo.
echo Cleanup completed.
pause
