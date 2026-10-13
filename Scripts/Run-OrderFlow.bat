```bat
@echo off
setlocal

echo ============================================
echo       OrderFlow Microservices Deployment
echo ============================================

set "ROOT=D:\Concepts\Micro Services\OrderManagementSystem"
set "PUBLISHED=D:\Concepts\Micro Services\published"
set "ENV_FILE=%ROOT%\OrderFlow.env"
set "NETWORK=orderflow-network"

REM ============================================
REM 1. Validate required files
REM ============================================

if not exist "%ENV_FILE%" (
    echo ERROR: Environment file not found:
    echo %ENV_FILE%
    goto :error
)

REM ============================================
REM 2. Create shared Docker network
REM ============================================

echo.
echo [1/4] Creating Docker network...

docker network inspect %NETWORK% >nul 2>&1
if errorlevel 1 (
    docker network create %NETWORK%
    if errorlevel 1 goto :error
) else (
    echo Network %NETWORK% already exists.
)

REM ============================================
REM 3. Remove existing containers
REM ============================================

echo.
echo [2/4] Removing existing containers...

docker rm -f userservice categoryservice catalogservice cartservice orderservice inventoryservice paymentservice 2>nul

REM ============================================
REM 4. Build all microservices
REM ============================================

echo.
echo [3/4] Building microservices...

echo.
echo Building UserService...
docker build -t userservice:latest -f "%ROOT%\UserService\Dockerfile" "%PUBLISHED%\UserService"
if errorlevel 1 goto :error

echo.
echo Building CategoryService...
docker build -t categoryservice:latest -f "%ROOT%\CategoryService\Dockerfile" "%PUBLISHED%\CategoryService"
if errorlevel 1 goto :error

echo.
echo Building CatalogService...
docker build -t catalogservice:latest -f "%ROOT%\CatalogService\Dockerfile" "%PUBLISHED%\CatalogService"
if errorlevel 1 goto :error

echo.
echo Building CartService...
docker build -t cartservice:latest -f "%ROOT%\CartService\Dockerfile" "%PUBLISHED%\CartService"
if errorlevel 1 goto :error

echo.
echo Building OrderService...
docker build -t orderservice:latest -f "%ROOT%\OrderService\Dockerfile" "%PUBLISHED%\OrderService"
if errorlevel 1 goto :error

echo.
echo Building InventoryService...
docker build -t inventoryservice:latest -f "%ROOT%\InventoryService\Dockerfile" "%PUBLISHED%\InventoryService"
if errorlevel 1 goto :error

echo.
echo Building PaymentService...
docker build -t paymentservice:latest -f "%ROOT%\PaymentService\Dockerfile" "%PUBLISHED%\PaymentService"
if errorlevel 1 goto :error

REM ============================================
REM 5. Start all services on shared network
REM ============================================

echo.
echo [4/4] Starting microservices...

echo.
echo Starting UserService...
docker run -d --name userservice --network %NETWORK% -p 7101:8080 --env-file "%ENV_FILE%" userservice:latest
if errorlevel 1 goto :error

echo.
echo Starting CategoryService...
docker run -d --name categoryservice --network %NETWORK% -p 7102:8080 --env-file "%ENV_FILE%" categoryservice:latest
if errorlevel 1 goto :error

echo.
echo Starting CatalogService...
docker run -d --name catalogservice --network %NETWORK% -p 7103:8080 --env-file "%ENV_FILE%" -e "Services__CategoryService__BaseUrl=http://categoryservice:8080/" -e "Services__CatalogService__BaseUrl=http://catalogservice:8080/" -e "CategoryService__BaseUrl=http://categoryservice:8080/" catalogservice:latest
if errorlevel 1 goto :error

echo.
echo Starting CartService...
docker run -d --name cartservice --network %NETWORK% -p 7104:8080 --env-file "%ENV_FILE%" -e "CatalogService__BaseUrl=http://catalogservice:8080/" cartservice:latest
if errorlevel 1 goto :error

echo.
echo Starting InventoryService...
docker run -d --name inventoryservice --network %NETWORK% -p 7106:8080 --env-file "%ENV_FILE%" inventoryservice:latest
if errorlevel 1 goto :error

echo.
echo Starting PaymentService...
REM Worker Service: no HTTP port mapping required
docker run -d --name paymentservice --network %NETWORK% --env-file "%ENV_FILE%" paymentservice:latest
if errorlevel 1 goto :error

echo.
echo Starting OrderService...
docker run -d --name orderservice --network %NETWORK% -p 7105:8080 --env-file "%ENV_FILE%" -e "Services__CatalogService__BaseUrl=http://catalogservice:8080/" -e "Services__CategoryService__BaseUrl=http://categoryservice:8080/" -e "InventoryService__Url=http://inventoryservice:8080/" -e "CartService__BaseUrl=http://cartservice:8080/" -e "CatalogService__BaseUrl=http://catalogservice:8080/" orderservice:latest
if errorlevel 1 goto :error

REM ============================================
REM 6. Show status
REM ============================================

echo.
echo ============================================
echo       OrderFlow Startup Complete
echo ============================================

docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

echo.
echo Internal Docker network: %NETWORK%
echo.
echo Host ports:
echo UserService      http://localhost:7101
echo CategoryService  http://localhost:7102
echo CatalogService   http://localhost:7103
echo CartService      http://localhost:7104
echo OrderService     http://localhost:7105
echo InventoryService http://localhost:7106
echo PaymentService   Worker - no HTTP port
echo.
echo To view logs: docker logs userservice
echo To inspect network: docker network inspect %NETWORK%
echo ============================================

pause
exit /b 0

:error
echo.
echo ERROR: A deployment step failed.
echo Check the Docker output above.
echo Inspect logs with: docker logs CONTAINER_NAME
pause
exit /b 1
