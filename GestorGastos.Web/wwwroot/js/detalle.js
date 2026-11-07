(function () {
    const vistaPrevia = document.getElementById('vista-previa');
    const botonDescargar = document.getElementById('boton-descargar');
    const comprobanteId = vistaPrevia.dataset.comprobanteId;
    const esPdf = vistaPrevia.dataset.contentType === 'application/pdf';

    function mostrarMensajeVistaPrevia(texto) {
        vistaPrevia.replaceChildren(document.createTextNode(texto));
    }

    async function pedirUrlArchivo(descarga) {
        const respuesta = await fetch('/api/comprobantes/' + comprobanteId + '/archivo?descarga=' + descarga);
        if (!respuesta.ok) {
            throw new Error('No se pudo obtener el archivo.');
        }
        const datos = await respuesta.json();
        return datos.url;
    }

    async function cargarVistaPrevia() {
        try {
            const url = await pedirUrlArchivo(false);
            const elemento = document.createElement(esPdf ? 'iframe' : 'img');
            elemento.setAttribute(esPdf ? 'title' : 'alt', 'Vista previa del comprobante');
            elemento.className = esPdf ? 'w-100 h-100 border-0' : 'mw-100 mh-100';
            elemento.addEventListener('error', function () {
                mostrarMensajeVistaPrevia('No se pudo cargar la vista previa.');
            });
            elemento.src = url;
            vistaPrevia.replaceChildren(elemento);
            botonDescargar.disabled = false;
        } catch {
            mostrarMensajeVistaPrevia('No se pudo cargar la vista previa.');
        }
    }

    botonDescargar.addEventListener('click', async function () {
        try {
            window.location.href = await pedirUrlArchivo(true);
        } catch {
            mostrarMensajeVistaPrevia('No se pudo descargar el archivo.');
        }
    });

    cargarVistaPrevia();
})();

(function () {
    const botonConfirmar = document.getElementById('confirmar-eliminar');
    if (!botonConfirmar) {
        return;
    }

    const comprobanteId = document.getElementById('vista-previa').dataset.comprobanteId;
    const cuadroError = document.getElementById('error-eliminar');

    function tokenAntiforgery() {
        return document.querySelector('input[name="__RequestVerificationToken"]').value;
    }

    function mostrarError(mensaje) {
        cuadroError.textContent = mensaje;
        cuadroError.classList.remove('d-none');
    }

    botonConfirmar.addEventListener('click', async function () {
        botonConfirmar.disabled = true;
        cuadroError.classList.add('d-none');

        try {
            const respuesta = await fetch('/api/comprobantes/' + comprobanteId, {
                method: 'DELETE',
                headers: { 'RequestVerificationToken': tokenAntiforgery() }
            });

            if (respuesta.status === 204) {
                window.location.href = '/Comprobantes';
                return;
            }

            const problema = await respuesta.json().catch(() => null);
            mostrarError((problema && problema.detail) || 'No se pudo eliminar el comprobante.');
        } catch {
            mostrarError('No se pudo conectar con el servidor.');
        }

        botonConfirmar.disabled = false;
    });
})();