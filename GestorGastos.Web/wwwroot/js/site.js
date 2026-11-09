(function () {
    "use strict";

    const MENSAJE_SIN_CONEXION = "No se pudo conectar con el servidor. Revisá tu conexión e intentá de nuevo.";
    const MENSAJE_ERROR_GENERICO = "Ocurrió un error. Intentá de nuevo.";

    function tokenAntiforgery() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? "";
    }

    function mensajeDeProblema(problema) {
        if (problema?.detail) {
            return problema.detail;
        }
        const primerError = problema?.errors && Object.values(problema.errors).flat()[0];
        return primerError || MENSAJE_ERROR_GENERICO;
    }

    async function enviar(url, opciones = {}) {
        const encabezados = { Accept: "application/json", RequestVerificationToken: tokenAntiforgery() };
        let cuerpo;
        if (opciones.cuerpo !== undefined) {
            encabezados["Content-Type"] = "application/json";
            cuerpo = JSON.stringify(opciones.cuerpo);
        }

        try {
            const respuesta = await fetch(url, { method: opciones.metodo ?? "GET", headers: encabezados, body: cuerpo });
            const datos = await respuesta.json().catch(() => null);
            return {
                ok: respuesta.ok,
                status: respuesta.status,
                datos,
                mensaje: respuesta.ok ? null : mensajeDeProblema(datos)
            };
        } catch {
            return { ok: false, status: 0, datos: null, mensaje: MENSAJE_SIN_CONEXION };
        }
    }

    function mostrarToast(mensaje, esError) {
        const toast = document.getElementById("toast");
        if (!toast) {
            return;
        }
        toast.classList.toggle("text-bg-success", !esError);
        toast.classList.toggle("text-bg-danger", !!esError);
        document.getElementById("toast-mensaje").textContent = mensaje;
        bootstrap.Toast.getOrCreateInstance(toast).show();
    }

    async function pedirUrlArchivo(comprobanteId, descarga) {
        const respuesta = await enviar(`/api/comprobantes/${comprobanteId}/archivo?descarga=${descarga}`);
        if (!respuesta.ok) {
            throw new Error(respuesta.mensaje);
        }
        return respuesta.datos.url;
    }

    function prepararVistaPrevia() {
        const vistaPrevia = document.getElementById("vista-previa");
        if (!vistaPrevia) {
            return;
        }

        const botonDescargar = document.getElementById("boton-descargar");
        const comprobanteId = vistaPrevia.dataset.comprobanteId;
        const esPdf = vistaPrevia.dataset.contentType === "application/pdf";

        function mostrarMensaje(texto) {
            vistaPrevia.replaceChildren(document.createTextNode(texto));
        }

        botonDescargar.addEventListener("click", async function () {
            try {
                window.location.href = await pedirUrlArchivo(comprobanteId, true);
            } catch {
                mostrarToast("No se pudo descargar el archivo.", true);
            }
        });

        pedirUrlArchivo(comprobanteId, false)
            .then(function (url) {
                const elemento = document.createElement(esPdf ? "iframe" : "img");
                elemento.setAttribute(esPdf ? "title" : "alt", "Vista previa del comprobante");
                elemento.className = esPdf ? "w-100 h-100 border-0" : "mw-100 mh-100";
                elemento.addEventListener("error", function () {
                    mostrarMensaje("No se pudo cargar la vista previa.");
                });
                elemento.src = url;
                vistaPrevia.replaceChildren(elemento);
                botonDescargar.disabled = false;
            })
            .catch(function () {
                mostrarMensaje("No se pudo cargar la vista previa.");
            });
    }

    function prepararModalEliminar() {
        const modal = document.getElementById("modal-eliminar");
        if (!modal) {
            return;
        }

        const nombre = document.getElementById("nombre-eliminar");
        const botonConfirmar = document.getElementById("confirmar-eliminar");
        const cuadroError = document.getElementById("error-eliminar");
        let comprobanteId = null;

        modal.addEventListener("show.bs.modal", function (evento) {
            comprobanteId = evento.relatedTarget.dataset.id;
            nombre.textContent = evento.relatedTarget.dataset.nombre;
            cuadroError.hidden = true;
        });

        botonConfirmar.addEventListener("click", async function () {
            botonConfirmar.disabled = true;
            cuadroError.hidden = true;

            const respuesta = await enviar(`/api/comprobantes/${comprobanteId}`, { metodo: "DELETE" });
            if (respuesta.ok) {
                if (modal.dataset.urlDestino) {
                    window.location.href = modal.dataset.urlDestino;
                } else {
                    window.location.reload();
                }
                return;
            }

            cuadroError.textContent = respuesta.mensaje;
            cuadroError.hidden = false;
            botonConfirmar.disabled = false;
        });
    }

    function prepararFiltrosAutomaticos() {
        document.querySelectorAll("form[data-enviar-al-cambiar]").forEach(function (formulario) {
            formulario.querySelectorAll("select").forEach(function (select) {
                select.addEventListener("change", function () {
                    formulario.submit();
                });
            });
        });
    }

    function mostrarMensajePendiente() {
        const mensaje = document.getElementById("toast")?.dataset.mensaje;
        if (mensaje) {
            mostrarToast(mensaje, false);
        }
    }

    window.GestorGastos = { enviar, mostrarToast };

    document.addEventListener("DOMContentLoaded", function () {
        prepararVistaPrevia();
        prepararModalEliminar();
        prepararFiltrosAutomaticos();
        mostrarMensajePendiente();
    });
})();
