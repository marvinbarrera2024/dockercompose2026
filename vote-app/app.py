from flask import Flask, render_template_string, request, redirect
import redis
import os

app = Flask(__name__)

# Conexión a Redis usando el nombre del servicio en Docker Compose
redis_host = os.getenv("REDIS_HOST", "redis")
r = redis.Redis(host=redis_host, port=6379, decode_responses=True,password="esfe2026")

HTML_TEMPLATE = """
<!DOCTYPE html>
<html>
<head>
    <title>Demo de Votación</title>
    <style>
        body { font-family: Arial, sans-serif; text-align: center; margin-top: 50px; background-color: #f4f4f9; }
        button { font-size: 20px; padding: 15px 30px; margin: 20px; cursor: pointer; border: none; border-radius: 5px; color: white; }
        .btn-c { background-color: #007acc; }
        .btn-java { background-color: #e41f23; }
    </style>
</head>
<body>
    <h1>¿Cuál es tu backend favorito para esta demo?</h1>
    <form action="/votar" method="POST">
        <button type="submit" name="voto" value="C#" class="btn-c">C# (.NET)</button>
        <button type="submit" name="voto" value="Java" class="btn-java">Java</button>
    </form>
    <p>¡Tu voto se enviará a una cola de Redis en tiempo real!</p>
</body>
</html>
"""

@app.route('/')
def index():
    return render_template_string(HTML_TEMPLATE)

@app.route('/votar', methods=['POST'])
def votar():
    voto = request.form.get('voto')
    if voto:
        # Encolar el voto en Redis (LPUSH añade a la lista)
        r.lpush("votos", voto)
    return redirect('/')

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)