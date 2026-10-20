# The Hollow Protocol

**Survival horror en primera persona hecho con Unity 6.** / *First-person survival horror made with Unity 6.*

> "El verdadero horror no es lo que creó la ciencia… sino en lo que podrías convertirte."

Proyecto final del curso *Profesión Desarrollo de videojuegos con Unity* — EBAC (Escuela Británica de Artes Creativas y Tecnología).

---

## Historia

Despiertas en un laboratorio biotecnológico aislado. Una terminal te indica seguir la luz roja de la Torre 7. Afuera, entre la niebla de la noche, algo camina a lo lejos y te vigila. Para escapar debes encontrar la brújula, la llave de la cabaña y el registro que explica lo que pasó, mientras una criatura que no puede morir te acosa hasta el final.

## Cómo jugar

| Control | Acción |
|---|---|
| **W A S D** | Moverse |
| **Mouse** | Mirar |
| **Shift** | Correr |
| **Espacio** | Saltar |
| **E** | Interactuar (puertas, notas, llaves, brújula) |
| **F** | Linterna |
| **Clic izquierdo** | Disparar (cuando tienes la pistola) |
| **R** | Recargar (cargador de 8; la reserva se consigue en cajas de munición) |
| **Esc** | Pausa |

**Recorrido:** laboratorio → torre (brújula y llave) → casa vieja (pistas) → cabaña (registro) → final.

## Mecánicas

- **Exploración e interacción:** puertas con bisagra y llave, tarjeta de acceso, notas y pistas, inventario de llaves.
- **Brújula:** barra superior con rumbo en grados; las pistas marcan destinos con su distancia aproximada.
- **Pistola:** disparo por raycast con trazador (`LineRenderer`), chispa de impacto y daño. Cargador de 8 balas, recarga con R y cajas de munición (hay una de 6 balas sobre la cama de la torre).
- **Vida del jugador:** barra de vida en pantalla; cada ataque del enemigo quita una cuarta parte. Si llega a cero, pantalla de muerte con reiniciar o volver al menú.
- **Enemigo "Rastreador"** (arquetipo del GDD, sección 5): oye tus pasos y disparos, te ve en un cono de visión (más lejos si llevas la linterna encendida), te busca donde te perdió, y **no muere**: al recibir daño o tocarte se retira y regresa más tarde. Primer avistamiento a lo lejos al salir del laboratorio y jump scare final.
- **Ambiente:** niebla nocturna, sonido por zonas, pasos, luces parpadeantes y un bosque generado sobre el terreno.
- **Interfaz:** menú principal (Jugar / Opciones / Salir), menú de pausa, HUD de brújula y mensajes, pantalla final con botones.

## Requisitos y ejecución

- **Ejecutable (Windows 64 bits):** descarga y descomprime `Build_Videojuego.zip`, luego abre `TheHollowProtocol.exe`.
  - Enlace de descarga: _(se agrega al hacer la entrega)_
- **Desde el código:** Unity **6000.0.62f1** con URP. Abre la carpeta del proyecto y carga `Assets/Scenes/Main Menu.unity`.

## Estructura del repositorio

```
.
├── Assets/               ← proyecto de Unity (escenas, scripts, modelos, audio)
├── Packages/ ProjectSettings/
├── Diseño/
│   ├── Documentos/       ← GDD de 10 páginas, documentación Doxygen (.zip)
│   ├── Modelado 3D/      ← información de los modelos 3D (hechos en Maya)
│   └── Arte (Opcional)/
├── CREDITS.txt           ← atribuciones de modelos y sonidos de terceros
├── LICENSE
└── README.md
```

## Documentación

- **GDD (10 páginas):** `Diseño/Documentos/GDD_TheHollowProtocol_10_paginas.pdf`
- **Doxygen:** `Diseño/Documentos/Documentacion_Doxygen_TheHollowProtocol.zip` (abrir `index.html`). Se regenera con `Diseño/Documentos/Doxygen/Doxyfile`.

## Git LFS

El repositorio usa **Git LFS** para escenas, imágenes, modelos, audio y comprimidos (ver `.gitattributes`). Para clonarlo:

```bash
git lfs install
git clone <url-del-repositorio>
```

## Créditos

- Modelo del monstruo: *"Smily horror monster"* (https://skfb.ly/6W6ut) by **Bento**, licenciado bajo [Creative Commons Attribution 4.0](http://creativecommons.org/licenses/by/4.0/).
- Paquetes de terreno, árboles y pasto: Realistic Terrain Textures Lite, Realistic Tree y GrassFlowers (ver sus licencias en Assets).
- Efectos de sonido y ambientes de terceros (freesound, pixabay y otros): ver `CREDITS.txt`.

---

## English summary

*The Hollow Protocol* is a first-person survival horror game built in **Unity 6 (URP)**. You wake up in an isolated biotech lab and must reach a cabin to recover a record while an unkillable creature stalks you. Features include a compass and clue system, key/door puzzles, a pistol, a sound- and sight-based tracker AI (`Enemy.cs`) with a first distant sighting and a final jump scare, night fog, a main menu and a pause menu.

**Build:** unzip `Build_Videojuego.zip` and run `TheHollowProtocol.exe` (Windows 64-bit). **Source:** Unity 6000.0.62f1, open `Assets/Scenes/Main Menu.unity`. **Docs:** GDD and Doxygen in `Diseño/Documentos`. The repo uses **Git LFS**.
