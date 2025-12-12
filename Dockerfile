# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY Clbio.Backend.sln ./
COPY Clbio.API/Clbio.API.csproj Clbio.API/
COPY Clbio.Application/Clbio.Application.csproj Clbio.Application/
COPY Clbio.Infrastructure/Clbio.Infrastructure.csproj Clbio.Infrastructure/
COPY Clbio.Domain/Clbio.Domain.csproj Clbio.Domain/
COPY Clbio.Tests/Clbio.Tests.csproj Clbio.Tests/
COPY Clbio.Abstractions/Clbio.Abstractions.csproj Clbio.Abstractions/
COPY Clbio.Shared/Clbio.Shared.csproj Clbio.Shared/
RUN dotnet restore

COPY . .
RUN dotnet publish Clbio.API/Clbio.API.csproj -c Release -o out

# runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "Clbio.API.dll"]
