
# 🗳️ Sistema de Votación Distribuido y Resiliente (Microservicios)

Este proyecto consiste en un sistema de votación en tiempo real diseñado bajo una arquitectura de microservicios altamente escalable, desacoplada y tolerante a fallos, utilizando tecnologías modernas como **.NET 10**, **Python (Flask)**, **Node.js (Express)**, **Redis** y **PostgreSQL**.

---

## 🏗️ Arquitectura del Sistema

El ecosistema está dividido en 5 componentes autónomos que se comunican de forma asíncrona mediante el patrón **Queue-based Load Leveling (Nivelación de carga basada en colas)**:

1. **`vote-app` (Python/Flask):** Frontend interactivo donde los usuarios emiten sus votos. Los registra instantáneamente en memoria RAM mediante Redis para ofrecer una latencia mínima.
2. **`redis` (Broker de Mensajes):** Base de datos NoSQL que actúa como una fila de mensajes (*Queue*) de alta velocidad, absorbiendo los picos de tráfico extremo y protegiendo al almacenamiento persistente.
3. **`worker` (.NET 10):** Servicio de procesamiento en segundo plano que consume de forma asíncrona y reactiva los votos de Redis para transferirlos ordenadamente a la base de datos relacional. Cuenta con políticas de reintento avanzadas y *Graceful Shutdown* (apagado controlado) para evitar la pérdida de datos.
4. **`db` (PostgreSQL):** Almacenamiento definitivo, estructurado y persistente de los votos procesados.
5. **`result-app` (Node.js/Express):** Dashboard administrativo en tiempo real que consulta las métricas de PostgreSQL para desplegar los resultados gráficos de la votación y gestionar las funciones de mantenimiento del sistema.

---

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