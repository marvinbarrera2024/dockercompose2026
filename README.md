
# 🗳️ Sistema de Votación Distribuido y Resiliente

Este proyecto implementa un sistema de votación en tiempo real diseñado bajo una arquitectura de **microservicios con balanceo de carga**, altamente escalable y tolerante a fallos, utilizando tecnologías modernas como **.NET 10**, **Python (Flask)**, **Node.js (Express)**, **Redis**, **PostgreSQL** y **Nginx**.

---

## 🏗️ Arquitectura del Sistema

El ecosistema está compuesto por 6 servicios autónomos que operan bajo un modelo de alta disponibilidad:

1. **`balanceador` (Nginx):** El punto de entrada unificado. Actúa como **Proxy Inverso**, recibiendo el tráfico externo y distribuyéndolo equitativamente entre las réplicas disponibles de la aplicación de votación.
2. **`vote-app` (Python/Flask):** Frontend interactivo. Se despliega mediante múltiples réplicas escalables para absorber el tráfico. Registra los votos en `Redis` para asegurar una latencia mínima.
3. **`redis` (Broker de Mensajes):** Base de datos NoSQL de alta velocidad que actúa como una cola de mensajes (*Queue*), protegiendo al almacenamiento persistente de picos de tráfico.
4. **`worker` (.NET 10):** Servicio asíncrono en segundo plano que consume los votos desde `Redis` y los transfiere de forma segura y ordenada a la base de datos.
5. **`db` (PostgreSQL):** Almacenamiento definitivo, estructurado y persistente de los votos procesados.
6. **`result-app` (Node.js/Express):** Dashboard administrativo que consulta las métricas en tiempo real almacenadas en `PostgreSQL` para desplegar los resultados.

## 🛠️ Requisitos Previos

Para ejecutar este proyecto, solo necesitas disponer de una de las siguientes opciones:
* **Entorno Cloud:** GitHub Codespaces (Entorno de desarrollo remoto preconfigurado).
* **Entorno Local:** [Docker Desktop](https://www.docker.com/products/docker-desktop/) instalado y en ejecución en tu sistema operativo.

---

## 🚀 Guía de Comandos Rápidos (Docker Compose)

Usa esta guía para administrar el ecosistema completo desde la terminal. Asegúrate de estar posicionado en la raíz del proyecto antes de ejecutar cualquier comando.

### 1. Gestión Global del Ecosistema

* **Iniciar todos los contenedores por primera vez (o aplicando cambios):**
  ```bash
  docker-compose up --build
  ```
 * **Iniciar todos los contenedores en segundo plano (Liberar la terminal):**
   ```bash
   docker-compose up -d
   ```
* **Detener todos los contenedores:**
  ```bash
  docker-compose down
  ```

* **Destruir el entorno y reiniciar desde cero:**
  ```bash
  docker-compose down -v
  ```

* **Gestión de contenedor específico (Reconstruir y aplicar cambios de código fuente):**
  ```bash
  docker-compose up -d --build <nombre_servicio>
  ```

* **Detener un solo contenedor:**
  ```bash
  docker-compose stop <nombre_servicio>
  ```

* **Iniciar contenedor que estaba detenido:**
  ```bash
  docker-compose start <nombre_servicio>
  ```

* **Reinicio de un contenedor rápido sin aplicar cambios:**
  ```bash
  docker-compose restart <nombre_servicio>
  ```

* **Ver logs en tiempo real:**
  ```bash
  docker-compose logs -f <nombre_servicio>
  ```

  ## 📦 Despliegue con un solo contenedor (Sin réplicas)

  Si quieres usar el balanceador Nginx pero **sin activar múltiples réplicas** para ahorrar memoria en tu computadora, debes indicarle a Docker que levante exactamente un (`1`) contenedor de la app de votación.

  ```bash
  docker-compose up -d --scale <nombre_servicio>=1
   ```

  ###  Ver contenedores del proyecto en ejecución
  Muestra el estado actual (Up/Down), los puertos asignados y cuántas réplicas están corriendo exactamente en este ecosistema.
  ```bash
  docker-compose ps
   ```
   ### Ver imágenes vinculadas al proyecto
   ```bash
   docker-compose images
   ```
   ### Apagar contenedores y borrar volúmenes
   ```bash
   docker-compose down --volumes
   ```