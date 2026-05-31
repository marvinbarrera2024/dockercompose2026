using Npgsql;
using StackExchange.Redis;

Console.WriteLine("Iniciando el Worker de Procesamiento Seguro (.NET 10)...");

// Configuración de conexiones usando variables de entorno
string redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "redis";
string dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "db";
string connectionString = $"Host={dbHost};Username=postgres;Password=supersecretpassword;Database=votaciones;";

// Control del ciclo de vida de la aplicación para un apagado seguro (Graceful Shutdown)
using CancellationTokenSource cts = new();
Console.CancelKeyPress += (sender, eventArgs) =>
{
    Console.WriteLine("Apagando el worker de forma segura...");
    cts.Cancel();
    eventArgs.Cancel = true; // Evita que el proceso muera bruscamente de inmediato
};

// 1. Conectar a Redis de forma asíncrona
using ConnectionMultiplexer redis = await ConnectionMultiplexer.ConnectAsync($"{redisHost}:6379,password=esfe2026");
IDatabase rDb = redis.GetDatabase();

// 2. Inicializar Base de Datos de manera segura (con límite de reintentos)
await InicializarBaseDeDatosAsync(connectionString, cts.Token);

Console.WriteLine("Worker conectado y listo para procesar votos eficientemente.");

// 3. Bucle de escucha reactivo (Sin Thread.Sleep)
while (!cts.Token.IsCancellationRequested)
{
    try
    {
        // Bloquea el hilo de forma asíncrona hasta por 5 segundos esperando un voto.
        // Si no hay votos, retorna null y repite el ciclo sin consumir CPU.
        string? voto = await rDb.ListRightPopAsync("votos");

        if (voto != null)
        {
            Console.WriteLine($"Voto detectado para: {voto}. Guardando en PostgreSQL...");
            await GuardarVotoEnDBAsync(connectionString, voto);
        }
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        Console.WriteLine($"Error en el ciclo de procesamiento: {ex.Message}");
        // Pequeña espera en caso de error de red con Redis para no saturar los logs
        await Task.Delay(2000, cts.Token);
    }
}

Console.WriteLine("Worker detenido limpiamente. No quedan tareas pendientes.");

// ============================================================================
// Funciones locales asíncronas (Modernas, seguras y eficientes)
// ============================================================================

async Task InicializarBaseDeDatosAsync(string connString, CancellationToken cancellationToken)
{
    int intentos = 0;
    const int maxIntentos = 10;

    while (!cancellationToken.IsCancellationRequested)
    {
        try
        {
            intentos++;
            using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync(cancellationToken);
            
            using var cmd = new NpgsqlCommand(
                "CREATE TABLE IF NOT EXISTS votos (id SERIAL PRIMARY KEY, opcion VARCHAR(50), fecha TIMESTAMP DEFAULT CURRENT_TIMESTAMP);", conn);
            
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            break; // Éxito, salimos del bucle
        }
        catch
        {
            if (intentos >= maxIntentos)
            {
                Console.WriteLine("No se pudo conectar a la base de datos tras múltiples intentos. Abortando...");
                throw;
            }
            Console.WriteLine($"[Intento {intentos}/{maxIntentos}] Esperando a que PostgreSQL esté listo...");
            await Task.Delay(3000, cancellationToken);
        }
    }
}

async Task GuardarVotoEnDBAsync(string connString, string opcion)
{
    try
    {
        using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        
        using var cmd = new NpgsqlCommand("INSERT INTO votos (opcion) VALUES (@opcion);", conn);
        cmd.Parameters.AddWithValue("opcion", opcion);
        
        await cmd.ExecuteNonQueryAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error crítico al guardar en DB: {ex.Message}");
    }
}