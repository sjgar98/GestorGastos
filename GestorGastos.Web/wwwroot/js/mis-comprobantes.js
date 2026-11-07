(function () {
    "use strict";

    const formulario = document.getElementById("formFiltros");
    if (!formulario) {
        return;
    }

    formulario.querySelectorAll("select").forEach(function (select) {
        select.addEventListener("change", function () {
            formulario.submit();
        });
    });
})();

(function () {
    "use strict";

    const modal = document.getElementById("modal-eliminar");
    if (!modal) {
        return;
    }

    const nombre = document.getElementById("nombre-eliminar");
    const botonConfirmar = document.getElementById("confirmar-eliminar");
    const cuadroError = document.getElementById("error-eliminar");
    let comprobanteId = null;

    function mostrarError(mensaje) {
        cuadroError.textContent = mensaje;
        cuadroError.classList.remove("d-none");
    }

    function tokenAntiforgery() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? "";
    }

    modal.addEventListener("show.bs.modal", function (evento) {
        comprobanteId = evento.relatedTarget.dataset.id;
        nombre.textContent = evento.relatedTarget.dataset.nombre;
        cuadroError.classList.add("d-none");
    });

    botonConfirmar.addEventListener("click", async function () {
        botonConfirmar.disabled = true;
        cuadroError.classList.add("d-none");

        try {
            const respuesta = await fetch(`/api/comprobantes/${comprobanteId}`, {
                method: "DELETE",
                headers: { RequestVerificationToken: tokenAntiforgery() }
            });

            if (respuesta.status === 204) {
                location.reload();
                return;
            }

            const problema = await respuesta.json().catch(() => null);
            mostrarError(problema?.detail || "No se pudo eliminar el comprobante.");
        } catch {
            mostrarError("No se pudo conectar con el servidor. Revisá tu conexión e intentá de nuevo.");
        }

        botonConfirmar.disabled = false;
    });
})();
