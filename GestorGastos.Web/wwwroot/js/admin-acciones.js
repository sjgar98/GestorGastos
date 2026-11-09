(function () {
    "use strict";

    const LARGO_MINIMO_MOTIVO = 10;
    const LARGO_MAXIMO_MOTIVO = 500;
    const MENSAJES_EXITO = {
        tomar: "Comprobante tomado en revisión.",
        aprobar: "Comprobante aprobado.",
        rechazar: "Comprobante rechazado.",
        pagar: "Comprobante marcado como pagado."
    };

    const { enviar, mostrarToast } = window.GestorGastos;

    async function ejecutarAccion(id, accion, cuerpo) {
        const respuesta = await enviar(`/api/admin/comprobantes/${id}/${accion}`, { metodo: "POST", cuerpo });
        if (respuesta.ok) {
            mostrarToast(MENSAJES_EXITO[accion], false);
            setTimeout(() => location.reload(), 1200);
            return true;
        }

        mostrarToast(respuesta.mensaje, true);
        if (respuesta.status === 409) {
            setTimeout(() => location.reload(), 2000);
        }
        return false;
    }

    document.querySelectorAll("[data-accion-admin]").forEach(function (boton) {
        boton.addEventListener("click", async function () {
            boton.disabled = true;
            if (!await ejecutarAccion(boton.dataset.id, boton.dataset.accionAdmin)) {
                boton.disabled = false;
            }
        });
    });

    function prepararModal(idModal, alAbrir, alConfirmar) {
        const modal = document.getElementById(idModal);
        if (!modal) {
            return;
        }

        let comprobanteId = null;
        modal.addEventListener("show.bs.modal", function (evento) {
            comprobanteId = evento.relatedTarget.dataset.id;
            alAbrir(evento.relatedTarget);
        });

        modal.querySelector(".modal-footer .btn:last-child").addEventListener("click", async function () {
            const cuerpo = alConfirmar();
            if (!cuerpo) {
                return;
            }
            bootstrap.Modal.getInstance(modal).hide();
            await ejecutarAccion(comprobanteId, cuerpo.accion, cuerpo.datos);
        });
    }

    const motivo = document.getElementById("motivo-rechazo");
    const contadorMotivo = document.getElementById("contador-motivo");
    motivo?.addEventListener("input", function () {
        contadorMotivo.textContent = motivo.value.length;
        motivo.classList.remove("is-invalid");
    });

    prepararModal("modal-rechazar",
        function () {
            motivo.value = "";
            contadorMotivo.textContent = "0";
            motivo.classList.remove("is-invalid");
        },
        function () {
            const texto = motivo.value.trim();
            const valido = texto.length >= LARGO_MINIMO_MOTIVO && texto.length <= LARGO_MAXIMO_MOTIVO;
            motivo.classList.toggle("is-invalid", !valido);
            return valido ? { accion: "rechazar", datos: { motivo: texto } } : null;
        });

    const fechaPago = document.getElementById("fecha-pago");
    prepararModal("modal-pagar",
        function (boton) {
            fechaPago.min = boton.dataset.fechaCarga;
            fechaPago.value = fechaPago.max;
            fechaPago.classList.remove("is-invalid");
        },
        function () {
            const fecha = fechaPago.value;
            const valida = !!fecha && fecha >= fechaPago.min && fecha <= fechaPago.max;
            fechaPago.classList.toggle("is-invalid", !valida);
            return valida ? { accion: "pagar", datos: { fechaPago: fecha } } : null;
        });
})();
