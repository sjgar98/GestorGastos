(function ($) {
    "use strict";

    if (!$ || !$.validator || !$.validator.unobtrusive) {
        return;
    }

    $.validator.setDefaults({
        highlight: function (elemento) {
            elemento.classList.add("is-invalid");
        },
        unhighlight: function (elemento) {
            elemento.classList.remove("is-invalid");
        }
    });

    $.validator.addMethod("fechanofutura", function (valor, elemento, maximo) {
        return this.optional(elemento) || valor <= maximo;
    });
    $.validator.unobtrusive.adapters.addSingleVal("fechanofutura", "max");

    $.validator.addMethod("dosdecimales", function (valor, elemento) {
        return this.optional(elemento) || /^\d+(\.\d{1,2})?$/.test(valor);
    });
    $.validator.unobtrusive.adapters.addBool("dosdecimales");
})(window.jQuery);
