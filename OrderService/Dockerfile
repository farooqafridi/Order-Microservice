FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY . .

EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "OrderService.dll"]