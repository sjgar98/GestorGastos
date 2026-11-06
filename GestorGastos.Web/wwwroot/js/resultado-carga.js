(function () {
    "use strict";

    const INTERVALO_CONSULTA_MS = 3000;
    const ESPERA_MAXIMA_MS = 2 * 60 * 1000;

    const tarjeta = document.getElementById("tarjetaResultado");
    if (!tarjeta) {
        return;
    }

    const comprobanteId = tarjeta.dataset.id;
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? "";

    function mostrarMensajeExito() {
        const toast = document.getElementById("toastResultado");
        const mensaje = toast?.dataset.mensaje;
        if (!mensaje) {
            return;
        }
        document.getElementById("toastResultadoMensaje").textContent = mensaje;
        bootstrap.Toast.getOrCreateInstance(toast).show();
    }

    function seguirExtraccion() {
        if (tarjeta.dataset.estado !== "Procesando") {
            return;
        }

        const panelAnalizando = document.getElementById("panelAnalizando");
        const panelDemora = document.getElementById("panelDemora");
        const inicio = Date.now();

        async function consultarEstado() {
            if (Date.now() - inicio >= ESPERA_MAXIMA_MS) {
                panelAnalizando.hidden = true;
                panelDemora.hidden = false;
                return;
            }

            try {
                const respuesta = await fetch(`/api/comprobantes/${comprobanteId}/estado`, {
                    headers: { Accept: "application/json" }
                });
                if (respuesta.ok) {
                    const comprobante = await respuesta.json();
                    if (comprobante.estado !== "Procesando") {
                        location.reload();
                        return;
                    }
                }
            } catch {
                // Un error de red puntual no corta el seguimiento: se vuelve a consultar en el próximo intervalo.
            }

            setTimeout(consultarEstado, INTERVALO_CONSULTA_MS);
        }

        setTimeout(consultarEstado, INTERVALO_CONSULTA_MS);
    }

    function prepararEliminacion() {
        const botonConfirmar = document.getElementById("btnConfirmarEliminar");
        const alertaError = document.getElementById("errorEliminar");
        if (!botonConfirmar) {
            return;
        }

        botonConfirmar.addEventListener("click", async () => {
            botonConfirmar.disabled = true;
            alertaError.hidden = true;

            let mensaje = "No se pudo eliminar el comprobante. Intentá de nuevo.";
            try {
                const respuesta = await fetch(`/api/comprobantes/${comprobanteId}`, {
                    method: "DELETE",
                    headers: { RequestVerificationToken: token }
                });
                if (respuesta.ok) {
                    location.href = "/Comprobantes";
                    return;
                }
                const problema = await respuesta.json().catch(() => null);
                if (problema?.detail) {
                    mensaje = problema.detail;
                }
            } catch {
                mensaje = "No se pudo conectar con el servidor. Revisá tu conexión e intentá de nuevo.";
            }

            alertaError.textContent = mensaje;
            alertaError.hidden = false;
            botonConfirmar.disabled = false;
        });
    }

    mostrarMensajeExito();
    seguirExtraccion();
    prepararEliminacion();
})();
