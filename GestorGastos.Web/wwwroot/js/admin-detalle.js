(function () {
    "use strict";

    const token = document
        .querySelector('input[name="__RequestVerificationToken"]')
        ?.value ?? "";

    // ── Preview ──────────────────────────────────────────────────────────────

    async function cargarPreview() {
        const contenedor = document.getElementById("contenedor-preview");
        const btnDescargar = document.getElementById("btn-descargar");

        try {
            const resp = await fetch(
                `/api/comprobantes/${COMPROBANTE_ID}/archivo?descarga=false`,
                { headers: { RequestVerificationToken: token } }
            );

            if (!resp.ok) {
                contenedor.innerHTML =
                    '<span class="text-muted small">No se pudo cargar la vista previa.</span>';
                return;
            }

            const { url, contentType } = await resp.json();
            btnDescargar.href = `/api/comprobantes/${COMPROBANTE_ID}/archivo?descarga=true`;

            if (contentType === "application/pdf") {
                const iframe = document.createElement("iframe");
                iframe.src = url;
                iframe.className = "w-100 border-0";
                // Alto fijo para el iframe de PDF, Bootstrap no lo resuelve
                iframe.style.height = "420px";
                iframe.title = "Vista previa del comprobante";
                contenedor.replaceChildren(iframe);
            } else {
                const img = document.createElement("img");
                img.src = url;
                img.className = "img-fluid rounded";
                img.alt = "Vista previa del comprobante";
                img.style.maxHeight = "420px";
                img.style.objectFit = "contain";
                contenedor.replaceChildren(img);
            }
        } catch {
            contenedor.innerHTML =
                '<span class="text-muted small">No se pudo cargar la vista previa.</span>';
        }
    }

    // ── Toast ─────────────────────────────────────────────────────────────────

    function mostrarToast(mensaje, esError) {
        const toastEl = document.getElementById("toastAccion");
        const toastMensaje = document.getElementById("toastMensaje");
        toastEl.classList.remove("text-bg-success", "text-bg-danger");
        toastEl.classList.add(esError ? "text-bg-danger" : "text-bg-success");
        toastMensaje.textContent = mensaje;
        bootstrap.Toast.getOrCreateInstance(toastEl).show();
    }

    // ── Acción genérica ───────────────────────────────────────────────────────

    async function ejecutarAccion(ruta, body, mensajeExito) {
        const resp = await fetch(ruta, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                RequestVerificationToken: token,
            },
            body: body ? JSON.stringify(body) : undefined,
        });

        if (resp.ok) {
            mostrarToast(mensajeExito, false);
            setTimeout(() => location.reload(), 1200);
        } else {
            let mensaje = "Ocurrió un error. Intentá de nuevo.";
            try {
                const problema = await resp.json();
                if (problema.detail) mensaje = problema.detail;
            } catch { /* ignorar */ }

            if (resp.status === 409) {
                mostrarToast(mensaje, true);
                setTimeout(() => location.reload(), 2000);
            } else {
                mostrarToast(mensaje, true);
            }
        }
    }

    // ── Tomar en revisión ─────────────────────────────────────────────────────

    document.getElementById("btn-tomar")?.addEventListener("click", () => {
        ejecutarAccion(
            `/api/admin/comprobantes/${COMPROBANTE_ID}/tomar`,
            null,
            "Comprobante tomado en revisión."
        );
    });

    // ── Aprobar ───────────────────────────────────────────────────────────────

    document.getElementById("btn-aprobar")?.addEventListener("click", () => {
        ejecutarAccion(
            `/api/admin/comprobantes/${COMPROBANTE_ID}/aprobar`,
            null,
            "Comprobante aprobado."
        );
    });

    // ── Rechazar ──────────────────────────────────────────────────────────────

    const inputMotivo = document.getElementById("inputMotivo");
    const contadorMotivo = document.getElementById("contadorMotivo");
    const feedbackMotivo = document.getElementById("feedbackMotivo");

    inputMotivo?.addEventListener("input", () => {
        contadorMotivo.textContent = inputMotivo.value.length;
    });

    document.getElementById("btn-confirmar-rechazar")?.addEventListener("click", () => {
        const motivo = inputMotivo.value.trim();
        if (motivo.length < 10 || motivo.length > 500) {
            feedbackMotivo.style.visibility = "visible";
            inputMotivo.classList.add("is-invalid");
            return;
        }
        feedbackMotivo.style.visibility = "hidden";
        inputMotivo.classList.remove("is-invalid");

        bootstrap.Modal.getInstance(document.getElementById("modalRechazar")).hide();
        ejecutarAccion(
            `/api/admin/comprobantes/${COMPROBANTE_ID}/rechazar`,
            { motivo },
            "Comprobante rechazado."
        );
    });

    // ── Marcar pagado ─────────────────────────────────────────────────────────

    const inputFechaPago = document.getElementById("inputFechaPago");
    const feedbackFechaPago = document.getElementById("feedbackFechaPago");

    document.getElementById("btn-confirmar-pagar")?.addEventListener("click", () => {
        const fechaStr = inputFechaPago?.value;
        if (!fechaStr) {
            inputFechaPago?.classList.add("is-invalid");
            feedbackFechaPago?.classList.add("d-block");
            return;
        }

        const hoy = new Date().toISOString().slice(0, 10);
        if (fechaStr > hoy || fechaStr < FECHA_CARGA_MIN) {
            inputFechaPago.classList.add("is-invalid");
            feedbackFechaPago.classList.add("d-block");
            return;
        }

        inputFechaPago.classList.remove("is-invalid");
        feedbackFechaPago?.classList.remove("d-block");

        bootstrap.Modal.getInstance(document.getElementById("modalPagar")).hide();
        ejecutarAccion(
            `/api/admin/comprobantes/${COMPROBANTE_ID}/pagar`,
            { fechaPago: fechaStr },
            "Comprobante marcado como pagado."
        );
    });

    // ── Init ──────────────────────────────────────────────────────────────────

    cargarPreview();
})();
