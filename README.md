Drone Escape

Descripción

Este es un sencillo juego de drones desarrollado en Unity. El jugador controla un dron que puede moverse en diferentes direcciones mientras evita obstáculos. Cuando el dron choca, aparece un menú de "Game Over" desde donde puede reiniciarse el juego.

Características

Movimiento del dron: El dron puede moverse lateralmente y verticalmente utilizando las teclas del teclado.

Colisiones: El juego detecta colisiones con obstáculos y edificios.

Menú de Game Over: Al chocar, se muestra un menú que permite reiniciar el juego.

Sistema de pausado: El juego se detiene al mostrar el menú de Game Over.

Controles

Movimiento horizontal: Teclas A (izquierda) y D (derecha) o las flechas del teclado.

Movimiento vertical: Teclas W (arriba) y S (abajo) o las flechas del teclado.

Reiniciar el juego: Clic en el botón "Restart" en el menú de Game Over.

Requisitos

Unity 2021.3 o superior.

TextMeshPro instalado (puede sustituirse por otro sistema de texto si se prefiere).

Estructura del Proyecto

Scripts:

DroneController: Controla el movimiento del dron y detecta colisiones.

GameController: Gestiona el estado del juego y el menú de Game Over.

Assets:

Prefabs para el dron, obstáculos y otros elementos del entorno.

Canvas para el menú de Game Over.

Instalación y Ejecución

Clona este repositorio o descarga el código.

Abre el proyecto en Unity.

Asegúrate de tener TextMeshPro importado desde el gestor de paquetes de Unity.

Ejecuta el juego desde el editor de Unity presionando el botón de Play.

Personalización

Velocidad del dron: Se puede ajustar en el componente DroneController cambiando los valores de lateralSpeed y verticalSpeed.

Texto del Game Over: Modifica el texto en el componente TextMeshPro dentro del Canvas del menú de Game Over.

Diseño del menú: Personaliza el Canvas y sus elementos gráficos para adaptarlos a tus necesidades.

Futuras Mejoras

Añadir niveles o un sistema de puntuación.

Implementar efectos de sonido y música.

Mejorar los gráficos y animaciones del dron.

Habilitar soporte para controladores externos.

Contribuciones

¡Este proyecto está abierto a contribuciones! Si deseas agregar nuevas características o corregir errores, no dudes en enviar un pull request.

Licencia

Este proyecto se publica bajo la licencia MIT. Puedes usarlo, modificarlo y distribuirlo libremente siempre que incluyas una copia de esta licencia.

¡Gracias por jugar Drone Escape! 🚀