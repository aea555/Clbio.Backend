# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY Clbio.sln ./
COPY Clbio.API/Clbio.API.csproj Clbio.API/
COPY Clbio.Application/Clbio.Application.csproj Clbio.Application/
COPY Clbio.Infrastructure/Clbio.Infrastructure.csproj Clbio.Infrastructure/
COPY Clbio.Domain/Clbio.Domain.csproj Clbio.Domain/
RUN dotnet restore

COPY ..
RUN dotnet publish Clbio.API/Clbio.API.csproj -c Release -o out

# runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "Clbio.API.dll"]
