(function () {
    "use strict";

    const INTERVALO_CONSULTA_MS = 3000;
    const ESPERA_MAXIMA_MS = 2 * 60 * 1000;

    const tarjeta = document.getElementById("tarjetaResultado");
    if (!tarjeta || tarjeta.dataset.estado !== "Procesando") {
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

        const respuesta = await window.GestorGastos.enviar(`/api/comprobantes/${tarjeta.dataset.id}/estado`);
        if (respuesta.ok && respuesta.datos.estado !== "Procesando") {
            location.reload();
            return;
        }

        setTimeout(consultarEstado, INTERVALO_CONSULTA_MS);
    }

    setTimeout(consultarEstado, INTERVALO_CONSULTA_MS);
})();
