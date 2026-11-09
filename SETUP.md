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
- Después del login, con rol `Empleado` o `Administrador`, se ve el Dashboard con la navbar (Administración solo
  para el admin) y el usuario queda guardado en la tabla `Usuarios`. Sin rol, se ve Acceso denegado.
- Con `func start` corriendo en `GestorGastos.Functions`, un comprobante cargado pasa de Procesando a Pendiente de
  Revisión (o a Error si el nombre del archivo tiene `error` o `borroso`).
- **Cerrar sesión** (menú con el nombre arriba a la derecha) vuelve a la Bienvenida.

## 2. En Azure: App Service

Con el directorio de **UNLaM** seleccionado, abrir `app-gestorgastos-pw3`.

1. **Settings → Environment variables → App settings**. Agregar una por fila con **Add**:

   | Name | Value |
   |---|---|
   | `AzureAd__TenantId` | Tenant ID |
   | `AzureAd__ClientId` | Client ID |
   | `AzureAd__ClientSecret` | Value del secreto |
   | `ConnectionStrings__GestorGastos` | Cadena ADO.NET de Azure SQL, con la contraseña (usuario administrador del servidor: la web aplica las migraciones) |
   | `Storage__ConnectionString` | Cadena de `stgestorgastospw3` (**Security + networking → Access keys → Connection string**; tiene que incluir `AccountKey`, se usa para firmar las URLs SAS) |
   | `Storage__Contenedor` | `comprobantes` |

   Verificar que ya exista `APPLICATIONINSIGHTS_CONNECTION_STRING` (se agrega al conectar App Insights al
   crear la app). Al final, **Apply → Confirm** (la app se reinicia).
2. **Settings → Configuration → General settings**: Stack **.NET**, versión **.NET 10**, y **HTTPS Only** en On. **Save**.

## 3. En Azure: Function App

Abrir `func-gestorgastos-pw3` y entrar a **Settings → Environment variables → App settings**:

| Name | Value |
|---|---|
| `ConnectionStrings__GestorGastos` | Cadena de Azure SQL |
| `Storage__ConnectionString` | Cadena del Storage (la misma que la web) |
| `Storage__Contenedor` | `comprobantes` |
| `DocumentIntelligence__Endpoint` | Endpoint de `di-gestorgastos-pw3` (**Resource Management → Keys and Endpoint**) |
| `DocumentIntelligence__Key` | KEY 1 |

`AzureWebJobsStorage` (o su variante con managed identity) y `APPLICATIONINSIGHTS_CONNECTION_STRING` los
creó el asistente: no tocarlos. Si `DocumentIntelligence__Endpoint` queda vacío, la Function usa el extractor mock.

## 4. Azure SQL y migraciones

La web aplica las migraciones pendientes **al arrancar**, también en Azure (decisión del equipo, ver `AGENTS.md` sección 0):
no hace falta correr `dotnet ef database update` contra producción. Solo hay que permitir que la web y la Function lleguen
a la base:

1. Abrir el servidor `sql-gestorgastos-pw3` (no la base) → **Security → Networking**.
2. Tildar **Allow Azure services and resources to access this server** → **Save**.

Para conectarse desde la máquina propia (por ejemplo con Azure Data Studio, para mirar datos), agregar además la IP en
**Firewall rules → Add your client IPv4 address**.

## 5. Desplegar (paso a paso)

### 5.1 Herramientas (una vez)

- **Azure CLI**: Arch/CachyOS `sudo pacman -S azure-cli`; Windows `winget install Microsoft.AzureCLI`; otros sistemas:
  <https://learn.microsoft.com/cli/azure/install-azure-cli>.
- **Azure Functions Core Tools v4**: `npm install -g azure-functions-core-tools@4` (también sirve para `func start` en local).
- Iniciar sesión en el directorio de UNLaM y elegir la suscripción:

  ```bash
  az login                                  # abre el navegador; entrar con la cuenta de UNLaM
  az account list -o table                  # ver el nombre de la suscripción
  az account set --subscription "<nombre de la suscripción Azure for Students>"
  ```

### 5.2 App Registration (tenant GestorGastos)

En el portal, cambiar al directorio **GestorGastos** → **App registrations → GestorGastos → Authentication**. En la
plataforma **Web**, agregar estas Redirect URIs (además de las de localhost) y **Save**:

```text
https://app-gestorgastos-pw3.azurewebsites.net/signin-oidc
https://app-gestorgastos-pw3.azurewebsites.net/signout-callback-oidc
```

Si el App Service tiene otro dominio (ver **Overview → Default domain**), usar ese.

### 5.3 Publicar la web

Desde la raíz del repo (la carpeta de salida queda fuera del repo):

```bash
dotnet publish GestorGastos.Web -c Release -o /tmp/gestorgastos-web
(cd /tmp/gestorgastos-web && zip -qr ../gestorgastos-web.zip .)
az webapp deploy --resource-group rg-gestorgastos --name app-gestorgastos-pw3 \
  --src-path /tmp/gestorgastos-web.zip --type zip
```

Verificar: abrir `https://app-gestorgastos-pw3.azurewebsites.net`. La primera carga puede tardar ~1 minuto (arranque en
frío del F1 y, si la base estaba pausada, su reanudación); en ese arranque se crean las tablas. Si da error 500,
mirar **App Service → Monitoring → Log stream**.

### 5.4 Publicar la Function

```bash
cd GestorGastos.Functions
func azure functionapp publish func-gestorgastos-pw3
cd ..
```

`func` compila en **Release**, que es lo que hace falta: el build Release usa el Blob Trigger con origen Event Grid, el único
que admite Flex Consumption. Verificar en **Function App → Overview → Functions** que aparezca `ExtraerComprobanteFunction`.

### 5.5 Event Grid

1. **Registrar el proveedor** (una vez por suscripción): en **Subscriptions → (la suscripción) → Settings → Resource
   providers**, buscar `Microsoft.EventGrid` y, si no dice *Registered*, hacer **Register**. Tarda unos minutos.
2. **Copiar la clave del webhook**: en `func-gestorgastos-pw3`, **Functions → App keys → System keys**, copiar el valor de
   `blobs_extension`. Existe recién después de publicar la Function.
3. **Armar la URL del webhook** con el *Default domain* de la Function App (**Overview**; Flex Consumption le agrega un
   sufijo al nombre) y la clave del paso 2:

   ```text
   https://func-gestorgastos-pw3-b4awdah7g9fca0a0.canadacentral-01.azurewebsites.net/runtime/webhooks/blobs?functionName=Host.Functions.ExtraerComprobanteFunction&code=<blobs_extension>
   ```

4. **Crear la suscripción**: en `stgestorgastospw3`, **Events → + Event Subscription**, y completar:

   | Campo | Valor |
   |---|---|
   | Name | `blob-comprobantes` |
   | Event Schema | Event Grid Schema |
   | System Topic Name | `stgestorgastospw3-eventos` (si lo pide) |
   | Filter to Event Types | Solo **Blob Created** |
   | Endpoint Type | **Web Hook** → **Configure an endpoint**, con la URL del paso 3 |
   | Pestaña **Filters** | Tildar **Enable subject filtering**; en *Subject Begins With*: `/blobServices/default/containers/comprobantes/` |

5. **Create**. Si falla la validación del endpoint, revisar que la Function esté publicada y que el `code` de la URL sea el
   de `blobs_extension`.

### 5.6 Prueba de punta a punta

Con la cuenta de prueba del Empleado:

1. Cargar un recibo legible (foto o PDF de un ticket). En menos de un minuto tiene que pasar a **Pendiente de Revisión**
   con monto y descripción.
2. Cargar una imagen ilegible (por ejemplo, una foto borrosa o de algo que no sea un comprobante). Tiene que terminar en
   **Error de extracción**; completar monto y descripción y guardarlo para enviarlo a revisión.
3. En el detalle, probar la vista previa y **Descargar**. Copiar la URL de la imagen, esperar 5 minutos y abrirla: tiene que
   dar error de autorización.

Con la cuenta del Administrador: en **Administración**, tomar → aprobar → marcar pagado un comprobante, y tomar → rechazar
otro con motivo. Si la extracción no avanza, mirar **Function App → Monitoring → Logs** y la suscripción de Event Grid
(**Storage → Events → la suscripción → Metrics**: *Delivered* vs *Failed*).

### 5.7 Captura de Application Insights (entregable)

1. Hacer una carga nueva (paso 5.6.1) y anotar la hora.
2. Abrir `appi-gestorgastos` → **Monitoring → Logs** (cerrar la galería de consultas, modo **KQL**) y correr:

   ```kusto
   union traces, requests, exceptions
   | where timestamp > ago(30m)
   | where message has "Comprobante" or message has "extracción" or operation_Name has "ExtraerComprobante"
   | project timestamp, cloud_RoleName, itemType, message, operation_Name
   | order by timestamp asc
   ```

   Tienen que verse, en orden: "Comprobante N cargado…" (web), "Inicio de extracción…", "Comprobante N pasó de Procesando
   a PendienteRevision" y "Fin de extracción … en N ms" (Function). Las trazas pueden tardar 2 a 5 minutos en aparecer.
3. Otra vista útil para la captura: **Investigate → Transaction search**, filtrar por los últimos 30 minutos y abrir la
   ejecución de `ExtraerComprobanteFunction`.
4. Si solo aparecen requests y ningún `traces` de nivel Information, avisar: habría que ajustar el filtro de logs del SDK.

### 5.8 Opcional: CD con GitHub Actions

Se puede agregar un job de despliegue al workflow de CI con `azure/login` (credenciales federadas OIDC de un service
principal con rol Contributor en `rg-gestorgastos`), `azure/webapps-deploy` para la web y `Azure/functions-action` para la
Function. No es obligatorio para la entrega.

## 6. Otros pendientes

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
| Error 500 en la web en Azure al arrancar | Falta algún App Setting (`AzureAd__*`, `Storage__*`, `ConnectionStrings__GestorGastos`): ver **Log stream** |
| "No se pudo cargar la vista previa" en Azure | `Storage__ConnectionString` sin `AccountKey` (no se pueden firmar SAS) |
| "Encountered an error (BadGateway) from host runtime" en la Function App | El worker se cae al arrancar: ver los `traces` de App Insights (p. ej. `TypeLoadException` por mezclar el SDK 3.x de Application Insights con `Microsoft.Azure.Functions.Worker.ApplicationInsights`) |
| Los comprobantes quedan en Procesando en Azure | Falta la suscripción de Event Grid, la Function se publicó en Debug o el `code` del webhook no es `blobs_extension` |
| `dotnet ef` no conecta a Azure SQL | La IP actual no está en el firewall |
| La base no responde a fin de mes | Se agotó el cupo gratuito mensual y quedó pausada hasta el mes siguiente |
