FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY oficinafiap-execucao-service.sln .
COPY Oficina.Execucao.Api/Oficina.Execucao.Api.csproj Oficina.Execucao.Api/
COPY Oficina.Execucao.Application/Oficina.Execucao.Application.csproj Oficina.Execucao.Application/
COPY Oficina.Execucao.Domain/Oficina.Execucao.Domain.csproj Oficina.Execucao.Domain/
COPY Oficina.Execucao.Infrastructure/Oficina.Execucao.Infrastructure.csproj Oficina.Execucao.Infrastructure/
COPY Oficina.Execucao.Domain.UnitTests/Oficina.Execucao.Domain.UnitTests.csproj Oficina.Execucao.Domain.UnitTests/
COPY Oficina.Execucao.Application.UnitTests/Oficina.Execucao.Application.UnitTests.csproj Oficina.Execucao.Application.UnitTests/
COPY Oficina.Execucao.Api.IntegrationTests/Oficina.Execucao.Api.IntegrationTests.csproj Oficina.Execucao.Api.IntegrationTests/

RUN dotnet restore "oficinafiap-execucao-service.sln"

COPY . .

RUN dotnet build "oficinafiap-execucao-service.sln" -c Release --no-restore

RUN dotnet test "Oficina.Execucao.Domain.UnitTests/Oficina.Execucao.Domain.UnitTests.csproj" -c Release --no-build --no-restore
RUN dotnet test "Oficina.Execucao.Application.UnitTests/Oficina.Execucao.Application.UnitTests.csproj" -c Release --no-build --no-restore

WORKDIR /src/Oficina.Execucao.Api
RUN dotnet publish "Oficina.Execucao.Api.csproj" -c Release -o /app/publish --no-restore --no-build

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080

RUN apt-get update && apt-get install -y curl && \
    curl -Lo /tmp/datadog-dotnet-apm.deb https://github.com/DataDog/dd-trace-dotnet/releases/download/v3.3.1/datadog-dotnet-apm_3.3.1_amd64.deb && \
    dpkg -i /tmp/datadog-dotnet-apm.deb && \
    rm /tmp/datadog-dotnet-apm.deb && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}
ENV CORECLR_PROFILER_PATH=/opt/datadog/Datadog.Trace.ClrProfiler.Native.so
ENV DD_DOTNET_TRACER_HOME=/opt/datadog

EXPOSE 8080
ENTRYPOINT ["dotnet", "Oficina.Execucao.Api.dll"]
