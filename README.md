# CleanDesk 🧹⚡

[![Release](https://img.shields.io/github/v/release/FaberOs/CleanDesk?color=3B82F6&label=Version)](https://github.com/FaberOs/CleanDesk/releases/latest)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D4?logo=windows)](https://github.com/FaberOs/CleanDesk/releases/latest)
[![Architecture](https://img.shields.io/badge/Arch-x64-64748B)](https://github.com/FaberOs/CleanDesk/releases/latest)
[![License](https://img.shields.io/badge/License-MIT-10B981)](LICENSE)

**CleanDesk** es un widget de escritorio nativo, minimalista y ultra ligero para Windows 11 y 10, diseñado específicamente como la herramienta definitiva de productividad para desarrolladores de software, ingenieros y power users.

Combina en una única interfaz flotante Fluent 2 la solución a las tres fricciones más frustrantes del día a día en desarrollo: **puertos locales bloqueados**, **procesos zombis devoradores de RAM** y **acumulación de cachés y basura del sistema**.

---

## 🎯 Por qué CleanDesk: Problemas y Casos de Uso que Resuelve

### 1. ⚡ KillerPort — Exterminio Inmediato de Puertos Dev Bloqueados
* **El dolor habitual:**  
  Estás desarrollando con Node.js, Vite, Next.js, Express, React, FastAPI, Flask, ASP.NET o contenedores Docker. Cierras tu terminal o el proceso crashea, y al intentar reiniciar el servidor te topas con el temido:  
  `Error: listen EADDRINUSE: address already in use :::3000`  
  Para resolverlo tenías que interrumpir tu flujo de trabajo, abrir la consola, recordar comandos arcanos como `netstat -ano | findstr :3000` y luego ejecutar `taskkill /PID <pid> /F`.
* **La solución CleanDesk:**  
  * Inspecciona la tabla de sockets TCP en tiempo real con latencia **< 5 ms** usando la API nativa de Win32 (`GetExtendedTcpTable`).
  * Identifica al instante el PID y el nombre del ejecutable que retiene el puerto.
  * Botón de **1-clic** para liberar cualquier puerto individualmente o el botón **"Kill Selected Ports"** para liberar todos los puertos de prueba ocupados simultáneamente.
  * Incluye un buscador dinámico para agregar cualquier puerto personalizado (ej. `4000`, `9000`, etc.).

---

### 2. 🛡️ ProcessGuardian — Guardián de Tareas Dev & Exterminador de Bucles de RAM
* **El dolor habitual:**  
  Al trabajar con agentes autónomos de código por IA (Claude Code, Antigravity, Cursor, Copilot CLI), pruebas automatizadas o scripts pesados, es muy frecuente que se generen árboles enteros de subprocesos huérfanos (`node.exe`, `pwsh.exe`, `cmd.exe`, `python.exe`, `git.exe`) que quedan corriendo en segundo plano consumiendo silenciosamente gigabytes de memoria RAM y ciclos de CPU.
* **La solución CleanDesk:**  
  * Agrupa y supervisa continuamente los procesos típicos de entornos dev.
  * **Alerta Inteligente de Bucles:** Si detecta 4 o más instancias huérfanas o un consumo acumulado superior a 350 MB, despliega una alerta visual destacada (*Runaway Loop Alert*).
  * Extermina de raíz el árbol completo de procesos padre e hijos con un solo clic (`taskkill /T /F`).
  * Ejecuta de inmediato `EmptyWorkingSet` para devolverle al sistema operativo toda la memoria RAM retenida.

---

### 3. 🧹 Limpiador Integral del Sistema y Cachés Dev
* **El dolor habitual:**  
  Cachés de paquetes (`npm-cache`, `pip cache`), volcados de memoria por errores (`MEMORY.DMP`, `CrashDumps`), shaders residuales de DirectX y miles de archivos en `%TEMP%` saturan el disco de tu máquina y degradan el rendimiento de los discos SSD.
* **La solución CleanDesk:**  
  * Auditoría segura y desglosada por categorías bajo demanda (sin molestos escaneos automáticos en segundo plano al iniciar).
  * Vaciado nativo de la Papelera de Reciclaje mediante la API de Windows Shell sin confirmaciones repetitivas.
  * Filtrado inteligente: nunca toca archivos en uso, perfiles de usuario ni navegadores abiertos.

---

### 4. 🪟 Experiencia Auténtica de Widget (Windows 11 Fluent 2)
* **El dolor habitual:**  
  La gran mayoría de optimizadores de sistema son aplicaciones intrusivas, pesadas, plagadas de publicidad y ocupan espacio valioso en la barra de tareas.
* **La solución CleanDesk:**  
  * **Sin barra de tareas:** Reside discretamente en los iconos ocultos de la bandeja del sistema (*System Tray / Hidden Icons*).
  * **Modo Compacto:** Se contrae en una píldora minimalista de escritorio con un **micro-carrusel táctil** controlable con la rueda del ratón (`MouseWheel`) para consultar métricas y ejecutar limpiezas con 1 solo clic.
  * **Diseño Fluent 2:** Esquinas redondeadas, desenfoque acrílico, transiciones a 60 FPS, tipografía Segoe UI Variable y soporte para Modo Oscuro y Modo Claro según la configuración de tu sistema.
  * **Menú Contextual Avanzado:** Clic derecho sobre el icono en la bandeja para mostrar/ocultar, cambiar de modo, escanear o liberar puertos directamente.

---

## 📦 Descarga e Instalación

Para utilizar CleanDesk no necesitas compilar código ni configurar dependencias:

1. Ve a la sección de **[Releases Oficiales](https://github.com/FaberOs/CleanDesk/releases/latest)**.
2. Descarga el instalador oficial de Windows: **`CleanDesk-v1.0-Setup.exe`**.
3. Ejecuta el asistente de instalación.

El instalador te permite personalizar la configuración a tu medida:
- [x] **Acceso directo en el Escritorio**
- [x] **Acceso directo en el Menú de Inicio (Start Menu)**
- [x] **Arranque automático con Windows (Startup App - Enabled)**
- [x] **Iniciar CleanDesk de inmediato al terminar**

> [!TIP]
> Puedes desinstalar CleanDesk en cualquier momento desde **Configuración de Windows > Aplicaciones > Aplicaciones instaladas** o desde el Panel de Control.

---

## 🌐 Idiomas Soportados

CleanDesk detecta de forma nativa el idioma de tu sistema operativo:
* 🇪🇸 **Español**
* 🇺🇸 **Inglés**

---

## 👤 Autor

Desarrollado con dedicación por **[FaberOs](https://github.com/FaberOs)**.  
Licencia MIT.
