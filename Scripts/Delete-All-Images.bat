@echo off
echo ==========================================
echo Deleting OrderFlow Microservice Images
echo ==========================================

docker rmi -f userservice:latest
docker rmi -f categoryservice:latest
docker rmi -f catalogservice:latest
docker rmi -f cartservice:latest
docker rmi -f orderservice:latest
docker rmi -f inventoryservice:latest
docker rmi -f paymentservice:latest

echo.
echo ==========================================
echo Remaining Images
echo ==========================================
docker images

echo.
echo Image cleanup completed.
pause
