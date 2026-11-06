# GestorGastos — contexto para agentes

TP PW3 (UNLaM) "Gestión de Reembolsos de Gastos con Azure". Los **empleados** cargan recibos y
facturas; el archivo se guarda en Blob Storage y una **Azure Function** extrae sus datos con Azure AI
Document Intelligence; un **administrador** los toma en revisión, aprueba o rechaza y marca como pagados.
Todo con login de Microsoft Entra ID (tenant propio del equipo) y roles `Empleado` / `Administrador`.

Este archivo resume el enunciado, fija las decisiones de arquitectura y registra el estado del código.
**Mantenerlo actualizado** al implementar, renombrar, mockear o eliminar funcionalidades (sobre todo
las secciones 0 "Decisiones del equipo", 5 "Estado por funcionalidad" y 6 "Mocks y pendientes"). Es la única fuente de contexto
para agentes: `CLAUDE.md` solo lo importa (`@AGENTS.md`), así que los cambios se hacen acá.

**Idioma**: el agente se comunica con el equipo **en español** (respuestas, preguntas, planes, resúmenes,
mensajes de commit y descripciones de PR), además de escribir el código en español (sección 3).

**Qué es fijo y qué no**: lo único impuesto por la cátedra son las **tecnologías y servicios de Azure**
de la sección 1. El enunciado lo escribió el equipo: requisitos, modelo, reglas, pantallas y endpoints se
pueden cambiar si hace falta, registrando la decisión en la sección 0 y actualizando este archivo.

**Fuentes de verdad** (ante una duda, mandan en este orden):
1. Sección 0 de este archivo — decisiones del equipo posteriores al enunciado.
2. `docs/enunciado.md` — requisitos, reglas RN-01…RN-27, endpoints, criterios de aceptación.
3. `docs/prototipos.html` — prototipo interactivo (Bootstrap 5.3, selector de pantalla y de rol
   Empleado/Administrador, datos de ejemplo, simulación de extracción OK / error / demora). Cada
   sección trae un bloque "Notas para el desarrollo" con los componentes esperados.
4. `docs/prototipos/<NN>-<pantalla>.png` — capturas planas de cada pantalla:

| Pantalla | Capturas |
|---|---|
| P1 Login | `01-login.png` |
| P2 Dashboard | `02-dashboard.png` |
| P3 Mis comprobantes | `03-mis-comprobantes.png`, `03b-mis-comprobantes-modal-eliminar.png` |
| P4 Cargar | `04a-cargar-subida.png`, `04b-cargar-validaciones.png`, `04c-cargar-procesando.png`, `04d-cargar-resultado-exito.png`, `04e-cargar-resultado-error.png` |
| P5 Detalle | `05a-detalle-pendiente.png`, `05b-detalle-editar.png`, `05c-detalle-error-extraccion.png`, `05d-detalle-rechazado.png` |
| P6 Admin listado | `06a-admin-todos-los-comprobantes.png`, `06b-admin-modal-rechazar.png`, `06c-admin-modal-marcar-pagado.png` |
| P7 Admin detalle | `07a-admin-detalle-en-revision.png`, `07b-admin-detalle-comprobante-propio.png` |
| Acceso denegado | `08-acceso-denegado.png` |

---

## 0. Decisiones del equipo

Decisiones que cambian o precisan el enunciado o el prototipo. Prevalecen sobre el resto del documento;
al agregar una, reflejarla también en las secciones afectadas.

| Tema | Decisión | Impacto |
|---|---|---|
| Concurrencia entre administradores (RN-19) | Se asume que no hay concurrencia: sin validaciones adicionales ni control por `RowVersion` | `Comprobante` no tiene `RowVersion`; los POST de admin no envían versión; no se maneja `DbUpdateConcurrencyException` |
| Tests unitarios | No se hacen | No hay proyecto de tests |
| Login / logout | `CuentaController` propio: `IniciarSesion` (Challenge OIDC) y `CerrarSesion` (POST con antiforgery, vuelve a `/`). Sin `Microsoft.Identity.Web.UI` ni Razor Pages | P1 apunta a `Cuenta/IniciarSesion`, no a `MicrosoftIdentity/Account/SignIn` |
| Migraciones en Azure | La web las aplica al arrancar en **todos** los entornos (EF Core toma un lock en la BD durante `Migrate`); la Function no migra | No se corre `dotnet ef database update` contra producción; el usuario de BD de la web necesita permisos DDL |

## 1. Stack y comandos

Stack **obligatorio**, fijado por la cátedra (cualquier reemplazo de tecnología o servicio de Azure se acuerda antes con ella):

| Capa | Tecnología | Condiciones |
|---|---|---|
| Runtime | .NET 10 (`net10.0`) | Misma versión en la web y en la Function |
| Web | ASP.NET Core MVC + Razor Views | Controllers + Views. **No Razor Pages** |
| API | Controllers de Web API en el **mismo proyecto** web | Rutas bajo `/api`, JSON, `ProblemDetails` |
| Datos | EF Core Code First + SQL Server | Migraciones versionadas en el repo |
| BD | Azure SQL Database (oferta gratuita) | Local: LocalDB (Windows) o SQL Server en Docker (Linux/macOS) |
| Auth | Microsoft Entra ID, `Microsoft.Identity.Web` | App Registration single-tenant + App Roles |
| Archivos | Azure Blob Storage | Contenedor **privado** `comprobantes`. Local: Azurite |
| Procesamiento | Azure Functions, isolated worker | Blob Trigger (origen Event Grid en Azure) |
| IA documental | Azure AI Document Intelligence F0 | SDK `Azure.AI.DocumentIntelligence`, `prebuilt-receipt` / `prebuilt-invoice` |
| Monitoreo | Application Insights | Web y Function al mismo recurso |
| Hosting | Azure App Service F1 o B1 | + Function App |
| UI | Bootstrap 5.3 (+ Bootstrap Icons) | CSS propio mínimo y justificado |

Comandos:

```bash
dotnet build GestorGastos.slnx
docker compose --env-file .env up -d               # compose.yaml: SQL Server + Azurite (copiar .env.example a .env)
azurite --skipApiVersionCheck --location "$env:USERPROFILE\.azurite"   # Windows sin Docker: Azurite por npm (npm install -g azurite) + LocalDB
set -a && source .env && set +a                    # Linux/macOS: exportar GESTORGASTOS_DB_* en la terminal (VS Code lo hace con envFile)
dotnet run --project GestorGastos.Web              # aplica migraciones pendientes al arrancar (también en Azure)
cd GestorGastos.Functions && func start            # requiere Azure Functions Core Tools v4 y local.settings.json (copiar de local.settings.example.json)
# Migraciones: DbContext en Entidades, configuración en Web
dotnet ef migrations add <NombreEnPascal> --project GestorGastos.Entidades --startup-project GestorGastos.Web
dotnet ef database update --project GestorGastos.Entidades --startup-project GestorGastos.Web
# Secretos locales (nunca en appsettings versionado)
dotnet user-secrets set "AzureAd:ClientSecret" "<valor>" --project GestorGastos.Web
```

- Conexión a BD (`Entidades/ConexionBaseDatos.Obtener`, la usan `Web/Program.cs` y `Functions/Program.cs`):
  si `ConnectionStrings:GestorGastos` tiene valor (Azure) se usa esa; si está vacía (local), sin `GESTORGASTOS_DB_HOST`
  usa LocalDB `(localdb)\MSSQLLocalDB/GestorGastos` (Windows) y con ella arma la cadena con
  `GESTORGASTOS_DB_HOST/PORT/NAME/USER/PASSWORD` (Docker, Linux/macOS). Fuera de Windows y sin ninguna de las dos falla al arrancar.
  En local **no** se guarda la connection string en User Secrets ni en `local.settings.json` (si estuviera, gana sobre lo anterior).
- `.env` (copiado de `.env.example`, solo Linux/macOS): lo usan `docker compose` y la web/Function para conectarse.
  La contraseña de `sa` tiene que cumplir la política de SQL Server (8+ caracteres con mayúsculas, minúsculas y
  números o símbolos). En la terminal hay que exportarlo (`set -a && source .env && set +a`) antes de `dotnet run`,
  `dotnet ef` o `func start`; el launch de VS Code lo carga solo (`envFile`, solo en Linux).
- `GestorGastosDbContext` recibe las opciones por constructor (no tiene `OnConfiguring`): la connection string
  la registran `Web/Program.cs` y `Functions/Program.cs` con `AddDbContext`. Por eso las migraciones usan
  `--startup-project GestorGastos.Web` (`migrations add` no se conecta).
- Migraciones: `Web/Program.cs` corre `MigrateAsync()` al arrancar en todos los entornos (ver 0), así que en Azure
  se aplican solas al desplegar. El usuario de la connection string de la web necesita permisos DDL (`db_owner`).
- `UseSqlServer` con `EnableRetryOnFailure()` (web y Function): la Azure SQL gratuita se auto-pausa y la primera
  conexión después de la pausa puede fallar o tardar ~1 min.
- `System.Security.Cryptography.Pkcs` está fijado en `Entidades` para pisar una versión transitiva vulnerable (NU1903).
- Application Insights se registra solo si `APPLICATIONINSIGHTS_CONNECTION_STRING` no está vacío (web y Function).

### Configuración (solo nombres; los valores van en User Secrets / App Settings / Key Vault)

| Clave | Web | Function | Notas |
|---|---|---|---|
| `ConnectionStrings:GestorGastos` | ✔ | ✔ | Solo Azure. Local: vacía → LocalDB o `GESTORGASTOS_DB_*` (ver arriba) |
| `AzureAd:Instance`, `TenantId`, `ClientId`, `ClientSecret`, `CallbackPath` | ✔ | — | `CallbackPath` = `/signin-oidc` |
| `Storage:ConnectionString`, `Storage:Contenedor` (= `comprobantes`) | ✔ | ✔ (`AzureWebJobsStorage` + conexión del trigger) | Local: `UseDevelopmentStorage=true` (Azurite) |
| `DocumentIntelligence:Endpoint`, `DocumentIntelligence:Key` | — | ✔ | Vacíos → extractor mock (ver 6) |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | ✔ | ✔ | Vacío → solo logs de consola |

- **Ningún secreto en el repositorio.** `local.settings.json` de la Function y `.env` no se versionan
  (sí `local.settings.example.json` y `.env.example` con claves vacías).
- CI (`.github/workflows/ci.yml`, en push a `main` y en PRs a `main`): restore → build Release de `GestorGastos.slnx`
  (`actions/checkout@v7`, `actions/setup-dotnet@v6`). No corre tests (ver 0) ni despliega.
- Commits: Conventional Commits en español (`feat:`, `fix:`, `chore:`, `docs:`), ramas + PR a `main`.

## 2. Arquitectura

```
Navegador ──HTML / fetch──▶ GestorGastos.Web (Controllers MVC + Controllers API /api)
                                    │
                                    ▼
GestorGastos.Functions ──▶ GestorGastos.Servicios ──▶ IAlmacenamientoArchivos ──▶ Blob "comprobantes"
          ▲                         │                 IExtractorDocumentos    ──▶ Document Intelligence
          │                         ▼                                                  │
          │              GestorGastos.Repositorios                                     │
          │                         ▼                                                  │
          │      GestorGastos.Entidades (DbContext) ──▶ Azure SQL                      │
          │                                                                            │
          └──────────── Blob Trigger (Event Grid) ◀── blob nuevo {GUID}.{ext} ─────────┘ (contenedor)

GestorGastos.DTOs: enums + DTOs, referenciado por todos.  Application Insights: Web + Function.
```

| Proyecto | Contenido | Referencia a |
|---|---|---|
| `GestorGastos.DTOs` | DTOs de entrada/salida (API y formularios), `Enums/` (`EstadoComprobante`, `TipoDocumento`, `Categoria`, `Moneda`), `Validaciones/` (atributos custom: `FechaNoFutura`, `MaximoDosDecimales`), `Helpers/` (`EtiquetasHelper`, `FechaHelper`) | — |
| `GestorGastos.Entidades` | Entidades EF, `GestorGastosDbContext`, `Migrations/` | DTOs (enums) |
| `GestorGastos.Repositorios` | `Interfaces/I*Repositorio` + implementación; **único** lugar con queries EF | Entidades |
| `GestorGastos.Servicios` | Lógica de negocio compartida web + Function: `ComprobanteServicio` (máquina de estados, historial, RN), `UsuarioServicio`, `ValidadorArchivo` (magic bytes), abstracciones `IAlmacenamientoArchivos` e `IExtractorDocumentos` con sus implementaciones, mapeo entidad→DTO | Repositorios, Entidades, DTOs |
| `GestorGastos.Web` | `Controllers/` (MVC), `Controllers/Api/` (API), `Areas/Admin/` (P6/P7), `Views/`, `Models/` (ViewModels de vistas), `Helpers/`, `wwwroot/js`, `wwwroot/css/site.css`, `Program.cs` (DI, auth, App Insights) | Servicios, DTOs (+ Entidades solo para el `DbContext` en DI) |
| `GestorGastos.Functions` | `ExtraerComprobanteFunction` (Blob Trigger) y su `Program.cs` (DI, App Insights) | Servicios, DTOs (+ Entidades solo para el `DbContext` en DI) |

No hay proyecto de tests (ver 0).

### Flujo de carga y extracción (§3 del enunciado)
1. P4 envía el formulario (`multipart/form-data`) a `ComprobantesController.Cargar [POST]` → valida
   (Data Annotations + `ValidadorArchivo`) → `ComprobanteServicio.CargarAsync`: crea el comprobante en
   **Procesando** + historial (`null → Procesando`) y **después** sube el blob `{GUID}.{ext}`. Si la
   subida falla, borra el registro. Redirige a la etapa 2 con el id.
2. El blob nuevo dispara la Function (en Azure, Blob Trigger con `Source = EventGrid`; el clásico demora minutos).
3. La Function busca por `BlobNombre`; si el comprobante ya no está en Procesando, no hace nada (RN-27).
   Elige modelo por `TipoDocumento` (RN-21) y llama a `IExtractorDocumentos`.
4. Guarda los datos y pasa a **PendienteRevision**, o a **ErrorExtraccion** con `MensajeError`
   (RN-25). Siempre registra historial con `UsuarioId = null`.
5. La vista consulta `GET /api/comprobantes/{id}/estado` cada 3 s, máximo 2 minutos.

### Reglas duras
- Controllers (MVC y API) **sin lógica de negocio**: validan `ModelState`, obtienen el usuario del token
  y delegan en `GestorGastos.Servicios`. Las transiciones se validan solo en `ComprobanteServicio`.
- Controllers → Servicios → Repositorios. La web y la Function usan **los mismos** servicios (no duplicar reglas en la Function).
- Permisos **solo** desde el token: `oid` (`User.GetObjectId()`) y App Roles. Nunca ids/roles enviados por el cliente.
- `[Authorize]` global: la fallback policy exige sesión **y** rol `Empleado` o `Administrador` (sin rol → Forbid →
  `AccessDeniedPath` = `/Cuenta/AccesoDenegado`). Excepciones: `HomeController.Index` (P1/P2, chequea el rol a mano con
  `User.TieneRol()`), `Cuenta/IniciarSesion` y `Cuenta/AccesoDenegado` son `[AllowAnonymous]`; `Cuenta/CerrarSesion` es
  `[Authorize]` (solo sesión, para que un usuario sin rol pueda salir). `[Authorize(Roles = Roles.Administrador)]`
  (`Web/Helpers/Roles.cs`, que también define `Roles.EmpleadoOAdmin` = `"Empleado,Administrador"`, hoy sin usos)
  en el área Admin y en `/api/admin/...`. Ocultar botones **no** reemplaza la validación del backend.
- Datos del usuario logueado en vistas y en el alta: extensiones de `Web/Helpers/ClaimsPrincipalHelper.cs`
  (`User.Nombre()`, `User.Email()`, `User.Rol()`, `User.TieneRol()`), importadas en `_ViewImports`.
- Toda consulta de empleado filtra por `UsuarioId` del usuario logueado. Recurso ajeno → **404**.
- Un administrador **no** cambia el estado de sus propios comprobantes (RN-15) → **403**, validado en el servicio.
- Errores de API con `ProblemDetails` (RFC 9457) y mensaje en español. Códigos: 400 inválido, 401 sin sesión,
  403 sin permiso / admin sobre propio, 404 inexistente o ajeno, 409 transición inválida.
- POST y DELETE de API exigen antiforgery en el header `RequestVerificationToken`; formularios con `asp-antiforgery`.
- HTTPS obligatorio. El navegador **nunca** recibe la connection string: solo URLs SAS de 5 minutos.
- async/await en todo I/O (EF, Blob, Document Intelligence).

## 3. Convenciones de código

- Todo en **español** (clases, métodos, variables, mensajes, comentarios), sin tildes ni ñ en
  identificadores (`TamanioBytes`, `Categoria.Viaticos`, `EstadoComprobante.ErrorExtraccion`).
  Sufijos: `*Servicio`, `*Repositorio`, `*Dto`, `*ViewModel`, `*Attribute`, `*Function`; métodos async
  terminan en `Async` (`ListarPorUsuarioAsync`, `ObtenerPorIdAsync`, `AgregarAsync`, `GuardarCambiosAsync`).
- Namespaces con llaves (no file-scoped), campos `_camelCase` inyectados por constructor,
  colecciones `new List<T>()`, strings `= string.Empty`, navegaciones `= null!`.
- **Código autodocumentado**: los nombres de clases, métodos y variables tienen que explicar qué hace el
  código. Comentarios **solo cuando sean estrictamente necesarios**, en dos casos:
  1. El código es tan complejo, o depende de una restricción tan poco visible (p. ej. una limitación de
     SQL Server o de un SDK), que no se entiende por sí solo.
  2. Tareas pendientes: `// TODO <Área>: <qué falta>`, con áreas `Autenticación`, `Almacenamiento`,
     `Extracción`, `Monitoreo`, `Despliegue`. Buscar con `grep -rn "TODO" --include=*.cs --include=*.cshtml --include=*.js`.

  No agregar comentarios que repitan lo que el código ya dice ni que narren cada paso o sección
  (`// Registrar servicios`, `// Login con Entra ID`), tampoco en vistas, csproj, YAML, `compose.yaml`
  ni archivos de ejemplo. La documentación de uso y configuración va en este archivo o en el README.
  Única excepción impuesta por el enunciado: cada regla de `site.css` lleva un comentario que la justifica.
- Validaciones de entrada: Data Annotations en el DTO con `ErrorMessage` en español y voseo, usando
  los mensajes sugeridos del enunciado ("Seleccioná un archivo.", "El archivo supera los 4 MB.").
  Reglas que dependen de BD o del estado (dueño, transición) van en el servicio.
  En el cliente: jQuery Validation Unobtrusive con los mismos atributos (+ adaptadores para los custom en
  `wwwroot/js/validacion-bootstrap.js`: `fechanofutura`, `dosdecimales`, y marca `is-invalid` de Bootstrap). Los
  atributos HTML `min`/`max`/`step` también los valida jQuery Validation en inglés: ponerles `data-msg-min`/`-max`/`-step`.
  Con `hidden` no se oculta un elemento que tiene `d-flex` (Bootstrap usa `!important`): poner `hidden` en un contenedor.
- Propiedades numéricas/fecha obligatorias de DTOs de entrada: nullable + `[Required]` (`decimal?`, `DateOnly?`).
- Enums: valores desde 1, guardados como **string** (`HasConversion<string>()`). Texto visible y clase
  de badge por extensiones en `GestorGastos.DTOs/Helpers/` (namespace `GestorGastos.DTOs.Helpers`, importado en los
  `_ViewImports` de Views y del área Admin), compartidas por web, servicios y validaciones:
  `EtiquetasHelper`: `EstadoComprobante.Texto()` ("Pendiente de Revisión"), `EstadoComprobante.ClaseBadge()`,
  `Categoria.Texto()` ("Viáticos"), `TipoDocumento.Texto()`, `decimal?.Moneda(moneda)` ("$ 12.345,67" o "—");
  `FechaHelper`: `DateTime.ConvertirAArgentina()`, `FechaLocal()` / `FechaHoraLocal()` ("28/10/2026 14:32").
  No duplicarlas en `Web/Helpers` (ahí quedan solo los helpers que dependen de ASP.NET: claims, roles, `Resultado`).
- Fechas: se guardan en **UTC** (`DateTime.UtcNow`, asignadas por el servidor) y se muestran en hora
  de Argentina (zona `America/Argentina/Buenos_Aires`, UTC−3) solo en los helpers de vista.
  Fechas sin hora (`FechaComprobante`, `FechaPago` del formulario) como `DateOnly`.
- Montos `decimal(18,2)`; cultura `es-AR` en vistas. Moneda por defecto `ARS`.
- Mapeo entidad→DTO: `private static XDto ADto(X entidad)` dentro del servicio.
- Resultados de servicio: un único tipo (p. ej. `Resultado<T>` con `TipoError { NoEncontrado, Prohibido,
  Conflicto, Invalido }` + mensaje) que un helper del controller traduce a 404/403/409/400 `ProblemDetails`.
  No lanzar excepciones para errores de negocio esperados. Helpers en `Web/Helpers/`: en API,
  `this.ComoProblema(resultado)` (`ResultadoHelper`, `ProblemDetails` 404/403/409/400); en MVC,
  `this.ComoVista(resultado)` (`ResultadoMvcHelper`: `NotFound()` / `Forbid()` / `BadRequest()`; los errores
  `Invalido`/`Conflicto` de un formulario se muestran con `ModelState.AddModelError` en vez de usarlo).
- Repositorios: listados con `AsNoTracking()` y paginación en BD (`Skip/Take`, 10 por página);
  `ObtenerPorIdAsync` con tracking e `Include(Historial)` para modificar y `GuardarCambiosAsync()`.
- Vistas: Bootstrap 5.3 tal como en el prototipo (navbar `navbar-expand-md`, `card`, `table table-hover align-middle`
  dentro de `table-responsive`, `thead.table-light`, `form-select-sm` para filtros, `btn-group btn-group-sm`
  para acciones de fila, `modal`, `toast` abajo a la derecha, `breadcrumb`, `pagination-sm`, `spinner-border`).
  Botón principal `btn-primary`, secundario `btn-outline-secondary`, destructivo `btn-outline-danger` / `btn-danger`.
  Badges de estado **siempre** por `ClaseBadge()` (tabla en 7.6). Partial `_BadgeEstado.cshtml`.
- JS por pantalla en `wwwroot/js/<pantalla>.js` (IIFE, vanilla, `fetch` con header `RequestVerificationToken`
  leído del `@Html.AntiForgeryToken()` del layout). CSS propio solo si Bootstrap no lo resuelve,
  en `wwwroot/css/site.css`, **cada regla con un comentario que la justifica** (p. ej. alto fijo de la previsualización).
- Responsive: probar cada pantalla a 375 px de ancho.
- Logging: `ILogger<T>` con plantillas estructuradas (`"Comprobante {ComprobanteId} pasó de {Anterior} a {Nuevo}"`)
  en: carga, inicio/fin de extracción con duración, error de extracción y cada cambio de estado.

## 4. Modelo de datos (`GestorGastos.Entidades`)

**Usuario** — alta en el primer login y actualización (nombre, email, último acceso) en los siguientes
(evento `OnTokenValidated` de OpenID Connect en `Web/Program.cs` → `UsuarioServicio.RegistrarAccesoAsync`, con `oid`,
claim `name` y `preferred_username`).

| Campo | Tipo | Reglas |
|---|---|---|
| Id | `string` PK | `oid` de Entra ID |
| Nombre | nvarchar(200) | Requerido |
| Email | nvarchar(256) | Requerido |
| UltimoAcceso | datetime2 | Requerido, UTC |

**Comprobante**

| Campo | Tipo | Reglas |
|---|---|---|
| Id | int PK identity | |
| UsuarioId | string FK | Dueño |
| NombreArchivoOriginal | nvarchar(255) | Tal como lo subió el usuario |
| BlobNombre | nvarchar(100) | `{GUID}.{ext}`; nunca el nombre original. Índice único recomendado |
| ContentType | nvarchar(50) | `application/pdf`, `image/jpeg`, `image/png` |
| TamanioBytes | bigint | ≤ 4 MB |
| TipoDocumento | enum (string) | `Recibo`, `Factura` |
| Categoria | enum (string) | `Viaticos`, `Pasajes`, `Hospedaje`, `Traslados`, `Otros` |
| Comercio | nvarchar(200) null | Extraído |
| FechaComprobante | date null | Extraída; no futura |
| Descripcion | nvarchar(500) null | Ítems concatenados con `"; "` |
| Monto | decimal(18,2) null | Total |
| Moneda | char(3) null | ISO 4217; `ARS` si no se detecta |
| ConfianzaDescripcion, ConfianzaMonto | float null | 0 a 1 |
| EditadoManualmente | bit | `true` si el empleado cambió algún dato extraído |
| Estado | enum (string) | Ver 7.2 |
| MensajeError | nvarchar(500) null | Motivo técnico de la falla de extracción |
| MotivoRechazo | nvarchar(500) null | Obligatorio si Rechazado |
| FechaCarga | datetime2 | Servidor, UTC |
| FechaPago | datetime2 null | Obligatoria si Pagado |

**HistorialEstado**

| Campo | Tipo | Reglas |
|---|---|---|
| Id | int PK identity | |
| ComprobanteId | int FK | **Cascade** al borrar el comprobante |
| EstadoAnterior | enum (string) null | Nulo en el primer registro |
| EstadoNuevo | enum (string) | Requerido |
| UsuarioId | string null | Nulo si lo hizo la Function |
| Fecha | datetime2 | Servidor, UTC |
| Observacion | nvarchar(500) null | Motivo de rechazo, fecha de pago u otra nota |

Índices: `(UsuarioId, Estado)` y `FechaCarga` en `Comprobante`.
`Usuario.Id` es nvarchar(36) (largo de un `oid`); `HistorialEstado.UsuarioId` es FK nullable a `Usuario` con
`Restrict` (evita dos caminos de cascada). Enums como texto por convención en `ConfigureConventions`
(nvarchar(30); `Moneda` char(3)), decimales (18,2).
Migraciones: `<ts>_Inicial` (`20261101174953_Inicial`). No editar migraciones ya mergeadas en `main`.

## 5. Estado por funcionalidad

Leyenda: ✅ hecho · 🟡 parcial/mock · ❌ no existe. Actualizar en cada PR.

| # | Pantalla / recurso | MVC (acción) | API | Estado |
|---|---|---|---|---|
| — | Solución, proyectos y DI base | — | — | ✅ `GestorGastos.slnx` con los 6 proyectos; DbContext, ProblemDetails y App Insights registrados en web y Function; `.vscode` (build + launch Web) |
| — | `compose.yaml` (SQL Server + Azurite), `.env.example`, `local.settings.example.json` | — | — | ✅ |
| — | Modelo + migración inicial | — | — | ✅ `Usuario`, `Comprobante`, `HistorialEstado` + enums en DTOs; migración `Inicial` (la web aplica las migraciones al arrancar, en todos los entornos) |
| — | Auth Entra ID (OIDC, App Roles, fallback policy) + alta/actualización de `Usuario` | — | — | ✅ `AddMicrosoftIdentityWebApp` con `OnTokenValidated` → `UsuarioServicio.RegistrarAccesoAsync` (`UsuarioRepositorio`), `AccessDeniedPath`, fallback policy con rol; login/logout en `CuentaController` |
| — | Layout con navbar (Dashboard, Mis comprobantes, Cargar, Administración solo admin; usuario → rol, email, Cerrar sesión) | `_Layout.cshtml` | — | ✅ Navbar completa según rol (solo con sesión) + `@Html.AntiForgeryToken()` para los `fetch`. Cargar marca `active` en `Cargar`/`Resultado`/`Editar`. Mis comprobantes da 404 hasta que exista P3 y Administración falla hasta que exista la vista de P6 (ver 6) |
| P1 | Bienvenida / Login (redirige a Dashboard si hay sesión) | `HomeController.Index` `[AllowAnonymous]` sin sesión → vista `Bienvenida` | — | ✅ Botón → `Cuenta/IniciarSesion` |
| — | Acceso denegado (sin rol o Empleado en `/Admin`) | `CuentaController.AccesoDenegado` | — | ✅ Responde 403; Cerrar sesión solo si hay sesión |
| P2 | Dashboard (abiertos + desglose + alerta de errores) | `HomeController.Index` con sesión (sin rol → Acceso denegado) | — | 🟡 Placeholder: solo "Hola, <nombre>" y un aviso de que no está disponible |
| P3 | Mis comprobantes (filtro estado, orden, paginación 10, eliminar con modal, polling de Procesando) | `ComprobantesController.Index` | `GET api/comprobantes/{id}/estado`, `DELETE api/comprobantes/{id}` | ❌ Listado MVC pendiente. Los dos endpoints de API ya existen (se hicieron con P4) y se pueden reusar tal cual |
| P4 | Cargar — etapa 1 (archivo, tipo, categoría, miniatura, Quitar) | `ComprobantesController.Cargar` GET/POST | — | ✅ `CargarComprobanteViewModel` + `Views/Comprobantes/Cargar.cshtml` + `wwwroot/js/cargar-comprobante.js` (valida extensión y 4 MB al elegir, miniatura o ícono PDF, aviso de PDF > 2 páginas, Quitar). Servidor: `ComprobanteServicio.CargarAsync` → `ValidadorArchivo` → registro en Procesando + historial → blob `{GUID}.{ext}` (si falla la subida borra el registro y muestra "No pudimos guardar el archivo…") |
| P4 | Cargar — etapa 2 (polling 3 s / 2 min, resultado, edición, Revisar < 0,8) | `ComprobantesController.Resultado/{id}` + `Editar [POST]` | `GET api/comprobantes/{id}/estado` | ✅ `Views/Comprobantes/Resultado.cshtml` + `wwwroot/js/resultado-carga.js`: en Procesando consulta el estado cada 3 s y recarga la vista cuando cambia; a los 2 min muestra el aviso de demora. Formulario `EditarComprobanteDto` (prefijo `Formulario`) con badge Revisar (partial `_BadgeRevisar`), Eliminar con modal (`DELETE api/comprobantes/{id}`) y toast de éxito. `ComprobanteServicio.EditarAsync` sirve también para P5 (Error → Pendiente con historial, `EditadoManualmente` solo si cambió un dato extraído). `EstadoComprobanteDto` serializa los enums como texto (`"estado": "Procesando"`) |
| P5 | Detalle (datos, preview SAS, descargar, editar, eliminar, historial) | `ComprobantesController.Detalle/{id}`, `Editar [POST]` | `GET api/comprobantes/{id}/archivo?descarga=` | ❌ `Editar [POST]` y `EditarAsync` ya existen (P4); hoy redirigen a `Resultado/{id}` |
| P6 | Todos los comprobantes (filtros estado + empleado, orden, paginación, acciones por estado, modales) | `Admin/ComprobantesController.Index` | `POST api/admin/comprobantes/{id}/tomar\|aprobar\|rechazar\|pagar` | 🟡 API de acciones implementada (`Api/AdminComprobantesController` + `ComprobanteServicio.TomarEnRevisionAsync`/`AprobarAsync`/`RechazarAsync`/`MarcarPagadoAsync`). `Admin/ComprobantesController.Index` devuelve `View()` pero **no existe** `Areas/Admin/Views/Comprobantes/Index.cshtml`: hoy `/Admin/Comprobantes` lanza una excepción (vista no encontrada) |
| P7 | Detalle administración (empleado nombre + email, acciones de estado, sin editar/eliminar) | `Admin/ComprobantesController.Detalle/{id}` | ídem P6 + `archivo` | 🟡 Vista y acción de detalle implementadas; falta preview/descarga y completar integración de acciones |
| — | Timeout: Procesando > 5 min → ErrorExtraccion al consultar estado | — | dentro de `GET .../estado` | ✅ `ComprobanteServicio.ObtenerEstadoAsync` (lo usan la API y `Resultado/{id}`): pasa a ErrorExtraccion con "El análisis tardó demasiado…" e historial con `UsuarioId = null` |
| — | Validación de archivo (extensión, magic bytes, 4 MB, aviso PDF > 2 páginas) | — | — | ✅ `Servicios/ValidadorArchivo` (estático): RN-01 a RN-04 con los mensajes del enunciado; RN-06 cuenta `/Type /Page` sin descomprimir el PDF (si las páginas están en object streams comprimidos no avisa). El cliente replica extensión, tamaño y conteo de páginas |
| — | Function de extracción (receipt/invoice, idempotente, historial) | — | — | ❌ |
| — | Vista de error genérica (sin detalles técnicos) | `HomeController.Error` | `ProblemDetails` | ✅ Vista `Error` genérica + `UseExceptionHandler` fuera de Development + `AddProblemDetails`; `Resultado<T>` se traduce con `ComoProblema` (API) y `ComoVista` (MVC), ver 3 |
| — | Almacenamiento de archivos (Blob Storage) | — | — | 🟡 `Servicios/AlmacenamientoArchivos` (singleton en `Web/Program.cs`): `SubirAsync` (crea el contenedor si no existe), `EliminarAsync` (`DeleteIfExistsAsync`), `GenerarUrlLecturaAsync` → `UrlArchivoDto` (SAS de lectura de 5 min sobre un blob; con `nombreDescarga` fija `Content-Disposition: attachment`). Carga y eliminación ya lo usan; falta el endpoint `GET api/comprobantes/{id}/archivo` (P5/P7), único consumidor de `GenerarUrlLecturaAsync`. No está registrado en la Function |
| — | Application Insights + logs de 7.6 en web y Function | — | — | ❌ |
| — | Despliegue (App Service, Function App, Azure SQL, Storage, Event Grid, DI F0) | — | — | ❌ |
| — | README (diagrama, recursos, App Registration + App Roles, variables, correr local, desplegar) | — | — | ❌ |
| — | 3 usuarios de prueba en el tenant (2 Empleado, 1 Administrador) | — | — | ❌ |
| — | Captura de App Insights con una extracción completa | — | — | ❌ |
| — | Opcional: CI/CD (GitHub Actions) web + Function | — | — | 🟡 CI ✅ (compila la solución en push y PR a `main`); falta el despliegue (CD) |
| — | Opcional: Managed Identity en lugar de secretos | — | — | ❌ |

## 6. Mocks y pendientes de integración

Cada servicio externo se usa a través de una interfaz en `GestorGastos.Servicios`; la implementación se
elige por configuración en `Program.cs` (web y Function), nunca con `if` en la lógica de negocio.

| Dependencia | Abstracción | Local / mock | Azure | Cómo se elige |
|---|---|---|---|---|
| Blob Storage | `IAlmacenamientoArchivos` (`SubirAsync`, `EliminarAsync` tolerando inexistente, `GenerarUrlLecturaAsync(blob, nombreDescarga?, 5 min)`) | Azurite (mismo SDK; SAS funciona con la cuenta de desarrollo) | Azure Storage con connection string que incluya la clave de la cuenta (la SAS se firma con ella). Managed Identity + User Delegation SAS requeriría cambiar `AlmacenamientoArchivos` | `Storage:ConnectionString` + `Storage:Contenedor` (vacías → excepción al resolver el servicio) |
| Document Intelligence | `IExtractorDocumentos` (`ExtraerAsync(stream, TipoDocumento)` → comercio, fecha, ítems, monto, moneda, confianzas) | `ExtractorDocumentosMock`: determinístico; si el nombre del blob/archivo original contiene `error` o `borroso` falla con "No se detectó el monto total del comprobante."; si no, devuelve datos de ejemplo con `ConfianzaDescripcion` < 0,8 | `ExtractorDocumentIntelligence` con `prebuilt-receipt` / `prebuilt-invoice` | `DocumentIntelligence:Endpoint` vacío → mock |
| Disparo de la Function | Blob Trigger | Azurite + `func start` (trigger clásico, polling) | Blob Trigger `Source = EventGrid` + suscripción Event Grid | Configuración del trigger / App Settings |
| Base de datos | `GestorGastosDbContext` | LocalDB (Windows) o SQL Server en Docker (`GESTORGASTOS_DB_HOST`) | Azure SQL Database free | `ConnectionStrings:GestorGastos` (vacía → local, `ConexionBaseDatos`) |
| Autenticación | `Microsoft.Identity.Web` | **Real** (tenant del equipo), también en local | ídem | `AzureAd:*` |
| Monitoreo | `ILogger` + App Insights | Consola | Application Insights | `APPLICATIONINSIGHTS_CONNECTION_STRING` |

Pendientes / mocks activos (agregar una fila por cada mock o integración a medias, con su `TODO <Área>`):

| Qué | Dónde | TODO |
|---|---|---|
| Dashboard (P2) placeholder (sin contadores ni alerta de errores) | `Web/Views/Home/Index.cshtml` | — |
| Links de la navbar con hrefs fijos (`/Comprobantes`, `/Comprobantes/Cargar`, `/Admin/Comprobantes`): pasarlos a `asp-*` cuando existan P3 y P6 | `Web/Views/Shared/_Layout.cshtml` | — |
| Listado de administración (P6) sin vista: `/Admin/Comprobantes` falla. El TODO no sigue el formato `TODO <Área>` de la sección 3 | `Web/Areas/Admin/Controllers/ComprobantesController.cs` | `TODO P6 (Federico)` |
| P4 enlaza a pantallas de otros: Cancelar, breadcrumb y Eliminar van a `/Comprobantes` (P3) e "Ir al detalle" a `/Comprobantes/Detalle/{id}` (P5); dan 404 hasta que existan | `Web/Views/Comprobantes/*.cshtml`, `wwwroot/js/resultado-carga.js` | — |
| `ComprobanteServicio` depende de `IAlmacenamientoArchivos`: al registrarlo en la Function, registrar también el almacenamiento | `Functions/Program.cs` | `TODO Extracción` |
| Preview y descarga del archivo en el detalle administrativo | `Web/Areas/Admin/Views/Comprobantes/Detalle.cshtml` | `TODO Almacenamiento` |
| `/api`: sin sesión o sin rol hoy redirige (a Entra ID / Acceso denegado); debería responder 401/403 `ProblemDetails`. Resolver con los eventos `OnRedirectToIdentityProvider` (OIDC) y `OnRedirectToAccessDenied` (cookie) al crear el primer controller API | `Web/Program.cs` | — |
| `ExtraerComprobanteFunction` y registro de servicios compartidos | `Functions/Program.cs` | `TODO Extracción` |

## 7. Requisitos funcionales (resumen del enunciado)

Alcance: login Entra ID con roles, carga + Blob + extracción por Function, listados, detalle con
preview y descarga, edición de datos extraídos, eliminación, flujo de aprobación con historial,
dashboard de documentos abiertos, App Insights.
**Fuera de alcance**: dashboard analítico, integración con pagos, emails, **tests unitarios**.

### 7.1 Autenticación y roles
- Todo requiere sesión salvo la bienvenida. Usuario autenticado **sin rol** → Acceso denegado en cualquier pantalla.
- App Registration single-tenant con App Roles `Empleado` y `Administrador`, asignados desde la Enterprise Application.

| Acción | Empleado | Administrador |
|---|---|---|
| Dashboard propio | Sí | Sí |
| Cargar, editar y eliminar comprobantes propios | Sí | Sí |
| Ver comprobantes propios | Sí | Sí |
| Ver comprobantes de todos | No | Sí |
| Cambiar estado | No | Sí, **excepto los propios** |

### 7.2 Estados y transiciones (enum `EstadoComprobante`)
`Procesando`, `ErrorExtraccion`, `PendienteRevision`, `EnRevision`, `Aprobado`, `Rechazado`, `Pagado`.
**Abiertos** = Procesando, ErrorExtraccion, PendienteRevision, EnRevision. **Finales** = Rechazado, Pagado.

| Transición | Quién | Condición |
|---|---|---|
| Procesando → PendienteRevision | Function | Se extrajeron monto y descripción |
| Procesando → ErrorExtraccion | Function, o la web si lleva > 5 min al consultar el estado | Falla del servicio o sin monto |
| ErrorExtraccion → PendienteRevision | Empleado dueño | Completa monto y descripción válidos ("Guardar y enviar a revisión") |
| PendienteRevision → EnRevision | Administrador | No es dueño |
| EnRevision → Aprobado | Administrador | No es dueño |
| EnRevision → Rechazado | Administrador | Motivo obligatorio. Final |
| Aprobado → Pagado | Administrador | Fecha de pago obligatoria. Final |

Cualquier otra → **409**. En PendienteRevision el empleado aún edita/elimina; desde EnRevision queda
bloqueado para él. Un rechazado no se reabre (se carga uno nuevo). Cada cambio crea `HistorialEstado`.
Nota: RN-24 deja la descripción vacía si no hay ítems; como la transición a PendienteRevision exige
monto **y** descripción, sin descripción la Function deja el comprobante en ErrorExtraccion para que el empleado la complete.

### 7.3 Reglas de negocio
**Archivo**

| Código | Regla | Mensaje |
|---|---|---|
| RN-01 | Archivo obligatorio | "Seleccioná un archivo." |
| RN-02 | Extensión `.pdf`, `.jpg`, `.jpeg`, `.png` | "Formato no permitido. Usá PDF, JPG o PNG." |
| RN-03 | Contenido coincide con extensión por **magic bytes** (`%PDF`, `FF D8 FF`, `89 50 4E 47`), no por header | "El contenido del archivo no corresponde a su formato." |
| RN-04 | Máximo 4 MB (límite F0) | "El archivo supera los 4 MB." |
| RN-05 | Un único archivo por comprobante | — |
| RN-06 | PDF > 2 páginas se acepta con aviso | "Solo se analizarán las primeras 2 páginas." |

**Datos**

| Código | Regla |
|---|---|
| RN-07 | Tipo de documento y categoría obligatorios al cargar ("Elegí el tipo de documento." / "Elegí una categoría.") |
| RN-08 | Monto obligatorio para salir de Error; > 0 y ≤ 99.999.999,99; dos decimales ("Ingresá un monto mayor a 0, con hasta 2 decimales.") |
| RN-09 | Descripción obligatoria para salir de Error; 3 a 500 caracteres ("Escribí una descripción de entre 3 y 500 caracteres.") |
| RN-10 | Fecha del comprobante opcional, no futura ("La fecha no puede ser futura.") |
| RN-11 | Moneda ∈ `ARS`, `USD`, `EUR`, `BRL`, `CLP`, `UYU` |
| RN-12 | Editar un dato extraído pone `EditadoManualmente = true` (solo si algo cambió) |

**Estados y permisos**

| Código | Regla |
|---|---|
| RN-13 | Solo transiciones de 7.2; otra → 409 |
| RN-14 | Empleado edita/elimina solo propios en PendienteRevision o ErrorExtraccion |
| RN-15 | Administrador no cambia el estado de sus comprobantes |
| RN-16 | Rechazar exige motivo de 10 a 500 caracteres → `MotivoRechazo` + `Observacion` del historial |
| RN-17 | Pagar exige fecha no futura y no anterior a la fecha de carga |
| RN-18 | Todo cambio de estado (incluida la Function) crea `HistorialEstado` |
| RN-19 | **No se implementa** (decisión del equipo): se asume que no hay concurrencia entre administradores, sin `RowVersion` ni control de concurrencia optimista |
| RN-20 | Eliminar borra primero el blob y después el registro; si el blob no existe, borra igual el registro |

**Extracción**

| Código | Regla |
|---|---|
| RN-21 | `Recibo` → `prebuilt-receipt`; `Factura` → `prebuilt-invoice` |
| RN-22 | Recibo: `MerchantName`, `TransactionDate`, `Total`, descripciones de `Items` |
| RN-23 | Factura: `VendorName`, `InvoiceDate`, `InvoiceTotal`, descripciones de `Items` |
| RN-24 | Sin ítems → descripción vacía (la completa el empleado) |
| RN-25 | Sin monto o falla del servicio → ErrorExtraccion con `MensajeError` |
| RN-26 | Confianza < 0,8 → badge `Revisar` (`badge text-bg-warning`, `title="Confianza NN%"`) |
| RN-27 | Function idempotente: si no está en Procesando, no hace nada |

### 7.4 Endpoints de API (todas requieren sesión; consumidos por `fetch` desde las vistas)

| Método | Ruta | Rol | Respuesta |
|---|---|---|---|
| GET | `/api/comprobantes/{id}/estado` | Dueño | Estado + comercio, fecha, descripción, monto, moneda, confianzas, mensaje de error. Aplica el timeout de 5 min |
| GET | `/api/comprobantes/{id}/archivo?descarga={bool}` | Dueño o Administrador | URL SAS de lectura (5 min, un blob) + vencimiento; con `descarga=true` fija `Content-Disposition: attachment; filename=<original>` |
| DELETE | `/api/comprobantes/{id}` | Dueño | 204; borra blob y registro |
| POST | `/api/admin/comprobantes/{id}/tomar` | Administrador | 200 con el nuevo estado |
| POST | `/api/admin/comprobantes/{id}/aprobar` | Administrador | 200 con el nuevo estado |
| POST | `/api/admin/comprobantes/{id}/rechazar` | Administrador | Body `{ "motivo": "…" }` → 200 |
| POST | `/api/admin/comprobantes/{id}/pagar` | Administrador | Body `{ "fechaPago": "YYYY-MM-DD" }` → 200 |

Listados, detalle y formularios **no** son API: son acciones MVC que devuelven vistas.

### 7.5 Pantallas

| # | Pantalla | Ruta | Rol |
|---|---|---|---|
| P1 | Bienvenida / Login | `/` (sin sesión) | Anónimo |
| P2 | Dashboard | `/Home/Index` | Empleado, Administrador |
| P3 | Mis comprobantes | `/Comprobantes` | Empleado, Administrador |
| P4 | Cargar comprobante | `/Comprobantes/Cargar` | Empleado, Administrador |
| P5 | Detalle | `/Comprobantes/Detalle/{id}` | Dueño |
| P6 | Todos los comprobantes | `/Admin/Comprobantes` | Administrador |
| P7 | Detalle administración | `/Admin/Comprobantes/Detalle/{id}` | Administrador |

P1 y P2 son la misma acción (`HomeController.Index`, que también responde en `/`): sin sesión devuelve la vista
`Bienvenida`; con sesión, el Dashboard. Así no hay dos acciones compitiendo por `/` con la ruta convencional.

- **P1**: card centrada sin layout autenticado: "UNLaM · Programación Web 3", título "Reembolsos", texto
  breve, `btn btn-primary btn-lg w-100` **Iniciar sesión con Microsoft** → `Cuenta/IniciarSesion`;
  "Usá una cuenta del tenant de Entra ID del equipo." Con sesión, `/` redirige al Dashboard.
- **Acceso denegado**: card `border-danger-subtle`, badge "403", "No tenés acceso a esta pantalla",
  botones Ir al dashboard / Cerrar sesión. Para usuarios sin rol y Empleados en `/Admin/...`.
- **P2**: "Hola, <nombre>"; botones **Cargar comprobante** y **Ver mis comprobantes**; card "Documentos
  abiertos" (`display-3`, "Aún no aprobados ni rechazados"); card "Por estado" (`list-group` con los 4 estados
  abiertos y su cantidad); si hay ErrorExtraccion, `alert alert-warning` "Tenés N comprobante(s) con error de
  extracción. Completá monto y descripción para enviarlo a revisión." + botón **Completar ahora**. Solo propios.
- **P3**: columnas Nombre (archivo original), Fecha de carga, Monto (con moneda, "—" si vacío), Estado (badge;
  Procesando con `spinner-border-sm`), Acción (**Ver detalle**; **Eliminar** con modal si PendienteRevision o
  ErrorExtraccion). Filtros: Estado (select "Todos" + 7) y Orden ("Más recientes primero" por defecto / "Más
  antiguos primero"); paginación 10 con "Mostrando 1–N de M". Vacío: "No hay comprobantes cargados. Cargar
  comprobante". Filas Procesando se actualizan por polling sin recargar.
  Modal eliminar: "Se va a eliminar **<archivo>** y su archivo. Esta acción no se puede deshacer."
- **P4 etapa 1** (breadcrumb Mis comprobantes › Cargar): card "1. Subí el archivo": input file
  (`accept=".pdf,.jpg,.jpeg,.png"`, ayuda "PDF, JPG o PNG. Máximo 4 MB. De los PDF se analizan las 2 primeras
  páginas."), al elegir muestra nombre, tamaño, miniatura (o ícono PDF) y **Quitar**; selects Tipo de documento y
  Categoría ("Elegí una opción"); botones Cancelar / **Subir y analizar**. Columna lateral "Qué pasa después" (4 pasos).
- **P4 etapa 2**: card "2. Resultado de la extracción". Procesando: spinner + "Analizando comprobante…".
  Éxito: `alert-success` + formulario con comercio, fecha, descripción, monto, moneda, categoría (badge **Revisar**
  en baja confianza). Error: `alert-danger` "No pudimos leer el comprobante. Completá descripción y monto para
  enviarlo a revisión." + mismo formulario. > 2 min: `alert-info` "El análisis está tardando más de lo habitual.
  Podés seguir el estado desde Mis comprobantes." Botones: **Eliminar**, **Ir al detalle**, **Guardar cambios**
  (o "Guardar y enviar a revisión" si está en Error).
- **P5**: breadcrumb, título = archivo + badge. Columna izquierda: card "Vista previa" (`<img>` o `<iframe>` para PDF
  con URL SAS; alto fijo con CSS propio justificado) + botón **Descargar**. Derecha: card "Datos del comprobante"
  (`dl.row`: archivo, tipo, categoría, comercio, fecha, descripción, monto, fecha de carga, fecha de pago si hay,
  badge "Editados manualmente" si corresponde) con alertas según estado (rechazo con motivo, error, procesando);
  footer con **Editar datos** (collapse; abierto por defecto en Error) y **Eliminar** solo en PendienteRevision/Error.
  Card "Historial" (`list-group`, más reciente primero): badge anterior → badge nuevo, fecha-hora, usuario
  ("Procesamiento automático" si es la Function) · observación.
- **P6**: igual a P3 + columna Empleado (badge "Propio" en los del admin) y filtros Estado, Empleado, Orden.
  Acciones por fila: **Tomar en revisión** (PendienteRevision), **Aprobar** / **Rechazar** (EnRevision),
  **Marcar pagado** (Aprobado), siempre **Ver detalle**; sin acciones de estado en los propios.
  Modal rechazar: textarea con contador "0/500", "El empleado va a ver este motivo.", feedback "Escribí un motivo
  de entre 10 y 500 caracteres.". Modal pagar: `input type=date` por defecto hoy, `max` = hoy, `min` = fecha de carga.
  Toasts: "Comprobante tomado en revisión." / "aprobado." / "rechazado." / "marcado como pagado.".
- **P7**: como P5 + nombre y email del empleado; sin Editar ni Eliminar; botones de estado con los modales de P6;
  en propios `alert-info` "Este comprobante es tuyo. Otro administrador debe revisarlo." Tras cada acción, toast
  de éxito/error y la vista se actualiza (409 → mensaje y recarga).

### 7.6 Seguridad, monitoreo e interfaz
- Contenedor `comprobantes` privado; SAS de solo lectura, sobre un único blob, 5 minutos.
- Excepciones no controladas → vista de error genérica (`UseExceptionHandler`), sin detalles técnicos.
- Logs mínimos: carga, inicio y fin de extracción con duración, error de extracción, cada cambio de estado.
- Badges (idénticos en todas las pantallas):

| Estado | Texto | Clases |
|---|---|---|
| Procesando | Procesando | `badge text-bg-secondary` |
| ErrorExtraccion | Error de extracción | `badge bg-danger-subtle text-danger-emphasis border border-danger-subtle` |
| PendienteRevision | Pendiente de Revisión | `badge text-bg-warning` |
| EnRevision | En Revisión | `badge text-bg-info` |
| Aprobado | Aprobado | `badge text-bg-primary` |
| Rechazado | Rechazado | `badge text-bg-danger` |
| Pagado | Pagado | `badge text-bg-success` |

### 7.7 Entregables y criterios de aceptación
Entregables: repo con web + Function + librerías; migraciones; README (diagrama, recursos de Azure,
App Registration + App Roles, variables sin valores, correr local y desplegar); URL pública en App Service
funcionando durante la corrección; 3 usuarios de prueba (2 Empleado, 1 Administrador) entregados por canal
privado; captura de App Insights con trazas de una extracción completa; opcional CI/CD y Managed Identity.

Checklist de aceptación:
- [ ] Usuario sin rol no accede a ninguna pantalla autenticada.
- [ ] Empleado no accede a `/Admin/...` ni a `/api/admin/...` (403).
- [ ] Empleado no ve ni descarga comprobantes ajenos cambiando el id (404).
- [ ] Archivo > 4 MB o con formato no permitido se rechaza con mensaje claro.
- [ ] Recibo legible pasa a Pendiente de Revisión con monto y descripción en < 1 minuto.
- [ ] Archivo ilegible termina en Error de extracción y puede completarse manualmente.
- [ ] Dashboard muestra la cantidad correcta de documentos abiertos.
- [ ] Listados filtran, ordenan y paginan correctamente.
- [ ] Preview y descarga funcionan para imágenes y PDF; la SAS deja de funcionar a los 5 minutos.
- [ ] Admin recorre tomar → aprobar → pagar, y tomar → rechazar con motivo.
- [ ] Transición inválida (p. ej. pagar un Pendiente) devuelve 409.
- [ ] Admin no puede cambiar el estado de su propio comprobante.
- [ ] Cada cambio de estado aparece en el historial con usuario y fecha.
- [ ] Eliminar un comprobante borra también su blob.
- [ ] No hay secretos en el repositorio.
- [ ] Bootstrap 5.3 con CSS propio mínimo y justificado.

## 8. Recetas

### 8.1 Agregar una funcionalidad de punta a punta
1. **DTOs** (`GestorGastos.DTOs`): DTO de entrada con Data Annotations (mensajes de 7.3) y DTO de salida.
2. **Repositorio** (si hace falta un query): método en `Interfaces/I<X>Repositorio.cs` + implementación.
   Filtrar por `UsuarioId` cuando sea una consulta de empleado.
3. **Servicio** (`GestorGastos.Servicios`): método en `Interfaces/I<X>Servicio.cs` + implementación,
   devolviendo `Resultado<T>`; registrar `AddScoped` en `Web/Program.cs` **y** en `Functions/Program.cs` si la Function lo usa.
4. **Controller**: MVC (vista) o API (`Controllers/Api`, `[ApiController]`, `[Route("api/...")]`,
   `[ValidateAntiForgeryToken]` en POST/DELETE, `[Authorize(Roles = "Administrador")]` si es admin).
   Usuario: `User.GetObjectId()`; nunca del body/query.
5. **Vista + JS** (+ ViewModel en `Web/Models/` si combina datos), siguiendo el prototipo y la PNG de la pantalla.
6. **Logging** si es un evento de 7.6.
7. Actualizar las secciones 5 y 6 de este archivo.

### 8.2 Cambiar el estado de un comprobante
Siempre por `ComprobanteServicio` (un único punto, p. ej. `CambiarEstadoAsync(id, nuevoEstado, usuarioId?, observacion?)`):
1. `ObtenerPorIdAsync` con tracking → 404 si no existe (o si es ajeno y quien pide no es admin).
2. Admin sobre propio → 403 (RN-15). Transición fuera de la tabla 7.2 → 409 (RN-13).
3. Validar datos de la transición (motivo RN-16, fecha de pago RN-17, monto/descripción RN-08/09).
4. Aplicar cambios + agregar `HistorialEstado` (RN-18) + `GuardarCambiosAsync()`.
5. Log del cambio. La Function usa este mismo método con `usuarioId = null`.

### 8.3 Cambiar el modelo
Editar entidad / `GestorGastosDbContext.OnModelCreating` → `dotnet ef migrations add <NombreEnPascal>` (comando de la
sección 1) → revisar la migración → actualizar la sección 4. No editar migraciones ya mergeadas en `main`.

### 8.4 Eliminar una funcionalidad
Cadena inversa: vista/JS/CSS → controller MVC/API → servicio + registros DI (web y Function) → métodos de
repositorio sin otros usos → DTOs → entidad + migración. Buscar referencias con
`grep -rn "<Nombre>" --include=*.cs --include=*.cshtml --include=*.js` (incluidos `asp-action`/`asp-controller`/`asp-area`)
y actualizar las secciones 5 y 6.

### 8.5 Datos de ejemplo del prototipo (para pruebas manuales y capturas)
Los usuarios reales vienen de Entra ID; estos sirven para crear las cuentas de prueba del tenant y archivos de ejemplo.
- Usuarios: **Lucía Benítez** y **Martín Ferreyra** (Empleado), **Carolina Ruiz** (Administrador),
  emails `<nombre>.<apellido>@<tenant>.onmicrosoft.com`.
- Comprobantes del prototipo: `ticket-taxi-ezeiza.jpg` (Lucía, Recibo, Traslados, Taxi Ezeiza SRL, $18.450, PendienteRevision),
  `factura-hotel-cordoba.pdf` (Lucía, Factura, Hospedaje, $142.300, EnRevision), `almuerzo-cliente.png` (Lucía, Viáticos, $36.900, Aprobado),
  `pasaje-aep-cor.pdf` (Lucía, Pasajes, $215.780,50, Pagado), `estacionamiento.jpg` (Lucía, $4.500, Rechazado: "El gasto corresponde a un
  día sin viaje registrado."), `recibo-borroso.jpg` (Lucía, ErrorExtraccion: "No se detectó el monto total del comprobante."),
  `peaje-autopista.jpg` (Lucía, Procesando), `combustible-ruta-2.jpg` (Martín, $52.000, PendienteRevision),
  `cena-equipo.jpg` (Martín, $88.400, EnRevision, confianza descripción 0,71), `hotel-rosario.pdf` (Martín, $98.500, Aprobado),
  `taxi-reunion-cliente.jpg` (Carolina, $12.300, PendienteRevision — caso "propio" del admin), `remis-aeroparque.jpg` (Martín, $15.600, Pagado).

## 9. Gotchas

- **No usar Razor Pages** (ni `AddRazorPages`): login y logout los resuelve `CuentaController` (ver 0), por eso no se
  usa `Microsoft.Identity.Web.UI`.
- `AlmacenamientoArchivos` firma la SAS con la clave de la cuenta (`CanGenerateSasUri`): con una connection string sin
  `AccountKey` (o con Managed Identity) `GenerarUrlLecturaAsync` lanza `InvalidOperationException`. Sin
  `Storage:ConnectionString` o `Storage:Contenedor` el constructor también falla, pero recién al resolver el servicio
  (cualquier request que use `ComprobanteServicio`), no al arrancar.
- El Blob Trigger clásico puede demorar minutos: en Azure usar origen **Event Grid** (criterio "< 1 minuto").
  En local, Azurite no emite eventos de Event Grid: usar el trigger clásico solo en local.
- Document Intelligence F0: máximo 4 MB y solo las 2 primeras páginas de un PDF.
- `IFormFile.ContentType` lo manda el navegador: validar por magic bytes (RN-03) y guardar el `ContentType` derivado.
- El nombre original nunca va al blob (`{GUID}.{ext}`); se usa solo para mostrar y en `Content-Disposition` de la descarga.
- Orden de la carga: primero el registro (Procesando), después el blob; si el blob falla, borrar el registro.
  Si fuera al revés, la Function podría dispararse sin registro que encontrar.
- Eliminar: primero blob (tolerar `404`), después registro; el historial cae por cascade.
- La Function puede ejecutarse más de una vez para el mismo blob: chequear estado Procesando antes de procesar.
- El timeout de 5 min lo aplica la **web** al consultar el estado, no un job: comparar `FechaCarga` (UTC) con `DateTime.UtcNow`.
- 404 vs 403: empleado sobre ajeno → 404; Empleado en `/api/admin` → 403; admin sobre su propio comprobante → 403.
  Usuario autenticado sin rol → redirigir a Acceso denegado (`AccessDeniedPath` de la cookie), no una 403 en blanco.
- Una acción `[AllowAnonymous]` saltea la fallback policy también para el rol: si muestra algo a usuarios logueados
  (como `HomeController.Index`), chequear `User.TieneRol()` y devolver `Forbid()`.
- Los roles llegan en el claim `roles`; con `Microsoft.Identity.Web` funcionan con `[Authorize(Roles = ...)]` y `User.IsInRole`.
- Fechas: guardar UTC y convertir solo al mostrar; el `input type=date` del pago/fecha del comprobante es fecha local de Argentina.
  "No futura" se compara contra la fecha de hoy en Argentina, no contra UTC (entre 21:00 y 24:00 difieren).
- Sin control de concurrencia entre administradores (ver 0): no agregar `RowVersion`, `[Timestamp]` ni
  `IsConcurrencyToken`, ni manejar `DbUpdateConcurrencyException`.
- `HasConversion<string>()` en **todos** los enums, incluido `HistorialEstado.EstadoAnterior` (nullable).
- `local.settings.json`, `.env`, `appsettings.*.local.json` y User Secrets nunca en el repo.
- Las migraciones se aplican solas al arrancar la web (ver 0): una migración nueva corre en producción en el próximo
  despliegue. Evitar cambios destructivos (renombrar/borrar columnas con datos) sin migrar los datos en la misma migración.
- Con `EnableRetryOnFailure`, una transacción explícita (`BeginTransactionAsync`) tiene que ir dentro de
  `db.Database.CreateExecutionStrategy().ExecuteAsync(...)`; si no, EF lanza `InvalidOperationException`.
