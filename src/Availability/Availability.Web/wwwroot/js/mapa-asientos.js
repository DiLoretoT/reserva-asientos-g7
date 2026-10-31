// Selección de un asiento en el mapa de la sala. Es JavaScript sin librerías y solo agrega
// comodidad: sin él, la página igual muestra el estado de cada asiento.
//
// Qué asiento se puede elegir no se decide acá: lo marca el presenter con
// data-seleccionable. Este archivo solo refleja la elección en pantalla. Se elige un solo
// asiento porque, según el contrato con Booking, cada solicitud reserva un solo asiento.
//
// Si el usuario inició sesión, la página trae el formulario de reserva (issue #4): al
// elegir un asiento, este archivo completa su Id en el campo oculto y habilita Reservar. Sin
// sesión no hay formulario, y la selección solo sirve para ver el estado del asiento.
(function () {
    "use strict";

    const sala = document.querySelector("[data-mapa-asientos]");
    if (!sala) {
        return;
    }

    const textoResumen = document.querySelector("[data-resumen-texto]");
    const botonQuitar = document.querySelector("[data-resumen-quitar]");
    const textoInicial = textoResumen.textContent;

    // Solo existen si el usuario inició sesión.
    const formulario = document.querySelector("[data-form-reserva]");
    const campoAsiento = document.querySelector("[data-asiento-elegido]");
    const botonReservar = document.querySelector("[data-boton-reservar]");

    let seleccionado = null;

    // Refleja el asiento elegido en el formulario: el Id va en el campo oculto y Reservar
    // queda habilitado solo si hay un asiento elegido.
    function actualizarFormulario() {
        if (!formulario) {
            return;
        }

        campoAsiento.value = seleccionado ? seleccionado.dataset.asientoId : "";
        botonReservar.disabled = !seleccionado;
    }

    function seleccionar(asiento) {
        if (seleccionado) {
            seleccionado.setAttribute("aria-pressed", "false");
        }

        seleccionado = asiento;
        asiento.setAttribute("aria-pressed", "true");
        textoResumen.textContent = "Elegiste el asiento " + asiento.dataset.etiqueta + ".";
        botonQuitar.hidden = false;
        actualizarFormulario();
    }

    function quitarSeleccion() {
        if (seleccionado) {
            seleccionado.setAttribute("aria-pressed", "false");
            seleccionado.focus();
        }

        seleccionado = null;
        textoResumen.textContent = textoInicial;
        botonQuitar.hidden = true;
        actualizarFormulario();
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

    if (formulario) {
        formulario.addEventListener("submit", function (evento) {
            // Sin asiento elegido no se envía nada, por si el botón se habilitó de otra forma.
            if (!campoAsiento.value) {
                evento.preventDefault();
                return;
            }

            // Se deshabilita Reservar al enviar para que un doble clic no mande dos
            // solicitudes del mismo asiento.
            botonReservar.disabled = true;
            textoResumen.textContent = "Enviando tu solicitud del asiento " + seleccionado.dataset.etiqueta + "…";
        });
    }
})();
