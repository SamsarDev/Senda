# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/Senda.Api/Senda.Api.csproj", "src/Senda.Api/"]
COPY ["src/Senda.Application/Senda.Application.csproj", "src/Senda.Application/"]
COPY ["src/Senda.Core/Senda.Core.csproj", "src/Senda.Core/"]
COPY ["src/Senda.Infrastructure/Senda.Infrastructure.csproj", "src/Senda.Infrastructure/"]

RUN dotnet restore "src/Senda.Api/Senda.Api.csproj"

# Copy the rest of the source code
COPY . .
WORKDIR "/src/src/Senda.Api"
RUN dotnet build "Senda.Api.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "Senda.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Senda.Api.dll"]
