# OrderFlow – Microservices Architecture with ASP.NET Core and Docker

## 1. Project Overview

OrderFlow is an e-commerce microservices demonstration project built with ASP.NET Core. It separates core business capabilities into independently deployable services that communicate over HTTP, gRPC, and asynchronous messaging.

The project demonstrates:

- Microservices architecture and service boundaries
- ASP.NET Core Web APIs
- Docker images and containers
- Docker networking and service discovery
- Inter-service HTTP communication
- gRPC communication between services
- SQL Server and Entity Framework Core
- Azure Service Bus integration
- Background worker services
- Environment-based configuration
- Container diagnostics and troubleshooting

## 2. Architecture

The system contains seven microservices.

| Service | Responsibility | Host Port |
|---|---|---:|
| UserService | User management and related operations | 7101 |
| CategoryService | Product category management | 7102 |
| CatalogService | Product catalog and category integration | 7103 |
| CartService | Shopping cart management | 7104 |
| OrderService | Order creation and orchestration | 7105 |
| InventoryService | Stock availability and reservation using gRPC | 7106 |
| PaymentService | Background payment processing | None |

The HTTP port mappings above assume that the respective API listens on port `8080` inside its container. PaymentService is a worker service and does not require an HTTP port mapping unless an HTTP endpoint is added.

### High-level architecture

```text
                         Client / Postman
                                |
             +------------------+------------------+
             |                  |                  |
        UserService       CatalogService       OrderService
          :7101              :7103                :7105
                                |                   |
                         CategoryService       CartService
                            :7102                 :7104
                                                    |
                                                    |
                                             InventoryService
                                                 :7106
                                              (gRPC / HTTP2)

                         PaymentService
                       (Background Worker)
                                |
                         Azure Service Bus
```

The diagram illustrates the main services and example communication paths. Actual calls depend on the endpoints and integrations implemented in each service.

## 3. Technology Stack

- C#
- ASP.NET Core Web API
- .NET
- Entity Framework Core
- SQL Server
- gRPC
- Docker
- Azure Service Bus
- Postman
- Windows command-line scripts

Use the .NET SDK and runtime versions targeted by the individual service projects.

## 4. Project Directory Structure

Example local layout:

```text
D:\Concepts\Micro Services\OrderManagementSystem\
│
├── UserService\
│   └── Dockerfile
├── CategoryService\
│   └── Dockerfile
├── CatalogService\
│   └── Dockerfile
├── CartService\
│   └── Dockerfile
├── OrderService\
│   └── Dockerfile
├── InventoryService\
│   └── Dockerfile
├── PaymentService\
│   └── Dockerfile
│
├── OrderFlow.env
└── README.md

D:\Concepts\Micro Services\published\
│
├── UserService\
├── CategoryService\
├── CatalogService\
├── CartService\
├── OrderService\
├── InventoryService\
└── PaymentService\
```

The `published` directories contain the published application files used as Docker build contexts.

The deployment script expects the paths and folder names above. Update the script if your actual directory layout differs.

## 5. Prerequisites

Install and configure:

1. Docker Desktop for Windows.
2. The .NET SDK required by the projects.
3. SQL Server, if required by the services.
4. Azure Service Bus configuration, if required by the application.
5. Postman for API testing.

Verify Docker is available:

```bat
docker --version
docker info
```

Verify the .NET SDK:

```bat
dotnet --info
```

## 6. Environment Configuration

The deployment script uses an environment file.

For the current script, the expected path is:

```text
D:\Concepts\Micro Services\OrderManagementSystem\OrderFlow.env
```

If your file is instead located at `OrderFlow\.env`, update `ENV_FILE` in the deployment script accordingly.

Example environment variables:

```dotenv
ASPNETCORE_ENVIRONMENT=Production
```

Add the database connection strings, authentication settings, and Azure Service Bus configuration required by your actual services.

For example, a SQL Server connection string may resemble:

```dotenv
ConnectionStrings__DefaultConnection=Server=YOUR_SQL_SERVER;Database=YOUR_DATABASE;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True
```

Use the connection-string key expected by each service. Do not copy the example connection string without adapting it.

### Important configuration notes

- `localhost` inside a container refers to that same container, not your Windows host and not another service.
- Use Docker service names for communication between containers.
- For SQL Server running on your Windows host, a suitable Docker-accessible hostname may be `host.docker.internal`.
- For SQL Server running in another container, use that container's network name and its internal port.
- Do not commit real passwords, tokens, or production connection strings to source control.
- Docker `--env-file` passes configuration into a container; it does not automatically make an application read arbitrary environment variables unless the configuration keys are recognized by the application.

## 7. Build and Publish the Services

Publish each service before building the Docker images.

Example:

```bat
dotnet publish "D:\Concepts\Micro Services\OrderManagementSystem\UserService\UserService.csproj" -c Release -o "D:\Concepts\Micro Services\published\UserService"
```

Repeat for the other six services, adjusting the project file and output directory.

Ensure that each service's published directory contains its application DLLs, dependencies, configuration files, and any other required runtime assets.

If you change the application code, republish the affected service before rebuilding its image.

## 8. Docker Networking

OrderFlow uses a shared Docker network named:

```text
orderflow-network
```

The deployment script creates this network if it does not already exist.

To create it manually:

```bat
docker network create orderflow-network
```

To inspect it:

```bat
docker network inspect orderflow-network
```

### Internal service URLs

Containers on the shared network communicate using container names and internal ports.

| Calling Service | Destination | Internal URL |
|---|---|---|
| CartService | CatalogService | `http://catalogservice:8080/` |
| CatalogService | CategoryService | `http://categoryservice:8080/` |
| OrderService | CartService | `http://cartservice:8080/` |
| OrderService | CatalogService | `http://catalogservice:8080/` |
| OrderService | InventoryService | `http://inventoryservice:8080/` for gRPC when configured accordingly |

These URLs are examples based on the current deployment configuration. The application must use the correct HTTP or gRPC client for each destination.

### Host ports versus container ports

For example:

```bat
-p 7103:8080
```

means:

- `7103`: port exposed on the Windows host.
- `8080`: port used inside the container.

Postman uses:

```text
http://localhost:7103
```

A different container on the same Docker network uses:

```text
http://catalogservice:8080
```

Do not use `localhost:7103` for container-to-container communication.

## 9. Deploy All Services

Run the deployment batch file from Command Prompt or File Explorer.

For example, if the file is named `deploy-orderflow.bat`:

```bat
deploy-orderflow.bat
```

The script performs the following operations:

1. Validates Docker and the environment file.
2. Checks for all Dockerfiles and published directories.
3. Creates the shared Docker network if needed.
4. Removes existing OrderFlow containers.
5. Builds the seven Docker images.
6. Starts the containers on the shared network.
7. Maps API host ports `7101` through `7106`.
8. Starts PaymentService without an HTTP port mapping.
9. Displays container status and network information.

**Warning:** The script removes existing containers with the OrderFlow container names before recreating them. Do not use those names for unrelated containers.

## 10. Verify Deployment

List running containers:

```bat
docker ps
```

List all containers, including stopped ones:

```bat
docker ps -a
```

Inspect the shared network:

```bat
docker network inspect orderflow-network
```

View logs:

```bat
docker logs --tail 100 userservice
docker logs --tail 100 categoryservice
docker logs --tail 100 catalogservice
docker logs --tail 100 cartservice
docker logs --tail 100 orderservice
docker logs --tail 100 inventoryservice
docker logs --tail 100 paymentservice
```

Follow logs continuously:

```bat
docker logs -f orderservice
```

A container being listed as `Up` does not necessarily mean its application is ready to handle requests. Check application logs and test its endpoints.

## 11. Test APIs Using Postman

Use these base URLs:

| Service | Base URL |
|---|---|
| UserService | `http://localhost:7101` |
| CategoryService | `http://localhost:7102` |
| CatalogService | `http://localhost:7103` |
| CartService | `http://localhost:7104` |
| OrderService | `http://localhost:7105` |
| InventoryService | `http://localhost:7106` |

PaymentService is a background worker, so it has no HTTP base URL by default.

Append the actual controller route or endpoint path to the relevant base URL. The exact endpoint paths depend on the routes implemented in each service.

For example, if a service implements `GET /api/products`, the request would be:

```http
GET http://localhost:7103/api/products
```

Use the HTTP method, request body, and authentication headers required by each endpoint.

### Testing checklist

- [ ] UserService responds successfully.
- [ ] CategoryService responds successfully.
- [ ] CatalogService returns products.
- [ ] CartService can retrieve the expected cart.
- [ ] OrderService can retrieve or create orders.
- [ ] InventoryService accepts its supported gRPC calls.
- [ ] PaymentService starts and processes its configured background work.
- [ ] Inter-service calls use Docker DNS names rather than host `localhost` URLs.

## 12. gRPC Configuration for InventoryService

OrderService communicates with InventoryService through gRPC for stock operations such as `ReserveStock`.

The client address is typically configured as:

```csharp
builder.Services.AddGrpcClient<Inventory.InventoryClient>(options =>
{
    options.Address = new Uri("http://inventoryservice:8080");
});
```

Replace `Inventory.InventoryClient` with the generated client type used by your project.

### HTTP/2 requirement

gRPC requires HTTP/2. If the client connects using cleartext HTTP to `inventoryservice:8080`, InventoryService must be configured to support HTTP/2 without TLS on that endpoint.

For example:

```csharp
using Microsoft.AspNetCore.Server.Kestrel.Core;

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});
```

Preserve the service's existing registrations and endpoint mappings when applying this configuration.

If InventoryService also exposes REST endpoints, consider separate ports for REST and gRPC, or configure HTTPS with appropriate HTTP/2 support. Do not assume that an HTTP/1.1 REST endpoint is automatically suitable for gRPC.

After changing the code:

1. Republish InventoryService.
2. Rebuild its Docker image.
3. Recreate the container.
4. Check its logs.
5. Retry the operation through OrderService.

## 13. Troubleshooting

### A. Postman reports "Could not get response"

Check whether the container is running:

```bat
docker ps -a
docker logs --tail 100 userservice
```

Confirm the host port is mapped correctly:

```bat
docker port userservice
```

Check that the application listens on the expected container port, normally `8080` for this deployment.

### B. Container exits immediately

Inspect the logs:

```bat
docker logs userservice
```

Common causes include:

- Missing configuration.
- Invalid connection strings.
- Database connectivity problems.
- Missing runtime files.
- Application startup exceptions.
- Invalid Azure Service Bus configuration.

### C. Service-to-service HTTP call fails

Check that both containers are attached to the shared network:

```bat
docker network inspect orderflow-network
```

Inspect the caller's configuration and logs:

```bat
docker logs --tail 100 cartservice
docker logs --tail 100 catalogservice
```

Verify the URL uses the destination container name and internal port.

### D. gRPC error: `HTTP_1_1_REQUIRED`

This indicates that the gRPC request is not successfully communicating over the required HTTP/2 connection.

Check:

- InventoryService Kestrel protocol configuration.
- The actual protocol configured on port `8080`.
- The OrderService gRPC client address.
- Whether the endpoint expects TLS.
- Whether the latest published InventoryService code was included in the Docker image.

Useful commands:

```bat
docker logs --tail 100 inventoryservice
docker logs --tail 100 orderservice
docker inspect inventoryservice
```

### E. Database connection fails

Check:

- The connection string key used by the service.
- Whether the database is reachable from the container.
- Whether the hostname is valid from inside Docker.
- Whether SQL Server authentication and permissions are configured correctly.
- Whether migrations or database initialization have been applied.

Remember that `localhost` inside a container refers to that container.

### F. Azure Service Bus connection fails

Check:

- The correct connection string or supported credential configuration.
- The namespace and entity names.
- Network connectivity.
- Whether the configured identity has the required permissions.
- Whether the application reads the expected configuration key.

Do not publish connection strings or secrets in logs, screenshots, or source control.

### G. PaymentService is running but no payment work is processed

Check:

```bat
docker logs --tail 100 paymentservice
```

Verify that the worker is configured with the correct messaging entity, receives the expected messages, and handles failures and retries correctly.

## 14. Useful Docker Commands

Stop all OrderFlow containers:

```bat
docker stop userservice categoryservice catalogservice cartservice orderservice inventoryservice paymentservice
```

Remove the containers:

```bat
docker rm -f userservice categoryservice catalogservice cartservice orderservice inventoryservice paymentservice
```

List local images:

```bat
docker images
```

Rebuild one service image:

```bat
docker build -t inventoryservice:latest -f "D:\Concepts\Micro Services\OrderManagementSystem\InventoryService\Dockerfile" "D:\Concepts\Micro Services\published\InventoryService"
```

Recreate one service after rebuilding:

```bat
docker rm -f inventoryservice
docker run -d --name inventoryservice --network orderflow-network -p 7106:8080 --env-file "D:\Concepts\Micro Services\OrderManagementSystem\OrderFlow.env" inventoryservice:latest
```

Adjust the environment-file path if your file is stored elsewhere.

## 15. Important Design Considerations

- Each microservice should own its business logic and data boundary.
- Avoid accessing another service's database directly.
- Use HTTP or gRPC for synchronous request/response interactions.
- Use messaging for asynchronous workflows and decoupled processing.
- Design message consumers to handle duplicate delivery safely.
- Consider the transactional outbox pattern when database updates and message publication must be coordinated.
- Add health checks and readiness checks for reliable startup.
- Use structured logging and distributed tracing to follow requests across services.
- Apply secure secret management and least-privilege access.
- Implement appropriate retries, timeouts, and error handling.
- Treat order, payment, and inventory updates as a business workflow requiring explicit failure and compensation handling where applicable.

## 16. Current Deployment Summary

| Setting | Value |
|---|---|
| Docker network | `orderflow-network` |
| Container application port | `8080` |
| UserService host port | `7101` |
| CategoryService host port | `7102` |
| CatalogService host port | `7103` |
| CartService host port | `7104` |
| OrderService host port | `7105` |
| InventoryService host port | `7106` |
| PaymentService | Background worker |
| Deployment | Windows batch script |
| Build context | Published application directories |

## 17. Future Enhancements

Potential next steps include:

- Add Docker Compose for declarative service orchestration.
- Add health and readiness endpoints.
- Introduce centralized observability with OpenTelemetry.
- Add integration tests for inter-service communication.
- Improve retry and idempotency handling.
- Implement an outbox-based messaging workflow.
- Add API gateway functionality.
- Deploy to Kubernetes or Azure Kubernetes Service.
- Automate builds and deployments through Azure DevOps pipelines.
- Add secure configuration management for development and production.

---

**OrderFlow** is intended as a practical learning project for building, containerizing, connecting, testing, and troubleshooting ASP.NET Core microservices.