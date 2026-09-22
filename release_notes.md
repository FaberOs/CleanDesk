# CleanDesk v1.0.0 — Official Windows Release 🧹⚡

CleanDesk es un widget de escritorio nativo, minimalista y ultra ligero para **Windows 11 y 10**, diseñado específicamente como la herramienta definitiva de productividad para desarrolladores de software, ingenieros y power users.

### 🌟 Novedades y Características Principales

* ⚡ **KillerPort (Gestión y Liberación de Puertos Dev):**
  * Auditoría en tiempo real de sockets TCP en escucha (`LISTEN`) con latencia **< 5 ms** usando la Win32 API.
  * Identificación inmediata de PID y nombre del proceso que bloquea puertos comunes (`3000-3006`, `4200`, `5000`, `5173`, `8000`, `8080`, `9000`).
  * Botón de **1-clic** para liberar cualquier puerto individualmente o el botón **"Kill Selected Ports"** para liberar todos a la vez.
  * Buscador y monitor dinámico de puertos personalizados.

* 🛡️ **ProcessGuardian (Guardián de Tareas & RAM):**
  * Supervisión continua de procesos dev típicos (`node.exe`, `pwsh.exe`, `cmd.exe`, `python.exe`, `git.exe`).
  * **Alerta Inteligente de Bucles:** Detección automática si hay 4 o más instancias simultáneas o consumo > 350 MB de RAM.
  * Exterminio de raíz del árbol de procesos padre e hijos (`taskkill /T /F`) y vaciado de memoria RAM con `EmptyWorkingSet`.

* 🧹 **Limpieza del Sistema y Caches Dev:**
  * Purga bajo demanda de archivos `%TEMP%`, cachés de desarrollo (`npm-cache`, `pip`), shaders DirectX y volcados de memoria.
  * Vaciado nativo de la Papelera de Reciclaje mediante la API de Windows Shell.

* 🪟 **Diseño Fluent 2 y Experiencia de Widget:**
  * Reside discretamente en los iconos ocultos de la bandeja del sistema (*Hidden Icons*) sin ensuciar la barra de tareas.
  * **Modo Compacto:** Píldora minimalista de escritorio con **micro-carrusel táctil** controlable con la rueda del ratón (`MouseWheel`) para acciones rápidas en 1-clic.
  * Menú contextual oscuro en la bandeja del sistema con estilo Windows 11 Acrylic.
  * Soporte nativo para modo oscuro y claro según el tema de Windows.
  * Detección multi-idioma automática (Español / Inglés).

---

### 📦 Instalación

Descarga el instalador adjunto **`CleanDesk-v1.0-Setup.exe`**.

El asistente de instalación te permite personalizar:
- [x] Crear acceso directo en el Escritorio
- [x] Crear acceso directo en el Menú de Inicio
- [x] Iniciar CleanDesk automáticamente al arrancar Windows (Startup App - Enabled)
- [x] Iniciar CleanDesk al completar la instalación
