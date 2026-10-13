```bat
@echo off
setlocal

echo ========================================
echo Building OrderFlow Docker Images
echo ========================================

docker build -t userservice:latest -f "D:\Concepts\Micro Services\OrderManagementSystem\UserService\Dockerfile" "D:\Concepts\Micro Services\published\UserService"
if errorlevel 1 echo ERROR: UserService Docker build failed.

docker build -t categoryservice:latest -f "D:\Concepts\Micro Services\OrderManagementSystem\CategoryService\Dockerfile" "D:\Concepts\Micro Services\published\CategoryService"
if errorlevel 1 echo ERROR: CategoryService Docker build failed.

docker build -t catalogservice:latest -f "D:\Concepts\Micro Services\OrderManagementSystem\CatalogService\Dockerfile" "D:\Concepts\Micro Services\published\CatalogService"
if errorlevel 1 echo ERROR: CatalogService Docker build failed.

docker build -t cartservice:latest -f "D:\Concepts\Micro Services\OrderManagementSystem\CartService\Dockerfile" "D:\Concepts\Micro Services\published\CartService"
if errorlevel 1 echo ERROR: CartService Docker build failed.

docker build -t orderservice:latest -f "D:\Concepts\Micro Services\OrderManagementSystem\OrderService\Dockerfile" "D:\Concepts\Micro Services\published\OrderService"
if errorlevel 1 echo ERROR: OrderService Docker build failed.

docker build -t inventoryservice:latest -f "D:\Concepts\Micro Services\OrderManagementSystem\InventoryService\Dockerfile" "D:\Concepts\Micro Services\published\InventoryService"
if errorlevel 1 echo ERROR: InventoryService Docker build failed.

docker build -t paymentservice:latest -f "D:\Concepts\Micro Services\OrderManagementSystem\PaymentService\Dockerfile" "D:\Concepts\Micro Services\published\PaymentService"
if errorlevel 1 echo ERROR: PaymentService Docker build failed.

echo.
echo ========================================
echo Docker build process completed.
echo Check errors above for failed builds.
echo ========================================

echo.
echo Available Docker images:
docker images

echo.
pause
endlocal
```