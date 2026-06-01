const express = require('express');
const { Pool } = require('pg');
const redis = require('redis');

const app = express();
const port = 3000;

// 1. Configuración de PostgreSQL
const pool = new Pool({
  host: process.env.DB_HOST || 'db',
  user: 'postgres',
  password: 'supersecretpassword',
  database: 'votaciones', // Ajustar al nombre real de la BD ("postgres" según tu docker-compose, cámbialo si es necesario)
  port: 5432,
  connectionTimeoutMillis: 5000 // Evita que se quede colgado esperando infinitamente
});

//  SOLUCIÓN CRÍTICA: Capturar errores globales del Pool para que el contenedor NO muera
pool.on('error', (err) => {
  console.error(' Error inesperado en el Pool de PostgreSQL (BD posiblemente caída):', err.message);
});

// 2. Configuración de Redis con la contraseña segura
const redisClient = redis.createClient({
  url: `redis://:esfe2026@${process.env.REDIS_HOST || 'redis'}:6379`
});
redisClient.connect().catch(console.error);

app.get('/resultados', async (req, res) => {
  try {
    // Intentamos hacer la consulta a PostgreSQL
    const queryResult = await pool.query("SELECT opcion, COUNT(*) as total FROM votos GROUP BY opcion");

    let conteo = { "C#": 0, "Java": 0, "Python": 0, "JavaScript": 0 };
    queryResult.rows.forEach(row => {
      if (conteo[row.opcion] !== undefined) {
        conteo[row.opcion] = parseInt(row.total);
      }
    });

    // Renderizar HTML Normal con datos
    res.send(renderizarDashboard(conteo, false));

  } catch (err) {
    //  Si la base de datos se cae, capturamos el error aquí
    console.error("⚠️ Error al consultar la BD. Mostrando modo de degradación amigable...");
    
    // Mandamos un conteo en ceros pero avisando en la interfaz que la BD está caída
    let conteoVacio = { "C#": 0, "Java": 0, "Python": 0, "JavaScript": 0 };
    res.status(200).send(renderizarDashboard(conteoVacio, true));
  }
});

// Ruta encargada de hacer la limpieza total
app.post('/resultados/reiniciar', async (req, res) => {
  try {
    console.log("Iniciando reinicio del sistema solicitado desde el Dashboard...");

    // A. Vaciar la tabla en PostgreSQL
    await pool.query("TRUNCATE TABLE votos;");

    // B. Eliminar la cola en Redis
    await redisClient.del("votos");

    console.log("¡Ecosistema reiniciado exitosamente!");
    res.redirect('/resultados');
  } catch (err) {
    console.error("Error durante el reinicio:", err);
    res.status(500).send("Error al intentar reiniciar los contadores (¿Está la BD caída?).");
  }
});

//  Función auxiliar para modularizar el HTML y meter alertas dinámicas
function renderizarDashboard(conteo, baseDeDatosCaida) {
  // Si la BD está caída, agregamos un banner de advertencia visual llamativo
  const bannerAlerta = baseDeDatosCaida 
    ? `<div style="background-color: #ffcccc; color: #cc0000; padding: 15px; border: 2px solid #cc0000; border-radius: 5px; margin: 20px auto; max-width: 600px; font-weight: bold;">
          Conexión perdida con la Base de Datos. Los resultados no se están actualizando en este momento.
       </div>`
    : '';

  // Deshabilitar botón de reinicio si la BD no responde
  const botonAtributos = baseDeDatosCaida ? 'disabled style="opacity: 0.5; cursor: not-allowed;"' : '';

  return `
    <!DOCTYPE html>
    <html>
    <head>
        <title>Resultados en Vivo</title>
        <meta http-equiv="refresh" content="3">
        <style>
            body { font-family: Arial, sans-serif; text-align: center; margin-top: 50px; background-color: #fafafa; }
            .box { display: inline-block; width: 200px; margin: 20px; padding: 20px; border-radius: 10px; color: white; font-size: 24px; }
            .c-box { background-color: #007acc; }
            .java-box { background-color: #e41f23; }
            .python-box { background-color: #19e04b; }
            .javascript-box { background-color: #c0f016; }
            .btn-danger { background-color: #dc3545; color: white; font-size: 16px; padding: 10px 20px; border: none; border-radius: 5px; cursor: pointer; margin-top: 30px; }
            .btn-danger:hover { background-color: #bd2130; }
        </style>
    </head>
    <body>
        <h1>Resultados de la Votación de Lenguajes</h1>
        
        ${bannerAlerta}

        <div class="box c-box">
            <h3>C# (.NET)</h3>
            <p>${conteo["C#"]} votos</p>
        </div>
        <div class="box java-box">
            <h3>Java</h3>
            <p>${conteo["Java"]} votos</p>
        </div>
        <div class="box python-box">
            <h3>Python</h3>
            <p>${conteo["Python"]} votos</p>
        </div>
        <div class="box javascript-box">
            <h3>JavaScript</h3>
            <p>${conteo["JavaScript"]} votos</p>
        </div>
        <p><i>Esta página se actualiza automáticamente cada 3 segundos.</i></p>
        
        <form action="/resultados/reiniciar" method="POST" onsubmit="return confirm('¿Estás seguro de que deseas reiniciar todos los votos a cero?');">
            <button type="submit" class="btn-danger" ${botonAtributos}>Reiniciar Conteo de Votos</button>
        </form>
    </body>
    </html>
  `;
}

app.listen(port, () => {
  console.log(`Dashboard corriendo en http://localhost:${port}`);
});