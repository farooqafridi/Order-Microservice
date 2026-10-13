@echo off
setlocal

echo ========================================
echo Creating OrderFlow Containers
echo ========================================

REM Set SQL Server credentials
set "DB_USER=farooq"
set "DB_PASSWORD=Agnip@t1983"
set "DB_SERVER=host.docker.internal"

REM Remove existing containers
docker rm -f userservice categoryservice catalogservice cartservice orderservice inventoryservice paymentservice 2>nul

echo.
echo Starting UserService...
docker run -d --name userservice -p 7101:8080 ^
  -e ASPNETCORE_HTTP_PORTS=8080 ^
  -e "ConnectionStrings__UserDb=Server=%DB_SERVER%;Database=UserDb;User Id=%DB_USER%;Password=%DB_PASSWORD%;TrustServerCertificate=True" ^
  userservice:latest
if errorlevel 1 echo ERROR: UserService container failed.

echo.
echo Starting CategoryService...
docker run -d --name categoryservice -p 7102:8080 ^
  -e ASPNETCORE_HTTP_PORTS=8080 ^
  -e "ConnectionStrings__CategoryDb=Server=%DB_SERVER%;Database=CategoryDb;User Id=%DB_USER%;Password=%DB_PASSWORD%;TrustServerCertificate=True" ^
  categoryservice:latest
if errorlevel 1 echo ERROR: CategoryService container failed.

echo.
echo Starting CatalogService...
docker run -d --name catalogservice -p 7103:8080 ^
  -e ASPNETCORE_HTTP_PORTS=8080 ^
  -e "ConnectionStrings__CatalogDb=Server=%DB_SERVER%;Database=CatalogDb;User Id=%DB_USER%;Password=%DB_PASSWORD%;TrustServerCertificate=True" ^
  catalogservice:latest
if errorlevel 1 echo ERROR: CatalogService container failed.

echo.
echo Starting CartService...
docker run -d --name cartservice -p 7104:8080 ^
  -e ASPNETCORE_HTTP_PORTS=8080 ^
  -e "ConnectionStrings__CartDb=Server=%DB_SERVER%;Database=CartDb;User Id=%DB_USER%;Password=%DB_PASSWORD%;TrustServerCertificate=True" ^
  cartservice:latest
if errorlevel 1 echo ERROR: CartService container failed.

echo.
echo Starting OrderService...
docker run -d --name orderservice -p 7105:8080 ^
  -e ASPNETCORE_HTTP_PORTS=8080 ^
  -e "ConnectionStrings__OrderDb=Server=%DB_SERVER%;Database=OrderDb;User Id=%DB_USER%;Password=%DB_PASSWORD%;TrustServerCertificate=True" ^
  orderservice:latest
if errorlevel 1 echo ERROR: OrderService container failed.

echo.
echo Starting InventoryService...
docker run -d --name inventoryservice -p 7106:8080 ^
  -e ASPNETCORE_HTTP_PORTS=8080 ^
  -e "ConnectionStrings__InventoryDb=Server=%DB_SERVER%;Database=InventoryDb;User Id=%DB_USER%;Password=%DB_PASSWORD%;TrustServerCertificate=True" ^
  inventoryservice:latest
if errorlevel 1 echo ERROR: InventoryService container failed.

echo.
echo Starting PaymentService...
docker run -d --name paymentservice ^
  -e "ConnectionStrings__PaymentDb=Server=%DB_SERVER%;Database=PaymentDb;User Id=%DB_USER%;Password=%DB_PASSWORD%;TrustServerCertificate=True" ^
  paymentservice:latest
if errorlevel 1 echo ERROR: PaymentService container failed.

echo.
echo ========================================
echo Container creation completed.
echo ========================================

echo.
echo Container status:
docker ps -a

echo.
pause
endlocal
