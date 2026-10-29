// Selección de un asiento en el mapa de la sala. Es JavaScript sin librerías y solo agrega
// comodidad: sin él, la página igual muestra el estado de cada asiento.
//
// Qué asiento se puede elegir no se decide acá: lo marca el presenter con
// data-seleccionable. Este archivo solo refleja la elección en pantalla. Se elige un solo
// asiento porque, según el contrato con Booking, cada solicitud reserva un solo asiento.
// Todavía no envía nada; la solicitud de reserva es el issue #4.
(function () {
    "use strict";

    const sala = document.querySelector("[data-mapa-asientos]");
    if (!sala) {
        return;
    }

    const textoResumen = document.querySelector("[data-resumen-texto]");
    const botonQuitar = document.querySelector("[data-resumen-quitar]");
    const textoInicial = textoResumen.textContent;
    let seleccionado = null;

    function seleccionar(asiento) {
        if (seleccionado) {
            seleccionado.setAttribute("aria-pressed", "false");
        }

        seleccionado = asiento;
        asiento.setAttribute("aria-pressed", "true");
        textoResumen.textContent = "Elegiste el asiento " + asiento.dataset.etiqueta + ".";
        botonQuitar.hidden = false;
    }

    function quitarSeleccion() {
        if (seleccionado) {
            seleccionado.setAttribute("aria-pressed", "false");
            seleccionado.focus();
        }

        seleccionado = null;
        textoResumen.textContent = textoInicial;
        botonQuitar.hidden = true;
    }

    // Un solo manejador para toda la sala en lugar de uno por asiento: con cientos de
    // asientos es más liviano y no hay que volver a asignarlo si cambia el mapa.
    sala.addEventListener("click", function (evento) {
        const asiento = evento.target.closest(".asiento");
        if (!asiento || !sala.contains(asiento)) {
            return;
        }

        if (asiento.dataset.seleccionable !== "true") {
            // Un clic en un asiento que no se puede elegir no selecciona nada, pero explica
            // por qué en el resumen, además de la descripción que aparece al pasar el mouse.
            textoResumen.textContent = asiento.dataset.descripcion + ".";
            return;
        }

        if (asiento === seleccionado) {
            quitarSeleccion();
        } else {
            seleccionar(asiento);
        }
    });

    botonQuitar.addEventListener("click", quitarSeleccion);
})();
