@echo off
setlocal

echo ========================================
echo Creating OrderFlow Containers
echo ========================================

docker rm -f userservice categoryservice catalogservice cartservice orderservice inventoryservice paymentservice 2>nul

echo.
echo Starting UserService...
docker run -d --name userservice -p 7101:8080 userservice:latest
if errorlevel 1 echo ERROR: UserService container failed.

echo.
echo Starting CategoryService...
docker run -d --name categoryservice -p 7102:8080 categoryservice:latest
if errorlevel 1 echo ERROR: CategoryService container failed.

echo.
echo Starting CatalogService...
docker run -d --name catalogservice -p 7103:8080 catalogservice:latest
if errorlevel 1 echo ERROR: CatalogService container failed.

echo.
echo Starting CartService...
docker run -d --name cartservice -p 7104:8080 cartservice:latest
if errorlevel 1 echo ERROR: CartService container failed.

echo.
echo Starting OrderService...
docker run -d --name orderservice -p 7105:8080 orderservice:latest
if errorlevel 1 echo ERROR: OrderService container failed.

echo.
echo Starting InventoryService...
docker run -d --name inventoryservice -p 7106:8080 inventoryservice:latest
if errorlevel 1 echo ERROR: InventoryService container failed.

echo.
echo Starting PaymentService...
docker run -d --name paymentservice paymentservice:latest
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