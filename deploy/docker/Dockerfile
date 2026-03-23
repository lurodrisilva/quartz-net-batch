FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["global.json", "."]
COPY ["src/Batch.Domain/Batch.Domain.csproj", "src/Batch.Domain/"]
COPY ["src/Batch.Observability/Batch.Observability.csproj", "src/Batch.Observability/"]
COPY ["src/Batch.Application/Batch.Application.csproj", "src/Batch.Application/"]
COPY ["src/Batch.Infrastructure/Batch.Infrastructure.csproj", "src/Batch.Infrastructure/"]
COPY ["src/Batch.Worker/Batch.Worker.csproj", "src/Batch.Worker/"]
RUN dotnet restore "src/Batch.Worker/Batch.Worker.csproj"

COPY src/ src/
RUN dotnet publish "src/Batch.Worker/Batch.Worker.csproj" \
    -c Release \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN groupadd --system --gid 1000 appgroup && \
    useradd --system --uid 1000 --gid appgroup appuser

COPY --from=build /app/publish .
RUN chown -R appuser:appgroup /app

USER appuser

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_gcServer=1

EXPOSE 8080

ENTRYPOINT ["dotnet", "Batch.Worker.dll"]
