# Enunciado TP – Gestión de Reembolsos de Gastos con Azure

Nov 1, 2026

## 1. Contexto y objetivo

El equipo debe desarrollar y desplegar en Azure una aplicación web de reembolso de gastos. Los empleados cargan recibos y facturas, la plataforma extrae sus datos con Azure AI Document Intelligence y un administrador los revisa, aprueba o rechaza y marca como pagados.

El trabajo implementa la propuesta "Azure Cloud Computing" presentada en Programación Web 3 (UNLaM). La solución se despliega en una suscripción de Azure con **tenant propio de Microsoft Entra ID**, independiente del directorio de la universidad.

**Dentro del alcance**

- Inicio de sesión con Microsoft Entra ID y roles Empleado / Administrador.
- Carga de comprobantes, almacenamiento en Blob Storage y extracción automática mediante una Azure Function.
- Listados, detalle con previsualización y descarga, edición de datos extraídos y eliminación.
- Flujo de aprobación con historial de cambios de estado.
- Dashboard simple con la cantidad de documentos abiertos del usuario.
- Monitoreo con Application Insights.

**Fuera del alcance**

- Dashboard analítico (gastos por categoría, período o empleado).
- Integración con sistemas de pago, notificaciones por email y tests unitarios.

## 2. Stack tecnológico obligatorio

Todo el stack es obligatorio. Cualquier reemplazo debe acordarse con la cátedra antes de implementarlo.

| Capa | Tecnología | Condiciones |
| --- | --- | --- |
| Runtime | .NET 10 (LTS) | Misma versión en la web y en la Function. |
| Web | ASP.NET Core MVC con vistas Razor | Controllers + Views. **No se aceptan Razor Pages.** |
| API | Controllers de Web API en el mismo proyecto | Rutas bajo `/api`, respuestas JSON (ver sección 8). |
| Acceso a datos | Entity Framework Core, Code First | Migraciones versionadas en el repositorio. |
| Base de datos | Azure SQL Database | Oferta gratuita de Azure SQL. |
| Autenticación | Microsoft Entra ID | `Microsoft.Identity.Web`, App Registration single-tenant, App Roles. |
| Archivos | Azure Blob Storage | Contenedor privado `comprobantes`. |
| Procesamiento | Azure Functions, modelo isolated worker | Disparada por Blob Trigger. |
| IA documental | Azure AI Document Intelligence, tier F0 | SDK `Azure.AI.DocumentIntelligence`, modelos `prebuilt-receipt` y `prebuilt-invoice`. |
| Monitoreo | Application Insights | Conectado a la web y a la Function. |
| Hosting | Azure App Service | Plan F1 o B1. |
| UI | Bootstrap 5.3 | CSS propio solo si Bootstrap no lo resuelve (ver sección 10). |

Ningún secreto (connection strings, claves, client secrets) puede estar en el repositorio. Se usan User Secrets en local y App Settings o Key Vault en Azure.

## 3. Arquitectura y flujo de procesamiento

La web nunca espera a Document Intelligence: guarda el archivo y responde, y una Azure Function hace la extracción en segundo plano.

&#91;embedded content: arquitectura · 8 componentes, extracción en 4 pasos\]

1. El empleado envía el archivo desde P4. La web valida, crea el comprobante en estado **Procesando** y recién después sube el blob como `{GUID}.{ext}`. Si la subida falla, borra el registro.
2. El nuevo blob dispara la Azure Function. Se recomienda el Blob Trigger con origen Event Grid, porque el trigger clásico puede demorar varios minutos.
3. La Function busca el comprobante por `BlobNombre` y llama a Document Intelligence con el modelo que corresponde al tipo de documento.
4. La Function guarda los datos extraídos y deja el comprobante en **Pendiente de Revisión**, o en **Error de extracción** si falla. Registra el cambio en el historial.
5. Mientras tanto, la vista de carga consulta el estado por polling hasta mostrar el resultado.

La web y la Function comparten las entidades y el `DbContext` en una librería de clases común.

## 4. Autenticación y roles

Toda la aplicación requiere sesión iniciada con Microsoft Entra ID, salvo la pantalla de bienvenida. Los permisos se resuelven con App Roles del token, nunca con datos enviados por el cliente.

1. Cada equipo crea un tenant propio de Entra ID y un App Registration **single-tenant** para la web.
2. En el App Registration se definen los App Roles `Empleado` y `Administrador`.
3. Los roles se asignan a usuarios desde la Enterprise Application. Un usuario sin rol asignado ve la pantalla de acceso denegado.
4. En el primer inicio de sesión, la aplicación registra al usuario en la tabla `Usuario` (oid, nombre, email). En los siguientes, actualiza nombre, email y último acceso.

| Acción | Empleado | Administrador |
| --- | --- | --- |
| Ver dashboard propio | Sí | Sí |
| Cargar, editar y eliminar comprobantes propios | Sí | Sí |
| Ver comprobantes propios | Sí | Sí |
| Ver comprobantes de todos los usuarios | No | Sí |
| Cambiar el estado de un comprobante | No | Sí, excepto los propios |

Un administrador puede cargar sus propios comprobantes, pero **no puede revisar, aprobar, rechazar ni pagar los suyos**. Esa regla se valida en el backend, no solo ocultando botones.

## 5. Modelo de datos

El modelo mínimo tiene tres entidades: `Usuario`, `Comprobante` e `HistorialEstado`. Los equipos pueden agregar campos, pero no quitar los listados. Todas las fechas se guardan en UTC y se muestran en hora de Argentina (UTC−3).

**Usuario**

| Campo | Tipo | Reglas |
| --- | --- | --- |
| Id | string (PK) | oid del usuario en Entra ID. |
| Nombre | nvarchar(200) | Requerido. |
| Email | nvarchar(256) | Requerido. |
| UltimoAcceso | datetime2 | Requerido. |

**Comprobante**

| Campo | Tipo | Reglas |
| --- | --- | --- |
| Id | int (PK) | Identity. |
| UsuarioId | string (FK) | Dueño del comprobante. |
| NombreArchivoOriginal | nvarchar(255) | Nombre tal como lo subió el usuario. |
| BlobNombre | nvarchar(100) | `{GUID}.{ext}`. Nunca se usa el nombre original en el blob. |
| ContentType | nvarchar(50) | `application/pdf`, `image/jpeg` o `image/png`. |
| TamanioBytes | bigint | Máximo 4 MB. |
| TipoDocumento | enum | `Recibo` o `Factura`. |
| Categoria | enum | `Viaticos`, `Pasajes`, `Hospedaje`, `Traslados`, `Otros`. |
| Comercio | nvarchar(200), nulo | Extraído. |
| FechaComprobante | date, nulo | Extraída. |
| Descripcion | nvarchar(500), nulo | Extraída: ítems concatenados con "; ". |
| Monto | decimal(18,2), nulo | Extraído. Total del comprobante. |
| Moneda | char(3), nulo | Código ISO 4217. Si no se detecta, `ARS`. |
| ConfianzaDescripcion | float, nulo | Valor 0 a 1 devuelto por Document Intelligence. |
| ConfianzaMonto | float, nulo | Valor 0 a 1 devuelto por Document Intelligence. |
| EditadoManualmente | bit | `true` si el empleado modificó algún dato extraído. |
| Estado | enum | Ver sección 6. |
| MensajeError | nvarchar(500), nulo | Motivo técnico si falló la extracción. |
| MotivoRechazo | nvarchar(500), nulo | Obligatorio si el estado es Rechazado. |
| FechaCarga | datetime2 | Asignada por el servidor. |
| FechaPago | datetime2, nulo | Obligatoria si el estado es Pagado. |
| RowVersion | rowversion | Control de concurrencia optimista. |

**HistorialEstado**

| Campo | Tipo | Reglas |
| --- | --- | --- |
| Id | int (PK) | Identity. |
| ComprobanteId | int (FK) | Borrado en cascada con el comprobante. |
| EstadoAnterior | enum, nulo | Nulo en el primer registro. |
| EstadoNuevo | enum | Requerido. |
| UsuarioId | string, nulo | Nulo cuando el cambio lo hace la Function. |
| Fecha | datetime2 | Asignada por el servidor. |
| Observacion | nvarchar(500), nulo | Motivo de rechazo u otra nota. |

Los enums se guardan como string (`HasConversion<string>()`) para que la base sea legible. Se crea un índice sobre `(UsuarioId, Estado)` y otro sobre `FechaCarga`.

## 6. Estados y transiciones

Un comprobante tiene siete estados posibles. Solo las transiciones del diagrama son válidas, y el backend rechaza cualquier otra con 409.

&#91;embedded content: flujo de estados · 7 estados, 7 transiciones\]

| Transición | Quién | Condición |
| --- | --- | --- |
| Procesando → Pendiente de Revisión | Azure Function | Se extrajeron monto y descripción. |
| Procesando → Error de extracción | Azure Function, o la web tras 5 minutos sin respuesta | Falla del servicio o sin monto detectado. |
| Error de extracción → Pendiente de Revisión | Empleado dueño | Completa monto y descripción válidos. |
| Pendiente de Revisión → En Revisión | Administrador | No es dueño del comprobante. |
| En Revisión → Aprobado | Administrador | No es dueño del comprobante. |
| En Revisión → Rechazado | Administrador | Motivo obligatorio. Estado final. |
| Aprobado → Pagado | Administrador | Fecha de pago obligatoria. Estado final. |

En Pendiente de Revisión el empleado todavía puede editar los datos o eliminar el comprobante. Desde que el administrador lo toma en revisión, el comprobante queda bloqueado para el empleado. Un comprobante rechazado no se reabre: el empleado debe cargar uno nuevo.

## 7. Pantallas y requerimientos funcionales

La aplicación tiene siete pantallas. Los prototipos adjuntos son la referencia visual. Las rutas son orientativas, pero deben respetar el esquema Controller/Action de MVC.

| # | Pantalla | Ruta sugerida | Rol |
| --- | --- | --- | --- |
| P1 | Bienvenida / Login | `/` (sin sesión) | Anónimo |
| P2 | Dashboard | `/Home/Index` | Empleado, Administrador |
| P3 | Mis comprobantes | `/Comprobantes` | Empleado, Administrador |
| P4 | Cargar comprobante | `/Comprobantes/Cargar` | Empleado, Administrador |
| P5 | Detalle de comprobante | `/Comprobantes/Detalle/{id}` | Dueño |
| P6 | Todos los comprobantes | `/Admin/Comprobantes` | Administrador |
| P7 | Detalle (administración) | `/Admin/Comprobantes/Detalle/{id}` | Administrador |

Todas las pantallas autenticadas comparten un layout con navbar: nombre de la app, links a Dashboard, Mis comprobantes y Cargar; el link Administración solo para ese rol; y a la derecha el nombre del usuario con la opción Cerrar sesión.

### P1. Bienvenida / Login

- Muestra el nombre de la aplicación, una descripción breve y el botón **Iniciar sesión con Microsoft**.
- El botón redirige al flujo OpenID Connect de Entra ID. Al volver, el usuario llega al Dashboard.
- Si el usuario ya tiene sesión, `/` redirige directamente al Dashboard.
- Se agrega una vista de **Acceso denegado** para usuarios autenticados sin rol asignado o que intentan entrar a rutas de administración.

### P2. Dashboard

- Muestra la cantidad de **documentos abiertos** del usuario logueado. Un documento está abierto si su estado es Procesando, Error de extracción, Pendiente de Revisión o En Revisión.
- Debajo del total, el desglose por cada uno de esos cuatro estados.
- Botones de acceso rápido: **Cargar comprobante** y **Ver mis comprobantes**.
- Si el usuario tiene comprobantes en Error de extracción, se muestra un alerta invitando a completarlos.

### P3. Mis comprobantes

- Tabla con una fila por comprobante propio. Columnas: **Nombre** (nombre original del archivo), **Fecha de carga**, **Monto** (con moneda), **Estado** (badge) y **Acción**.
- Acción: botón **Ver detalle**. Si el estado es Pendiente de Revisión o Error de extracción, se agrega **Eliminar**, con confirmación en un modal.
- Monto vacío se muestra como "—". Monto con formato `$ 12.345,67` (cultura es-AR).
- Filtro por estado (select), orden por fecha de carga (descendente por defecto) y paginación de 10 filas.
- Sin resultados: mensaje vacío con link a Cargar comprobante.
- Las filas en estado Procesando actualizan su estado sin recargar la página (polling, ver sección 8).

### P4. Cargar comprobante

La pantalla tiene dos etapas en la misma vista.

**Etapa 1: subida**

- Campos: **Archivo** (requerido), **Tipo de documento** (Recibo / Factura, requerido) y **Categoría** (requerida).
- Al elegir un archivo se muestra su nombre, tamaño y una miniatura si es imagen, junto a un botón **Quitar** para descartarlo antes de enviar.
- Al enviar: validación en cliente y servidor, subida a Blob Storage, alta del comprobante en estado Procesando y redirección a la etapa 2.

**Etapa 2: resultado de la extracción**

- Mientras el estado es Procesando: spinner y texto "Analizando comprobante…". La vista consulta el estado cada 3 segundos, hasta un máximo de 2 minutos.
- Al terminar con éxito: muestra comercio, fecha, descripción y monto extraídos. Si la confianza de un campo es menor a 0,8, se marca con un aviso para revisarlo.
- Si falla: alerta de error y formulario para completar descripción y monto manualmente. Si pasan 2 minutos sin resultado: aviso de que puede seguir el estado desde Mis comprobantes. Un comprobante que lleva más de 5 minutos en Procesando pasa a Error de extracción al consultarse su estado.
- Botones: **Guardar cambios** (si editó algo), **Ir al detalle** y **Eliminar**.

### P5. Detalle de comprobante

- Muestra: nombre del archivo, tipo, categoría, comercio, fecha del comprobante, descripción, monto con moneda, estado, fecha de carga y, si corresponde, motivo de rechazo y fecha de pago.
- **Previsualización**: imagen con `<img>` o PDF con `<iframe>`, ambos con URL SAS de corta duración.
- **Descargar**: descarga el archivo con su nombre original.
- **Editar**: solo en Pendiente de Revisión o Error de extracción. Permite modificar descripción, monto, moneda, comercio, fecha y categoría.
- **Eliminar**: mismas condiciones que en P3.
- **Historial**: lista de cambios de estado con fecha, usuario y observación.

### P6. Todos los comprobantes (administración)

- Misma tabla que P3 con una columna adicional **Empleado** (nombre del dueño) y todos los comprobantes de la plataforma.
- Filtros: estado y empleado. Orden por fecha de carga y paginación de 10.
- Acciones por fila según el estado: **Tomar en revisión** (Pendiente de Revisión), **Aprobar** o **Rechazar** (En Revisión), **Marcar pagado** (Aprobado). Siempre **Ver detalle**.
- En comprobantes propios del administrador no se muestran acciones de estado.
- Rechazar abre un modal con el motivo obligatorio. Marcar pagado abre un modal de confirmación con la fecha de pago (por defecto, hoy).

### P7. Detalle (administración)

- Igual que P5, con el nombre y email del empleado.
- Sin Editar ni Eliminar: el administrador no modifica datos de otros usuarios.
- Botones de cambio de estado según la sección 6, con los mismos modales que P6.
- Tras cada acción se muestra un mensaje de éxito o error y la vista se actualiza.

## 8. Endpoints de API

Las vistas Razor consumen estos endpoints con `fetch` para el polling, la previsualización y las acciones de estado. Todos requieren sesión. Las páginas completas (listados, detalle, formularios) siguen siendo acciones MVC que devuelven vistas.

| Método | Ruta | Rol | Respuesta |
| --- | --- | --- | --- |
| GET | `/api/comprobantes/{id}/estado` | Dueño | Estado y datos extraídos (comercio, fecha, descripción, monto, moneda, confianzas, mensaje de error). |
| GET | `/api/comprobantes/{id}/archivo?descarga={bool}` | Dueño o Administrador | URL SAS de lectura válida 5 minutos y su vencimiento. |
| DELETE | `/api/comprobantes/{id}` | Dueño | 204. Borra el blob y el registro. |
| POST | `/api/admin/comprobantes/{id}/tomar` | Administrador | 200 con el nuevo estado. |
| POST | `/api/admin/comprobantes/{id}/aprobar` | Administrador | 200 con el nuevo estado. |
| POST | `/api/admin/comprobantes/{id}/rechazar` | Administrador | Body `{ "motivo": "…" }`. 200 con el nuevo estado. |
| POST | `/api/admin/comprobantes/{id}/pagar` | Administrador | Body `{ "fechaPago": "YYYY-MM-DD" }`. 200 con el nuevo estado. |

**Condiciones comunes**

- Las respuestas de error usan `ProblemDetails` (RFC 9457) con mensaje en español.
- Códigos: 400 datos inválidos, 401 sin sesión, 403 sin permiso o administrador sobre un comprobante propio, 404 inexistente, 409 transición inválida para el estado actual o conflicto de concurrencia.
- Un empleado que pide un comprobante ajeno recibe 404, no 403, para no revelar su existencia.
- Los POST y DELETE exigen token antiforgery en el header `RequestVerificationToken`.
- Los controllers no contienen lógica de negocio: delegan en un servicio (por ejemplo, `ComprobanteService`) que valida las transiciones.

## 9. Validaciones y reglas de negocio

Cada regla se valida en el servidor. La validación en cliente es complementaria y usa los atributos de Data Annotations con jQuery Validation Unobtrusive.

**Archivo**

| Código | Regla | Mensaje sugerido |
| --- | --- | --- |
| RN-01 | El archivo es obligatorio. | "Seleccioná un archivo." |
| RN-02 | Extensión permitida: `.pdf`, `.jpg`, `.jpeg`, `.png`. | "Formato no permitido. Usá PDF, JPG o PNG." |
| RN-03 | El tipo de contenido coincide con la extensión: se verifica la firma del archivo (magic bytes), no solo el header. | "El contenido del archivo no corresponde a su formato." |
| RN-04 | Tamaño máximo 4 MB, límite del tier F0. | "El archivo supera los 4 MB." |
| RN-05 | Un único archivo por comprobante. | — |
| RN-06 | Un PDF de más de 2 páginas se acepta, pero se avisa que solo se analizan las 2 primeras (límite F0). | "Solo se analizarán las primeras 2 páginas." |

**Datos del comprobante**

| Código | Regla |
| --- | --- |
| RN-07 | Tipo de documento y categoría son obligatorios al cargar. |
| RN-08 | Monto: obligatorio para salir de Error de extracción; mayor a 0 y menor o igual a 99.999.999,99; dos decimales. |
| RN-09 | Descripción: obligatoria para salir de Error de extracción; entre 3 y 500 caracteres. |
| RN-10 | Fecha del comprobante: opcional; no puede ser futura. |
| RN-11 | Moneda: uno de `ARS`, `USD`, `EUR`, `BRL`, `CLP`, `UYU`. |
| RN-12 | Al editar un dato extraído, `EditadoManualmente` pasa a `true`. |

**Estados y permisos**

| Código | Regla |
| --- | --- |
| RN-13 | Solo se permiten las transiciones de la sección 6. Cualquier otra devuelve 409. |
| RN-14 | El empleado solo edita y elimina sus comprobantes en Pendiente de Revisión o Error de extracción. |
| RN-15 | Un administrador no cambia el estado de sus propios comprobantes. |
| RN-16 | Rechazar exige motivo de 10 a 500 caracteres. Se guarda en `MotivoRechazo` y en el historial. |
| RN-17 | Pagar exige fecha de pago no futura y no anterior a la fecha de carga. |
| RN-18 | Cada cambio de estado, incluidos los de la Function, crea un registro en `HistorialEstado`. |
| RN-19 | Si dos administradores actúan a la vez, el segundo recibe 409 por `RowVersion` y la vista se recarga. |
| RN-20 | Eliminar borra primero el blob y después el registro. Si el blob ya no existe, se borra igual el registro. |

**Extracción**

| Código | Regla |
| --- | --- |
| RN-21 | `TipoDocumento = Recibo` usa `prebuilt-receipt`; `Factura` usa `prebuilt-invoice`. |
| RN-22 | Recibo: comercio = `MerchantName`, fecha = `TransactionDate`, monto = `Total`, descripción = descripciones de `Items`. |
| RN-23 | Factura: comercio = `VendorName`, fecha = `InvoiceDate`, monto = `InvoiceTotal`, descripción = descripciones de `Items`. |
| RN-24 | Si no se detectan ítems, la descripción queda vacía y el empleado debe completarla. |
| RN-25 | Si no se detecta monto o el servicio falla, el estado pasa a Error de extracción con `MensajeError`. |
| RN-26 | Una confianza menor a 0,8 se marca en la interfaz con un aviso de revisión. |
| RN-27 | La Function es idempotente: si el comprobante ya no está en Procesando, no hace nada. |

## 10. Seguridad, monitoreo e interfaz

**Seguridad**

- El contenedor `comprobantes` es privado (sin acceso anónimo). El navegador nunca recibe la connection string.
- Las URLs de previsualización y descarga son SAS de solo lectura, sobre un único blob, con 5 minutos de vigencia. La de descarga fija `Content-Disposition: attachment` con el nombre original.
- Toda consulta de comprobantes de empleado filtra por `UsuarioId` del usuario logueado.
- Los controllers usan `[Authorize]` y `[Authorize(Roles = "Administrador")]` donde corresponda. Ocultar botones no reemplaza esa validación.
- HTTPS obligatorio y antiforgery en todos los formularios.
- Opcional (suma): Managed Identity entre App Service, Function, Storage y SQL en lugar de secretos.

**Monitoreo con Application Insights**

- La web y la Function envían telemetría al mismo recurso.
- Se registran con `ILogger` como mínimo: carga de comprobante, inicio y fin de extracción con su duración, error de extracción y cada cambio de estado.
- Las excepciones no controladas se muestran al usuario con una vista de error genérica, sin detalles técnicos.

**Interfaz con Bootstrap 5.3**

- Se usa Bootstrap 5.3 para layout, tipografía, formularios, tablas y componentes. Los íconos pueden ser de Bootstrap Icons.
- Componentes esperados: navbar, cards, `table table-hover`, badges, `form-control` / `form-select` con `is-invalid` e `invalid-feedback`, alerts, spinners, modals, pagination, breadcrumb y toasts.
- CSS propio solo para lo que Bootstrap no resuelve (por ejemplo, el alto fijo de la previsualización). Va en `site.css` y cada regla lleva un comentario que la justifica.
- La interfaz es responsive: las tablas usan `table-responsive` y se prueban en un ancho de 375 px.
- Badges de estado, iguales en todas las pantallas:

| Estado | Clases Bootstrap |
| --- | --- |
| Procesando | `badge text-bg-secondary` |
| Error de extracción | `badge bg-danger-subtle text-danger-emphasis border border-danger-subtle` |
| Pendiente de Revisión | `badge text-bg-warning` |
| En Revisión | `badge text-bg-info` |
| Aprobado | `badge text-bg-primary` |
| Rechazado | `badge text-bg-danger` |
| Pagado | `badge text-bg-success` |

## 11. Entregables y criterios de aceptación

**Entregables**

1. Repositorio Git con la solución: proyecto web, proyecto de Azure Functions y, si existe, una librería compartida con entidades y `DbContext`.
2. Migraciones de EF Core versionadas.
3. README con: diagrama de arquitectura, recursos de Azure creados, configuración del App Registration y los App Roles, variables de configuración (sin valores secretos) y pasos para correr en local y desplegar.
4. URL pública de la aplicación en App Service, funcionando durante la corrección.
5. Tres usuarios de prueba en el tenant del equipo, con credenciales entregadas por un canal privado a la cátedra: dos con rol Empleado y uno con rol Administrador.
6. Captura de Application Insights con trazas de una extracción completa.
7. Opcional (suma): pipeline de CI/CD en GitHub Actions o Azure DevOps que compile y despliegue la web y la Function.

No se piden tests unitarios.

**Criterios de aceptación**

- [ ] Un usuario sin rol no accede a ninguna pantalla autenticada.
- [ ] Un Empleado no accede a `/Admin/...` ni a `/api/admin/...` (403).
- [ ] Un Empleado no ve ni descarga comprobantes ajenos, aun cambiando el id en la URL (404).
- [ ] Un archivo mayor a 4 MB o con formato no permitido se rechaza con mensaje claro.
- [ ] Al cargar un recibo legible, en menos de 1 minuto pasa a Pendiente de Revisión con monto y descripción.
- [ ] Un archivo ilegible termina en Error de extracción y puede completarse manualmente.
- [ ] El dashboard muestra la cantidad correcta de documentos abiertos.
- [ ] Los listados filtran, ordenan y paginan correctamente.
- [ ] La previsualización y la descarga funcionan para imágenes y PDF; la URL SAS deja de funcionar a los 5 minutos.
- [ ] Un administrador recorre el flujo completo: tomar, aprobar y pagar; y tomar y rechazar con motivo.
- [ ] Una transición inválida (por ejemplo, pagar un Pendiente) devuelve 409.
- [ ] Un administrador no puede cambiar el estado de su propio comprobante.
- [ ] Cada cambio de estado aparece en el historial con usuario y fecha.
- [ ] Eliminar un comprobante borra también su blob.
- [ ] No hay secretos en el repositorio.
- [ ] La interfaz usa Bootstrap 5.3 y el CSS propio es mínimo y justificado.
