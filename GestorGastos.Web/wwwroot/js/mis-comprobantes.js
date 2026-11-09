(function () {
    "use strict";

    const INTERVALO_CONSULTA_MS = 3000;
    const ESPERA_MAXIMA_MS = 2 * 60 * 1000;

    function crearBadge(estado) {
        const badge = document.createElement("span");
        badge.className = estado.claseBadge;
        badge.textContent = estado.estadoTexto;
        return badge;
    }

    function actualizarFila(fila, estado) {
        fila.querySelector('[data-celda="estado"]').replaceChildren(crearBadge(estado));
        fila.querySelector('[data-celda="monto"]').textContent = estado.montoTexto;
        const botonEliminar = fila.querySelector('[data-bs-target="#modal-eliminar"]');
        if (botonEliminar) {
            botonEliminar.hidden = !estado.editable;
        }
    }

    function seguirFila(fila) {
        const inicio = Date.now();

        async function consultar() {
            if (Date.now() - inicio >= ESPERA_MAXIMA_MS) {
                return;
            }

            const respuesta = await window.GestorGastos.enviar(`/api/comprobantes/${fila.dataset.id}/estado`);
            if (respuesta.ok && respuesta.datos.estado !== "Procesando") {
                actualizarFila(fila, respuesta.datos);
                return;
            }

            setTimeout(consultar, INTERVALO_CONSULTA_MS);
        }

        setTimeout(consultar, INTERVALO_CONSULTA_MS);
    }

    document.querySelectorAll("tr[data-procesando]").forEach(seguirFila);
})();
