# Running with Docker Compose

The Compose setup builds and runs the Blazor Web app and the JWT API in separate .NET 8 containers. Both apps use an existing SQL Server database; the database server and schema must already be available.

1. Copy `.env.example` to `.env`.
2. Set `WEB_CONNECTION_STRING` and `API_CONNECTION_STRING` to SQL Server connection strings reachable from the containers. If SQL Server runs on the Docker host, `host.docker.internal` is mapped to the host gateway.
3. Set `JWT_KEY`, `JWT_ISSUER`, and `JWT_AUDIENCE`. Web and API use the same signing key, issuer, and audience.
4. Start both apps:

   ```bash
   docker compose up --build
   ```

Open the Web app at `http://localhost:8080`; the API listens at `http://localhost:8081`. The Web app addresses the API over the Compose network at `http://api:8081/`. API Swagger is enabled only in the Development environment.

The published host ports can be changed with `WEB_PORT` and `API_PORT`. Set `ALLOWED_HOSTS` to the semicolon-separated list of public tenant hostnames when it differs from the example. For multi-tenant deployments, add the matching `ConnectionStrings__Tenants__<host>` environment settings to each service as needed.

Uploaded Web files and the mutable `modulesettings.json` persist in named volumes when containers are recreated. `docker compose down -v` removes those volumes, including uploaded files and module settings. Do not commit `.env`; for production, provide credentials through the deployment platform's secret management.
