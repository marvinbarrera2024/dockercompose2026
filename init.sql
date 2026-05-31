-- init.sql

-- 1. Crear la base de datos si no existe (PostgreSQL no soporta IF NOT EXISTS para DATABASE directamente en un script simple, por lo que usamos este bloque de código)
SELECT 'CREATE DATABASE votaciones'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'votaciones')\gexec

-- 2. Conectarse a la base de datos que se acaba de verificar/crear
\c votaciones;

-- 3. Crear la tabla si no existe
CREATE TABLE IF NOT EXISTS votos (
    id SERIAL PRIMARY KEY,
    opcion VARCHAR(50) NOT NULL,
    fecha TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);