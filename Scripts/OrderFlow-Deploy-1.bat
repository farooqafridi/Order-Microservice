@echo off
setlocal EnableExtensions

title OrderFlow Microservices Deployment

echo ============================================================
echo          OrderFlow Microservices Deployment
echo ============================================================

REM ============================================================
REM Configuration
REM ============================================================

set "ROOT=D:\Concepts\Micro Services\OrderManagementSystem"
set "PUBLISHED=D:\Concepts\Micro Services\published"
set "ENV_FILE=%ROOT%\OrderFlow.env"
set "NETWORK=orderflow-network"

REM ============================================================
REM 1. Validate prerequisites
REM ============================================================

echo.
echo [1/6] Validating prerequisites...

where docker >nul 2>&1
if errorlevel 1 (
    echo ERROR: Docker CLI was not found.
    goto :error
)

docker info >nul 2>&1
if errorlevel 1 (
    echo ERROR: Docker is not running. Start Docker Desktop.
    goto :error
)

if not exist "%ENV_FILE%" (
    echo ERROR: Environment file not found:
    echo "%ENV_FILE%"
    goto :error
)

for %%S in (UserService CategoryService CatalogService CartService OrderService InventoryService PaymentService) do (
    if not exist "%ROOT%\%%S\Dockerfile" (
        echo ERROR: Dockerfile not found:
        echo "%ROOT%\%%S\Dockerfile"
        goto :error
    )
    if not exist "%PUBLISHED%\%%S" (
        echo ERROR: Published directory not found:
        echo "%PUBLISHED%\%%S"
        goto :error
    )
)

REM ============================================================
REM 2. Create shared Docker network
REM ============================================================

echo.
echo [2/6] Checking Docker network...

docker network inspect "%NETWORK%" >nul 2>&1
if errorlevel 1 (
    docker network create "%NETWORK%"
    if errorlevel 1 goto :error
) else (
    echo Network "%NETWORK%" already exists.
)

REM ============================================================
REM 3. Remove existing containers
REM ============================================================

echo.
echo [3/6] Removing existing containers...

docker rm -f userservice categoryservice catalogservice cartservice orderservice inventoryservice paymentservice >nul 2>&1

REM ============================================================
REM 4. Build all images
REM ============================================================

echo.
echo [4/6] Building microservices...

call :BuildImage userservice UserService
if errorlevel 1 goto :error

call :BuildImage categoryservice CategoryService
if errorlevel 1 goto :error

call :BuildImage catalogservice CatalogService
if errorlevel 1 goto :error

call :BuildImage cartservice CartService
if errorlevel 1 goto :error

call :BuildImage orderservice OrderService
if errorlevel 1 goto :error

call :BuildImage inventoryservice InventoryService
if errorlevel 1 goto :error

call :BuildImage paymentservice PaymentService
if errorlevel 1 goto :error

REM ============================================================
REM 5. Start all containers on shared network
REM ============================================================

echo.
echo [5/6] Starting microservices...

echo Starting UserService...
docker run -d --name userservice --network "%NETWORK%" -p 7101:8080 --env-file "%ENV_FILE%" userservice:latest
if errorlevel 1 goto :error

echo Starting CategoryService...
docker run -d --name categoryservice --network "%NETWORK%" -p 7102:8080 --env-file "%ENV_FILE%" categoryservice:latest
if errorlevel 1 goto :error

echo Starting CatalogService...
docker run -d --name catalogservice --network "%NETWORK%" -p 7103:8080 --env-file "%ENV_FILE%" ^
  -e "Services__CategoryService__BaseUrl=http://categoryservice:8080/" ^
  -e "Services__CatalogService__BaseUrl=http://catalogservice:8080/" ^
  -e "CategoryService__BaseUrl=http://categoryservice:8080/" ^
  catalogservice:latest
if errorlevel 1 goto :error

echo Starting CartService...
docker run -d --name cartservice --network "%NETWORK%" -p 7104:8080 --env-file "%ENV_FILE%" ^
  -e "Services__CatalogService__BaseUrl=http://catalogservice:8080/" ^
  cartservice:latest
if errorlevel 1 goto :error

echo Starting InventoryService...
docker run -d --name inventoryservice --network "%NETWORK%" -p 7106:8080 --env-file "%ENV_FILE%" inventoryservice:latest
if errorlevel 1 goto :error

echo Starting OrderService...
docker run -d --name orderservice --network "%NETWORK%" -p 7105:8080 --env-file "%ENV_FILE%" ^
  -e "Services__CatalogService__BaseUrl=http://catalogservice:8080/" ^
  -e "Services__CategoryService__BaseUrl=http://categoryservice:8080/" ^
  -e "InventoryService__Url=http://inventoryservice:8080/" ^
  -e "CartService__BaseUrl=http://cartservice:8080/" ^
  -e "CatalogService__BaseUrl=http://catalogservice:8080/" ^
  orderservice:latest
if errorlevel 1 goto :error

echo Starting PaymentService...
REM Worker Service: no HTTP port mapping required.
docker run -d --name paymentservice --network "%NETWORK%" --env-file "%ENV_FILE%" paymentservice:latest
if errorlevel 1 goto :error

REM ============================================================
REM 6. Show status and diagnostics
REM ============================================================

echo.
echo [6/6] Waiting for containers to initialize...
timeout /t 5 /nobreak >nul

echo.
echo ============================================================
echo                 CONTAINER STATUS
echo ============================================================

docker ps -a --filter "name=userservice" --filter "name=categoryservice" --filter "name=catalogservice" --filter "name=cartservice" --filter "name=orderservice" --filter "name=inventoryservice" --filter "name=paymentservice" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

echo.
echo ============================================================
echo                 DOCKER NETWORK
echo ============================================================

docker network inspect "%NETWORK%" --format "{{range .Containers}}{{println .Name}}{{end}}"

echo.
echo ============================================================
echo                 POSTMAN BASE URLS
echo ============================================================

echo UserService      : http://localhost:7101
echo CategoryService  : http://localhost:7102
echo CatalogService   : http://localhost:7103
echo CartService      : http://localhost:7104
echo OrderService     : http://localhost:7105
echo InventoryService : http://localhost:7106
echo PaymentService   : Background worker - no HTTP port

echo.
echo Internal service URLs use Docker DNS and port 8080.
echo Example: http://catalogservice:8080

echo.
echo Diagnostic commands:
echo   docker logs --tail 100 cartservice
echo   docker logs --tail 100 catalogservice
echo   docker network inspect %NETWORK%

echo.
echo Deployment commands completed.
echo Check the container status above for startup failures.
echo ============================================================

pause
exit /b 0

REM ============================================================
REM Helper: Build one service image
REM ============================================================

:BuildImage
echo.
echo Building %~2...
docker build -t "%~1:latest" -f "%ROOT%\%~2\Dockerfile" "%PUBLISHED%\%~2"
if errorlevel 1 (
    echo ERROR: Failed to build image %~1:latest
    exit /b 1
)
exit /b 0

:error
echo.
echo ============================================================
echo ERROR: Deployment failed.
echo Review the error shown above.
echo.
echo Useful diagnostic commands:
echo   docker ps -a
echo   docker logs cartservice
echo   docker logs catalogservice
echo   docker network inspect %NETWORK%
echo ============================================================
pause
exit /b 1