# GoWithFlow_API
GoWithFlow_API

## EF Core migrations

Run commands from the `Backend` directory.

```bash
# SQL Server migration
dotnet ef migrations add <Name> --project GoWithFlow.Infrastructure --startup-project GoWithFlow.API --context GoWithFlowDbContext --output-dir Migrations/SqlServer -- --provider SqlServer

# PostgreSQL migration
dotnet ef migrations add <Name> --project GoWithFlow.Infrastructure --startup-project GoWithFlow.API --context GoWithFlowDbContext --output-dir Migrations/PostgreSQL -- --provider PostgreSQL
```

`GoWithFlow.Infrastructure/Data/GoWithFlowDbContextFactory.cs` reads the `--provider` argument and resolves the matching `ConnectionStrings:{Provider}` entry from `GoWithFlow.API/appsettings*.json`.
