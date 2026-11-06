(function () {
    "use strict";

    const TAMANIO_MAXIMO_BYTES = 4 * 1024 * 1024;
    const EXTENSIONES_PERMITIDAS = [".pdf", ".jpg", ".jpeg", ".png"];
    const PAGINAS_ANALIZADAS = 2;

    const formulario = document.getElementById("formCarga");
    if (!formulario) {
        return;
    }

    const inputArchivo = document.getElementById("Archivo");
    const feedbackArchivo = document.getElementById("archivoFeedback");
    const contenedorInput = document.getElementById("archivoInputWrap");
    const archivoSeleccionado = document.getElementById("archivoSeleccionado");
    const miniatura = document.getElementById("archivoMiniatura");
    const iconoPdf = document.getElementById("archivoIconoPdf");
    const nombreArchivo = document.getElementById("archivoNombre");
    const tamanioArchivo = document.getElementById("archivoTamanio");
    const avisoPaginas = document.getElementById("archivoAvisoPaginas");
    const botonQuitar = document.getElementById("btnQuitar");
    const botonSubir = document.getElementById("btnSubir");

    let urlMiniatura = null;

    function validarArchivo(archivo) {
        if (!archivo || archivo.size === 0) {
            return "Seleccioná un archivo.";
        }
        const nombre = archivo.name.toLowerCase();
        if (!EXTENSIONES_PERMITIDAS.some((extension) => nombre.endsWith(extension))) {
            return "Formato no permitido. Usá PDF, JPG o PNG.";
        }
        if (archivo.size > TAMANIO_MAXIMO_BYTES) {
            return "El archivo supera los 4 MB.";
        }
        return null;
    }

    function mostrarErrorArchivo(mensaje) {
        inputArchivo.classList.toggle("is-invalid", Boolean(mensaje));
        feedbackArchivo.textContent = mensaje ?? "";
    }

    function formatearTamanio(bytes) {
        if (bytes < 1024 * 1024) {
            return `${Math.max(1, Math.round(bytes / 1024))} KB`;
        }
        return `${(bytes / (1024 * 1024)).toFixed(1).replace(".", ",")} MB`;
    }

    async function contarPaginasPdf(archivo) {
        const texto = new TextDecoder("latin1").decode(await archivo.arrayBuffer());
        return (texto.match(/\/Type\s*\/Page(?![a-zA-Z])/g) ?? []).length;
    }

    function liberarMiniatura() {
        if (urlMiniatura) {
            URL.revokeObjectURL(urlMiniatura);
            urlMiniatura = null;
        }
        miniatura.removeAttribute("src");
    }

    async function mostrarArchivoSeleccionado(archivo) {
        liberarMiniatura();
        const esPdf = archivo.name.toLowerCase().endsWith(".pdf");

        nombreArchivo.textContent = archivo.name;
        tamanioArchivo.textContent = formatearTamanio(archivo.size);
        iconoPdf.hidden = !esPdf;
        miniatura.hidden = esPdf;
        avisoPaginas.hidden = true;

        if (!esPdf) {
            urlMiniatura = URL.createObjectURL(archivo);
            miniatura.src = urlMiniatura;
        }

        contenedorInput.hidden = true;
        archivoSeleccionado.hidden = false;

        if (esPdf) {
            try {
                avisoPaginas.hidden = (await contarPaginasPdf(archivo)) <= PAGINAS_ANALIZADAS;
            } catch {
                avisoPaginas.hidden = true;
            }
        }
    }

    function quitarArchivo() {
        liberarMiniatura();
        inputArchivo.value = "";
        archivoSeleccionado.hidden = true;
        contenedorInput.hidden = false;
        mostrarErrorArchivo(null);
    }

    function restaurarBotonSubir() {
        botonSubir.disabled = false;
        botonSubir.querySelector(".spinner-border").hidden = true;
        botonSubir.querySelector(".texto-boton").textContent = "Subir y analizar";
    }

    inputArchivo.addEventListener("change", () => {
        const archivo = inputArchivo.files[0];
        if (!archivo) {
            quitarArchivo();
            return;
        }

        const error = validarArchivo(archivo);
        if (error) {
            inputArchivo.value = "";
            mostrarErrorArchivo(error);
            return;
        }

        mostrarErrorArchivo(null);
        mostrarArchivoSeleccionado(archivo);
    });

    botonQuitar.addEventListener("click", () => {
        quitarArchivo();
        inputArchivo.focus();
    });

    formulario.addEventListener("submit", (evento) => {
        const errorArchivo = validarArchivo(inputArchivo.files[0]);
        mostrarErrorArchivo(errorArchivo);
        const camposValidos = window.jQuery ? window.jQuery(formulario).valid() : true;

        if (errorArchivo || !camposValidos) {
            evento.preventDefault();
            return;
        }

        botonSubir.disabled = true;
        botonSubir.querySelector(".spinner-border").hidden = false;
        botonSubir.querySelector(".texto-boton").textContent = "Subiendo…";
    });

    window.addEventListener("pageshow", restaurarBotonSubir);
})();
