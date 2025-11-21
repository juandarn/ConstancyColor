# VR Color & Light Lab 🕶💡

*Experimento de constancia del color en un entorno doméstico en realidad virtual*

---

## 🧩 Tecnologías usadas

* **Unity** (motor principal de desarrollo, versión 2021 LTS o similar)
* **XR Interaction Toolkit** (interacción VR, rayos, hand menu, agarrar paneles)
* **OpenXR / Oculus (Rift S)** como runtime de VR
* **TextMeshPro** (UI en VR)
* **FlexibleColorPicker** (asset gratuito para elegir colores de luz de forma continua)

---

## 🔧 Requisitos previos

Antes de abrir el proyecto o ejecutar la escena en VR, necesitas:

* **Unity 2021.x o superior** (recomendado LTS)
* Paquetes vía Package Manager:

  * `XR Interaction Toolkit`
  * `XR Plugin Management` (configurado con **OpenXR** u Oculus según tu visor)
  * `TextMeshPro`
* Un visor compatible:

  * Oculus **Rift S** (principal)
  * Cualquier visor compatible con OpenXR (Meta, Vive, etc.)
* Controladores VR con:

  * Gatillo para seleccionar en UI
  * Botón de agarre para coger el panel de control / menús

---

## 📘 ¿De qué se trata este proyecto?

Este proyecto es una **experiencia VR de laboratorio de iluminación** para estudiar la **constancia del color** en un entorno doméstico virtual.

El usuario se encuentra en una habitación con:

* **3 lámparas en el techo** y un objeto / cuadro de referencia.
* Un **panel flotante de ajustes de luz** que puede agarrar, mover y usar con rayos láser.
* Un sistema de **referencias fotográficas** y un **módulo de comparación** que evalúa qué tan bien el usuario iguala una condición de luz objetivo.

La idea es que la persona:

1. Lea las **reglas iniciales** (panel que sigue la vista de la cámara).
2. Use el panel para ajustar:

   * **Temperatura de color (Kelvin)**
   * **Color de filtro** (tinte) con un color picker
   * **Intensidad** de cada lámpara
3. Trate de igualar una **condición objetivo** (por ejemplo, la iluminación con la que se tomó una foto).
4. Pulse **Comparar** y reciba:

   * Un **puntaje global** (0–100%)
   * Un detalle por lámpara de qué tanto se acercó a la intensidad, temperatura y color esperados.

---

## ✨ Características principales

### 🔆 Control de iluminación por lámpara

* Script principal: **`LightUIController`**
* Permite:

  * Seleccionar **“Todas”** las luces o una individual mediante un **Dropdown**.
  * Ajustar **temperatura de color** (3000K, 4500K, 6000K o valor continuo).
  * Cambiar **modo Kelvin / Color** (toggle):

    * Modo Kelvin: la luz usa `useColorTemperature` y `colorTemperature`.
    * Modo Color/Filter: se usa el color directo del filtro.
  * Ajustar **intensidad** de 0 a ~10.
  * Cambiar **tono del filtro** mediante:

    * Sliders HSV / FlexibleColorPicker que llaman a `SetHSV(h, s, v)`.

### 🎨 Selector de color (FlexibleColorPicker)

* Integrado en el panel XR.
* La selección de color se aplica al filtro de la(s) luz(ces) activa(s):

  * Usa `SetFilterColor(Color c)` y `SetHSV(h, s, v)` del `LightUIController`.
* Permite probar fácilmente:

  * Colores neutros
  * Iluminaciones coloreadas (rojizas, azuladas, violáceas, etc.)

### 🧾 Panel de estado en vivo

* El script de comparación muestra en un **status panel**:

  * Luz seleccionada (Todas / Lamp_Light1 / Lamp_Light2 / Lamp_Light3)
  * Para cada lámpara:

    * Intensidad actual
    * Color en formato `#RRGGBB`
    * Temperatura Kelvin (si está activa)
* Actualiza varias veces por segundo para facilitar la depuración y la observación.

### 📸 Visor de imágenes de referencia

* Script: **`PhotoPopupController`**
* Función:

  * Mostrar un **canvas flotante** frente a la cámara con una **foto de referencia**.
  * El mismo botón va cambiando de estado:

    * `Ver foto` → muestra la primera imagen
    * `Siguiente` → cambia a la siguiente (hay 3 fotos configurables)
    * `Cerrar` → oculta el canvas y vuelve a “Ver foto”
* La imagen:

  * Se posiciona **frente al usuario** a una distancia fija.
  * Se escala automáticamente para mantener la proporción de la textura.

### 📋 Reglas iniciales / Pantalla de instrucciones

* Script: **`IntroRulesFlow`**
* Muestra un **panel de reglas** que:

  * Aparece **frente a la cámara** al inicio.
  * Opcionalmente **sigue la vista** mientras está activo (yaw-only).
* Textos típicos:

  * Mantente sentado o de pie de forma estable.
  * No muevas los objetos de la escena, solo usa el panel de ajustes.
  * Usa el gatillo para seleccionar en la UI y el botón de agarre para mover el panel.
  * Ajusta brillo, modo y color hasta que consideres que coincide con la referencia.
* Al pulsar “He leído / Continuar”:

  * Se oculta el panel de reglas.
  * Se muestra el **panel principal de ajustes**.

### 🧮 Comparación y puntaje

* Script principal: **`LightCompareController`**
* Funcionalidad:

  * Define **valores objetivo por lámpara** (intensidad, color, Kelvin), por ejemplo:

    * `Lamp_Light1` → Int = 3.05, Color = `#D2204C`, K = 6000
    * `Lamp_Light2` → Int = 10.00, Color = `#1648D2`, K = 3000
    * `Lamp_Light3` → Int = 6.48, Color = `#6E50D2`, K = 6000

  * Al pulsar el botón **Comparar**:

    1. Lee el estado actual de todas las lámparas.
    2. Calcula:

       * Coincidencia de **color** (en espacio HSV).
       * Coincidencia de **intensidad**.
       * Coincidencia de **temperatura de color**.
    3. Saca un **puntaje global (0–100%)** combinando esos factores.
    4. Muestra un **overlay delante de la cámara** con:

       * Resultado general (ej. “Muy bien, estás bastante cerca.”).
       * Detalle por lámpara:

         * Objetivo vs. Usuario (Int, K, Color #).
         * “Coincidencia aprox.” en % y explicación tipo:

           * “Color: bastante cercano”
           * “Intensidad: 80%”
           * “K: 90%”

### 🧾 Logging para análisis

* El experimento genera logs en la consola (Unity):

  * Cuando se comparan valores:

    * `========== LIGHT LOG: Comparar (estado actual del usuario) ==========`
    * Lista todas las lámparas con:

      * Intensidad final
      * Color `#RRGGBB`
      * Kelvin (si aplica)
  * `LightCompareController` también puede guardar distintos reportes en una lista (`compareHistory`) para exportar resultados de las sesiones.

---

## 🗂️ Estructura general del proyecto (simplificada)

* `Assets/Scenes/`

  * Escena principal del experimento (habitación + XR Origin + UI + luces).
* `Assets/Scripts/`

  * `LightUIController.cs` – Control básico de las luces.
  * `LightCompareController.cs` – Comparación, scoring y overlay de resultado.
  * `IntroRulesFlow.cs` – Pantalla de reglas inicial.
  * `PhotoPopupController.cs` – Visor de fotos flotante.
* `Assets/FlexibleColorPicker/`

  * Asset externo para seleccionar color de luz.

*(Los nombres y rutas exactas pueden variar según cómo tengas organizado tu proyecto, pero esta es la lógica general.)*

---

## 🧑‍💻 Modo desarrollo: abrir el proyecto en Unity

1. Abre **Unity Hub**.
2. Añade la carpeta del proyecto (`Add > Select folder`).
3. Ábrelo con la versión de Unity recomendada (2021.x LTS).
4. En **Edit → Project Settings → XR Plugin Management**:

   * Activa **OpenXR** (u Oculus, según tu visor).
5. En **Package Manager**, comprueba que estén:

   * `XR Interaction Toolkit`
   * `XR Plugin Management`
   * `TextMeshPro`
6. Abre la escena principal del experimento.
7. Asegúrate de que:

   * La cámara VR tenga el tag **MainCamera**.
   * El `LightUIController` tenga asignadas las 3 luces de techo.
   * El `LightCompareController` tenga referenciados:

     * `lightsUI`
     * `compareCanvas`
     * `compareText`
     * `statusText` (si usas panel de estado).

---

## ▶️ Guía rápida de uso en VR

1. **Iniciar la escena en Play** con el visor conectado.
2. **Leer el panel de instrucciones** que aparece frente a ti:

   * Gatillo → seleccionar botones y sliders.
   * Botón de agarre → mover panel principal.
3. Cuando pulses **“He leído / Continuar”**:

   * Se oculta el panel de reglas.
   * Aparece el **panel de ajustes de luz**.
4. Usa el panel para:

   * Elegir **qué lámpara** ajustar (Todas, 1, 2 o 3).
   * Cambiar **modo Kelvin/Color**.
   * Ajustar **intensidad** con el slider.
   * Ajustar **color** con el color picker.
   * Opcionalmente mostrar una **foto de referencia** con el botón de “Ver foto / Siguiente / Cerrar”.
5. Cuando creas que has igualado la condición de luz deseada:

   * Pulsa el botón **Comparar**.
   * Se mostrará un **overlay delante de tu vista** con el resultado:

     * Puntaje global.
     * Qué tan cerca estuviste en cada lámpara.
6. Cierra el overlay y puedes volver a ajustar las luces para otro intento.

---

## 📊 Uso para experimento / estudio

Este proyecto está pensado para:

* Evaluar **qué tan bien los usuarios pueden igualar una condición de iluminación objetivo** usando:

  * Temperatura de color
  * Intensidad
  * Tonalidad (tinte de color)
* Registrar:

  * Parámetros finales de cada intento.
  * Puntaje de coincidencia.
  * Diferencias por lámpara en intensidad, Kelvin y color.

A partir de los logs se puede:

* Analizar **patrones de error**.
* Estudiar **constancia del color** bajo diferentes combinaciones de luces.
* Comparar desempeño entre participantes o condiciones experimentales.
