# Image of the ErabliereAPI MCP server, running the Streamable HTTP transport.
# Build it from the root of the repository, the context needs ErabliereAPI.Proxy:
#   docker build -t erabliereapi/erabliereapi-mcp:local -f ErabliereApi.Mcp/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-mcp-env
WORKDIR /app

COPY global.json ./

COPY ErabliereAPI.Proxy/*.csproj ./ErabliereAPI.Proxy/
COPY ErabliereApi.Mcp/*.csproj ./ErabliereApi.Mcp/
COPY ErabliereApi.Mcp.Test/*.csproj ./ErabliereApi.Mcp.Test/

RUN dotnet restore ErabliereApi.Mcp.Test/ErabliereApi.Mcp.Test.csproj

COPY ErabliereAPI.Proxy/. ./ErabliereAPI.Proxy/
COPY ErabliereApi.Mcp/. ./ErabliereApi.Mcp/
COPY ErabliereApi.Mcp.Test/. ./ErabliereApi.Mcp.Test/

RUN dotnet build ErabliereApi.Mcp/ErabliereApi.Mcp.csproj -c Release
RUN dotnet test ErabliereApi.Mcp.Test/ErabliereApi.Mcp.Test.csproj -c Release
# -o is relative to the working directory, not to the project.
RUN dotnet publish ErabliereApi.Mcp/ErabliereApi.Mcp.csproj -c Release -o /app/out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-mcp-env /app/out ./
RUN chmod u+x docker-entrypoint.sh

# curl is only there for the container health check: the aspnet base image ships
# no http client, and /live is the cheapest way to know the process is serving.
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

EXPOSE 8080

ENV ASPNETCORE_HTTP_PORTS=8080
# The image serves HTTP; the entrypoint also passes --http so a docker run
# without an environment still starts the right transport.
ENV ERABLIEREAPI_MCP_TRANSPORT=http

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl --fail http://localhost:8080/live || exit 1

ENTRYPOINT ["/app/docker-entrypoint.sh"]
