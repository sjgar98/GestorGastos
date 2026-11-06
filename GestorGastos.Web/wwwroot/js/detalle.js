(function() {
    const botonConfirmar = document.getElementById('confirmar-eliminar');
    if (!botonConfirmar) return;

    const comprobanteId = document.getElementById('vista-previa').dataset.comprobanteId;
    const cuadroError = document.getElementById('error-eliminar');

    function tokenAntiforgery() {
        return document.querySelector('input[name="__RequestVerificationToken"]').value;
    }

    function mostrarError(mensaje) {
        cuadroError.textContent = mensaje;
        cuadroError.classList.remove('d-none');
    }

    botonConfirmar.addEventListener('click', async function() {
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