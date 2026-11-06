# Configuración pendiente

Pasos para conectar el repo con los recursos de Azure ya creados. Los valores (Tenant ID, Client ID,
secreto, cadenas de conexión, keys) **no van en este archivo ni en el repo**: tomarlos del lugar privado
donde se anotaron al crear los recursos.

Recursos de referencia (grupo `rg-gestorgastos`, región Canada Central, suscripción Azure for Students
del directorio de UNLaM; login en el tenant propio `gestorgastospw3.onmicrosoft.com`):

| Recurso | Nombre |
|---|---|
| Storage account | `stgestorgastospw3` (contenedor `comprobantes`) |
| SQL server / database | `sql-gestorgastos-pw3` / `GestorGastos` |
| Document Intelligence | `di-gestorgastos-pw3` (F0) |
| Application Insights | `appi-gestorgastos` |
| App Service | `app-gestorgastos-pw3` (plan `asp-gestorgastos`, F1) |
| Function App | `func-gestorgastos-pw3` (Flex Consumption) |
| App Registration | `GestorGastos` (tenant GestorGastos) |

## 1. En la máquina de cada integrante

Desde la carpeta del repo:

```bash
cp .env.example .env        # poner una contraseña fuerte en GESTORGASTOS_DB_PASSWORD
docker compose --env-file .env up -d   # en Windows con LocalDB: solo Azurite (docker compose up -d azurite o por npm, ver abajo)

dotnet user-secrets set "AzureAd:TenantId" "<Tenant ID de GestorGastos>" --project GestorGastos.Web
dotnet user-secrets set "AzureAd:ClientId" "<Application (client) ID>" --project GestorGastos.Web
dotnet user-secrets set "AzureAd:ClientSecret" "<Value del secreto>" --project GestorGastos.Web
```

- En local se usa una base propia, no la de Azure (no se gasta el cupo gratuito y cada uno tiene sus
  datos): **LocalDB** en Windows, sin configurar nada, y el SQL Server de **Docker** en Linux/macOS, con las
  variables `GESTORGASTOS_DB_*` del `.env`. En la terminal hay que exportarlas antes de `dotnet run`,
  `dotnet ef` o `func start`: `set -a && source .env && set +a`. El launch de VS Code ya las carga.
  No hace falta la connection string en User Secrets; si alguien ya la había guardado, borrarla con
  `dotnet user-secrets remove "ConnectionStrings:GestorGastos" --project GestorGastos.Web` (gana sobre lo anterior).
  El Storage local es Azurite y ya está configurado en `appsettings.Development.json`.
- Windows sin Docker: LocalDB para la base y Azurite instalado por npm (requiere [Node.js](https://nodejs.org) LTS).
  Instalarlo una vez:

  ```powershell
  npm install -g azurite
  ```

  Y levantarlo en una terminal aparte, que queda abierta mientras se trabaja (Ctrl+C lo detiene):

  ```powershell
  azurite --skipApiVersionCheck --location "$env:USERPROFILE\.azurite"
  ```

  `--skipApiVersionCheck` evita errores de versión con el SDK de Storage (el `compose.yaml` usa el mismo flag) y
  `--location` guarda los datos fuera del repo. Tiene que estar corriendo antes de `dotnet run` y de `func start`, y
  no puede convivir con el Azurite de Docker (usan los mismos puertos 10000–10002). Para actualizarlo: `npm update -g azurite`.
- Opcional, para ver las trazas locales en Azure:
  `dotnet user-secrets set "APPLICATIONINSIGHTS_CONNECTION_STRING" "<cadena>" --project GestorGastos.Web`
- Function: `cp GestorGastos.Functions/local.settings.example.json GestorGastos.Functions/local.settings.json`
  y dejar `ConnectionStrings:GestorGastos` vacía (usa la misma base local que la web). `DocumentIntelligence__*` puede
  quedar vacío: la Function usa el extractor mock. Un archivo con `error` o `borroso` en el nombre simula una extracción fallida.

**Prueba:** `dotnet run --project GestorGastos.Web --launch-profile https` y abrir `https://localhost:7282`.
- `/` muestra la Bienvenida sin pedir login. **Iniciar sesión con Microsoft** lleva al login del tenant GestorGastos.
- Después del login, con rol `Empleado` o `Administrador`, se ve "Hola, <nombre>" con la navbar (Administración solo
  para el admin) y el usuario queda guardado en la tabla `Usuarios`. Sin rol, se ve Acceso denegado.
- **Cerrar sesión** (menú con el nombre arriba a la derecha) vuelve a la Bienvenida.
- Los links Mis comprobantes, Cargar y Administración dan 404 porque esas pantallas todavía no existen.

## 2. En Azure: App Service

Con el directorio de **UNLaM** seleccionado, abrir `app-gestorgastos-pw3` y entrar a **Settings → Environment variables → App settings**. Agregar una por fila con **Add**:

| Name | Value |
|---|---|
| `AzureAd__TenantId` | Tenant ID |
| `AzureAd__ClientId` | Client ID |
| `AzureAd__ClientSecret` | Value del secreto |
| `ConnectionStrings__GestorGastos` | Cadena ADO.NET de Azure SQL, con la contraseña |
| `Storage__ConnectionString` | Cadena de `stgestorgastospw3` |
| `Storage__Contenedor` | `comprobantes` |

Verificar que ya exista `APPLICATIONINSIGHTS_CONNECTION_STRING` (se agrega al conectar App Insights al
crear la app). Al final, **Apply → Confirm** (la app se reinicia).

## 3. En Azure: Function App

Abrir `func-gestorgastos-pw3` y entrar a **Settings → Environment variables → App settings**:

| Name | Value |
|---|---|
| `ConnectionStrings__GestorGastos` | Cadena de Azure SQL |
| `Storage__ConnectionString` | Cadena del Storage |
| `Storage__Contenedor` | `comprobantes` |
| `DocumentIntelligence__Endpoint` | Endpoint |
| `DocumentIntelligence__Key` | KEY 1 |

`AzureWebJobsStorage` (o su variante con managed identity) y `APPLICATIONINSIGHTS_CONNECTION_STRING` los
creó el asistente: no tocarlos.

## 4. Migraciones en Azure SQL

Desde el lugar de desarrollo habitual:

1. Agregar la IP en el firewall del servidor SQL (repetir si cambia la IP o la red):
   - Abrir el servidor `sql-gestorgastos-pw3` (no la base).
   - **Security → Networking**.
   - En **Firewall rules**, clic en **Add your client IPv4 address**.
   - **Save**.
2. Correr:

   ```bash
   ConnectionStrings__GestorGastos="<cadena de Azure SQL>" \
     dotnet ef database update --project GestorGastos.Entidades --startup-project GestorGastos.Web
   ```

   Alternativa sin abrir el firewall: generar el script y ejecutarlo en **Query editor** de la base, en el portal.

   ```bash
   dotnet ef migrations script --idempotent -o migracion.sql \
     --project GestorGastos.Entidades --startup-project GestorGastos.Web
   ```

Repetir cada vez que se agregue una migración: en Azure la web no las aplica sola (solo en Development).

## 5. Qué falta después

- **Desplegar la web y la Function**: el App Service y la Function App están vacíos. A mano (VS Code,
  `dotnet publish`) o agregando CD al pipeline de GitHub Actions.
- **Event Grid** (la Function hay que publicarla en **Release**: el build Debug usa el trigger clásico, que Flex Consumption no admite):
  1. En `func-gestorgastos-pw3`: **Functions → App keys → System keys**, copiar `blobs_extension`.
  2. En `stgestorgastospw3`: **Events → Event Subscription**, con:
     - Esquema: **Event Grid Schema**.
     - Evento: **Blob Created**.
     - Filtro de sujeto: empieza con `/blobServices/default/containers/comprobantes/`.
     - Endpoint: **Web Hook**, con esta URL:

       ```text
       https://<dominio de la Function App>/runtime/webhooks/blobs?functionName=Host.Functions.ExtraerComprobanteFunction&code=<blobs_extension>
       ```
- **Contraseñas definitivas de las cuentas de prueba** (si no se hizo): iniciar sesión una vez con cada una en `https://myapps.microsoft.com` en una ventana de incógnito y guardarlas en un lugar privado.
- **Permisos de los integrantes** (si no se hizo):
  - Rol **Contributor** en `rg-gestorgastos` (directorio de UNLaM).
  - **Owner** del App Registration `GestorGastos` (tenant GestorGastos).

## Problemas comunes

| Síntoma | Causa probable |
|---|---|
| `AADSTS50011` (redirect URI no coincide) | Falta la URI exacta (esquema, puerto, `/signin-oidc`) en **Authentication** del App Registration |
| `AADSTS700054` (response_type id_token no habilitado) | No está tildado **ID tokens** en **Authentication** |
| `AADSTS50105` | **Assignment required?** quedó en Yes y el usuario no tiene rol |
| `AADSTS7000215` (secreto inválido) | Se copió el *Secret ID* en lugar del *Value*, o el secreto venció (vence a los 365 días) |
| El usuario entra pero no tiene rol | Se asignó el rol con la sesión abierta: cerrar sesión y volver a entrar |
| La web en Azure no conecta a SQL | Falta **Allow Azure services** en el firewall del servidor |
| `dotnet ef` no conecta a Azure SQL | La IP actual no está en el firewall |
| La base no responde a fin de mes | Se agotó el cupo gratuito mensual y quedó pausada hasta el mes siguiente |
