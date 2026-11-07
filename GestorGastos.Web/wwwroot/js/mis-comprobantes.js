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
