# Running with Docker Compose

The Compose setup builds the Blazor Web app and JWT API as separate .NET 8 containers and runs SQL Server 2022 in a third service. On first start, a one-shot initialization service creates the `MyShop` database. The repository has no complete bootstrap schema or automatic migration, so the application tables and stored procedures must still be provisioned from the SQL scripts or a database backup.

1. Copy `.env.example` to `.env`.
2. Set `DB_SA_PASSWORD` to a strong password that meets SQL Server's password policy. Both applications use the `MyShop` database on the Compose `db` service; Compose supplies their connection strings.
3. Set `JWT_KEY`, `JWT_ISSUER`, and `JWT_AUDIENCE`. Web and API use the same signing key, issuer, and audience.
4. Start the stack:

   ```bash
   docker compose up --build
   ```

Open the Web app at `http://localhost:8080`; the API listens at `http://localhost:8081`. SQL Server is published to `127.0.0.1:1433` for local DB tools and is reachable by the apps as `db:1433`. The Web app addresses the API over the Compose network at `http://api:8081/`. API Swagger is enabled only in the Development environment.

The published host ports can be changed with `WEB_PORT`, `API_PORT`, and `DB_PORT`. Set `ALLOWED_HOSTS` to the semicolon-separated list of public tenant hostnames when it differs from the example. For multi-tenant deployments, add the matching `ConnectionStrings__Tenants__<host>` environment settings to each service as needed.

SQL Server data, uploaded Web files, and the mutable `modulesettings.json` persist in named volumes when containers are recreated. `docker compose down -v` removes those volumes, including the database and uploaded files. Do not commit `.env`; for production, provide credentials through the deployment platform's secret management. SQL Server Developer edition is for development and testing, not production use.
