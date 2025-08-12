# .Net

High-level, repeatable approach for doing reverse-engineering of a .NET project’s.

## 1. Overall Strategy

Break it into three main phases:

- Information gathering – quickly identify “anchors” in the architecture: services, databases, message buses, external dependencies.

- Classification & grouping – categorize each component (web API, background worker, UI, DB, external system).

- Relationship mapping – determine how components talk to each other (HTTP, queues, direct DB access, etc.).

## 2. What to Look for in a .NET Repository

### Identify Architectural Blocks

- By project files (.csproj)

    - Often names reveal roles: *.Api, *.Service, *.Worker, *.Function, *.Web.

    - Check OutputType (Exe / Library) and dependencies (e.g., Microsoft.AspNetCore.App).

- By entry points

    - Web apps: Program.cs/Startup.cs with WebApplication.CreateBuilder or IHostBuilder.

    - Background workers: classes implementing IHostedService / BackgroundService.

    - Azure Functions: attributes like [FunctionName].

- UI clients – Razor Pages, Blazor, WPF, WinForms.

### Detect Databases

- Dependencies: Microsoft.EntityFrameworkCore.*, Npgsql.EntityFrameworkCore.PostgreSQL, System.Data.SqlClient, etc.

- Connection strings in appsettings.json, appsettings.*.json, *.config.

- EF Core: look for DbContext classes and entities.

- Dapper/ADO.NET: search for "SELECT " / "INSERT " patterns.

### Detect Queues / Messaging

- Dependencies: MassTransit, RabbitMQ.Client, Azure.Messaging.ServiceBus, Confluent.Kafka, NServiceBus.

- Config keys like "broker", "queue", "topic".

- Subscriber classes: IConsumer<T>, MessageHandler, OnMessageAsync.

### Detect External Dependencies

- Code: HttpClient, RestSharp, Grpc.Net.Client, ReFit (and the URLs they hit).

- Config: BaseUrl, ApiKey.

- Known SDKs: Stripe, SendGrid, AWS.*, Azure.*.