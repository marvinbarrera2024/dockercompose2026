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
  database: 'votaciones',
  port: 5432,
});

// 2. Configuración de Redis con la contraseña segura
const redisClient = redis.createClient({
  url: `redis://:esfe2026@${process.env.REDIS_HOST || 'redis'}:6379`
});
redisClient.connect().catch(console.error);

app.get('/', async (req, res) => {
  try {
    const queryResult = await pool.query("SELECT opcion, COUNT(*) as total FROM votos GROUP BY opcion");

    let conteo = { "C#": 0, "Java": 0 };
    queryResult.rows.forEach(row => {
      conteo[row.opcion] = parseInt(row.total);
    });

    // Renderizar HTML con el nuevo botón de reinicio
    res.send(`
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
              .btn-danger { background-color: #dc3545; color: white; font-size: 16px; padding: 10px 20px; border: none; border-radius: 5px; cursor: pointer; margin-top: 30px; }
              .btn-danger:hover { background-color: #bd2130; }
          </style>
      </head>
      <body>
          <h1>Resultados de la Votación en Tiempo Real</h1>
          <div class="box c-box">
              <h3>C# (.NET)</h3>
              <p>${conteo["C#"]} votos</p>
          </div>
          <div class="box java-box">
              <h3>Java</h3>
              <p>${conteo["Java"]} votos</p>
          </div>
          <p><i>Esta página se actualiza automáticamente cada 3 segundos.</i></p>
          
          <form action="/reiniciar" method="POST" onsubmit="return confirm('¿Estás seguro de que deseas reiniciar todos los votos a cero?');">
              <button type="submit" class="btn-danger">Reiniciar Conteo de Votos</button>
          </form>
      </body>
      </html>
    `);
  } catch (err) {
    console.error(err);
    res.status(500).send("Error en la base de datos.");
  }
});

// Ruta encargada de hacer la limpieza total
app.post('/reiniciar', async (req, res) => {
  try {
    Console.log("Iniciando reinicio del sistema solicitado desde el Dashboard...");

    // A. Vaciar la tabla en PostgreSQL
    await pool.query("TRUNCATE TABLE votos;");

    // B. Eliminar la cola en Redis
    await redisClient.del("votos");

    Console.log("¡Ecosistema reiniciado exitosamente!");
    res.redirect('/');
  } catch (err) {
    console.error("Error durante el reinicio:", err);
    res.status(500).send("Error al intentar reiniciar los contadores.");
  }
});

app.listen(port, () => {
  console.log(`Dashboard corriendo en http://localhost:${port}`);
});