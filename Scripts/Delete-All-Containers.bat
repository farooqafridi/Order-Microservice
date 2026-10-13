@echo off
echo ==========================================
echo Deleting OrderFlow Microservice Containers
echo ==========================================

docker rm -f UserService
docker rm -f CategoryService
docker rm -f CatalogService
docker rm -f CartService
docker rm -f OrderService
docker rm -f InventoryService
docker rm -f PaymentService

echo.
echo ==========================================
echo Remaining Containers
echo ==========================================
docker ps -a

echo.
echo Container cleanup completed.
pause
