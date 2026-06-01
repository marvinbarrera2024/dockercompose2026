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

// CONFIGURACIÓN DE SEGURIDAD 
// Llamamos aquí a la recuperación para vaciar "votos:procesando" antes de aceptar nuevos votos
await RecuperarVotosPendientesAsync();

Console.WriteLine("Worker conectado y listo para procesar votos eficientemente.");

// 3. Bucle de escucha reactivo (Sin Thread.Sleep)
while (!cts.Token.IsCancellationRequested)
{
   string? voto = null;
    try
    {
        // Mueve de "votos" a "votos:procesando" de forma atómica
        voto = await rDb.ListMoveAsync("votos", "votos:procesando", ListSide.Right, ListSide.Left);

        if (voto != null)
        {
            Console.WriteLine($"Voto detectado para: {voto}. Guardando en PostgreSQL...");
                        
            // Intentar guardar en la base de datos
            await GuardarVotoEnDBAsync(connectionString, voto);

            // Si tuvo éxito, lo borramos de la cola temporal
            await rDb.ListRemoveAsync("votos:procesando", voto, 1);
        }
        else
        {
            // Cola vacía: Esperamos un segundo para no saturar a Redis
            await Task.Delay(1000, cts.Token);
        }
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        Console.WriteLine($"Error en el ciclo principal: {ex.Message}");
        Console.WriteLine($"El voto de '{voto}' quedó retenido seguro en 'votos:procesando'.");
                    
        // Esperar 3 segundos antes de reintentar (le da tiempo a la BD de revivir)
        await Task.Delay(3000, cts.Token);
    }
}

Console.WriteLine("Worker detenido limpiamente. No quedan tareas pendientes.");

// ============================================================================
// Funciones locales asíncronas (Modernas, seguras y eficientes)
// ============================================================================
async Task RecuperarVotosPendientesAsync()
{
    Console.WriteLine("Verificando si quedaron votos pendientes en la cola de contingencia...");

    try
    {
        // Revisamos si hay elementos atascados en "votos:procesando"
        long pendientes = await rDb.ListLengthAsync("votos:procesando");

        while (pendientes > 0)
        {
            Console.WriteLine($"Se encontraron {pendientes} votos retenidos de una caída anterior. Recuperando...");

            // Obtenemos el voto sin borrarlo aún
            string? votoPendiente = await rDb.ListGetByIndexAsync("votos:procesando", -1);

            if (votoPendiente != null)
            {
                // Intentamos guardarlo en la base de datos que ya debería estar activa
                await GuardarVotoEnDBAsync(connectionString, votoPendiente);

                // Si se guardó con éxito, lo eliminamos de la cola de contingencia
                await rDb.ListRemoveAsync("votos:procesando", votoPendiente, 1);
                Console.WriteLine($"Voto de '{votoPendiente}' recuperado e insertado con éxito.");
            }

            // Actualizamos el contador del bucle
            pendientes = await rDb.ListLengthAsync("votos:procesando");
        }

        Console.WriteLine("No quedan votos pendientes en la cola de contingencia.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"No se pudieron recuperar los votos en el arranque (¿BD sigue caída?): {ex.Message}");
    }
}

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
    //  NOTA DE CONTROL: Quitamos el try/catch interno para que, si PostgreSQL está caído, 
    // la excepción suba y sea capturada por el ciclo principal o por el recuperador. 
    // Si la atrapas aquí adentro y solo imprimes un log, tus funciones de arriba creerán 
    // que el voto se guardó exitosamente y terminarán borrándolo de Redis.
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    
    using var cmd = new NpgsqlCommand("INSERT INTO votos (opcion) VALUES (@opcion);", conn);
    cmd.Parameters.AddWithValue("opcion", opcion);
    
    await cmd.ExecuteNonQueryAsync();
}