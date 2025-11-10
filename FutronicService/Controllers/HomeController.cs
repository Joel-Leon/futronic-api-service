using Microsoft.AspNetCore.Mvc;
using System;

namespace FutronicService.Controllers
{
    [ApiController]
    public class HomeController : ControllerBase
    {
        [HttpGet("/")]
        public ContentResult Index()
        {
   var html = @"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Futronic API - Servicio de Huellas Dactilares</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        min-height: 100vh;
    padding: 20px;
        }
        .container {
    max-width: 1200px;
     margin: 0 auto;
        }
      header {
   background: white;
            padding: 30px;
        border-radius: 10px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.1);
            margin-bottom: 30px;
        }
        h1 {
      color: #667eea;
   font-size: 2.5em;
            margin-bottom: 10px;
        }
        .subtitle {
            color: #666;
            font-size: 1.2em;
        }
.status {
            display: inline-block;
   padding: 8px 16px;
border-radius: 20px;
            font-weight: bold;
    margin-top: 15px;
        }
        .status.online {
            background: #4caf50;
         color: white;
        }
        .endpoints {
         display: grid;
            grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
   gap: 20px;
    margin-bottom: 30px;
        }
        .endpoint-card {
       background: white;
 padding: 25px;
            border-radius: 10px;
    box-shadow: 0 5px 20px rgba(0,0,0,0.1);
          transition: transform 0.2s;
        }
    .endpoint-card:hover {
    transform: translateY(-5px);
  box-shadow: 0 10px 30px rgba(0,0,0,0.15);
        }
    .method {
       display: inline-block;
     padding: 5px 12px;
            border-radius: 5px;
      font-weight: bold;
         font-size: 0.85em;
 margin-right: 10px;
 }
    .method.get { background: #61affe; color: white; }
        .method.post { background: #49cc90; color: white; }
     .endpoint-path {
   font-family: 'Courier New', monospace;
   color: #333;
     font-size: 1.1em;
   margin: 10px 0;
        }
        .endpoint-desc {
            color: #666;
  line-height: 1.6;
        }
        .star {
            color: #ffd700;
    margin-left: 5px;
     }
   .footer {
  background: white;
         padding: 20px;
       border-radius: 10px;
            text-align: center;
  box-shadow: 0 5px 20px rgba(0,0,0,0.1);
        }
 .footer a {
            color: #667eea;
      text-decoration: none;
   font-weight: bold;
   }
        .footer a:hover {
            text-decoration: underline;
    }
        .quick-links {
            background: white;
 padding: 20px;
            border-radius: 10px;
  box-shadow: 0 5px 20px rgba(0,0,0,0.1);
            margin-bottom: 30px;
        }
        .quick-links h2 {
   color: #667eea;
    margin-bottom: 15px;
        }
    .quick-links a {
            display: inline-block;
            margin: 5px 10px 5px 0;
  padding: 10px 20px;
  background: #667eea;
      color: white;
            text-decoration: none;
        border-radius: 5px;
     transition: background 0.2s;
        }
        .quick-links a:hover {
      background: #764ba2;
      }
    </style>
</head>
<body>
  <div class='container'>
  <header>
  <h1>?? Futronic API</h1>
      <p class='subtitle'>Servicio REST de Huellas Dactilares - Futronic FS88</p>
          <span class='status online' id='status'>? En línea</span>
        </header>

     <div class='quick-links'>
            <h2>Enlaces Rápidos</h2>
          <a href='/health' target='_blank'>Health Check</a>
            <a href='/api/fingerprint/config' target='_blank'>Configuración</a>
            <a href='https://github.com/JoelLeonUNS/futronic-cli' target='_blank'>?? Documentación</a>
      </div>

        <div class='endpoints'>
   <!-- Health & Config -->
            <div class='endpoint-card'>
       <span class='method get'>GET</span>
                <div class='endpoint-path'>/health</div>
       <p class='endpoint-desc'>Estado del servicio y dispositivo Futronic</p>
            </div>

            <div class='endpoint-card'>
          <span class='method get'>GET</span>
                <div class='endpoint-path'>/api/fingerprint/config</div>
   <p class='endpoint-desc'>Obtener configuración actual (threshold, timeout)</p>
            </div>

    <div class='endpoint-card'>
  <span class='method post'>POST</span>
           <div class='endpoint-path'>/api/fingerprint/config</div>
   <p class='endpoint-desc'>Actualizar configuración del servicio</p>
            </div>

            <!-- Capture -->
        <div class='endpoint-card'>
<span class='method post'>POST</span>
     <div class='endpoint-path'>/api/fingerprint/capture</div>
        <p class='endpoint-desc'>Captura temporal de huella dactilar</p>
    </div>

            <!-- Register -->
            <div class='endpoint-card'>
           <span class='method post'>POST</span>
         <div class='endpoint-path'>/api/fingerprint/register</div>
     <p class='endpoint-desc'>Registrar huella (1 muestra)</p>
    </div>

          <div class='endpoint-card'>
    <span class='method post'>POST</span>
     <div class='endpoint-path'>/api/fingerprint/register-multi</div>
     <p class='endpoint-desc'>Registrar huella con múltiples muestras (1-5) <span class='star'>?</span></p>
            </div>

          <!-- Verify -->
     <div class='endpoint-card'>
     <span class='method post'>POST</span>
        <div class='endpoint-path'>/api/fingerprint/verify</div>
        <p class='endpoint-desc'>Verificar huella (comparar archivos)</p>
      </div>

            <div class='endpoint-card'>
     <span class='method post'>POST</span>
     <div class='endpoint-path'>/api/fingerprint/verify-simple</div>
           <p class='endpoint-desc'>Verificación 1:1 con captura en vivo <span class='star'>?</span></p>
   </div>

       <!-- Identify -->
  <div class='endpoint-card'>
          <span class='method post'>POST</span>
            <div class='endpoint-path'>/api/fingerprint/identify</div>
              <p class='endpoint-desc'>Identificar huella (1:N con archivos)</p>
   </div>

    <div class='endpoint-card'>
     <span class='method post'>POST</span>
    <div class='endpoint-path'>/api/fingerprint/identify-live</div>
         <p class='endpoint-desc'>Identificación 1:N con captura en vivo <span class='star'>?</span></p>
            </div>
  </div>

        <div class='footer'>
            <p>
      <strong>Futronic API v1.0</strong> | 
       .NET Framework 4.8 | 
     SDK Futronic 4.2.0
 </p>
 <p style='margin-top: 10px;'>
      <a href='https://github.com/JoelLeonUNS/futronic-cli' target='_blank'>GitHub</a> | 
    <a href='/health'>Health Check</a> | 
                <a href='/api/fingerprint/config'>Config</a>
            </p>
        </div>
    </div>

    <script>
        // Verificar estado del servicio
        fetch('/health')
         .then(r => r.json())
   .then(data => {
     const status = document.getElementById('status');
    if (data.success && data.data.deviceConnected) {
             status.textContent = '? Servicio operativo - Dispositivo conectado';
      status.className = 'status online';
} else if (data.success) {
      status.textContent = '?? Servicio operativo - Dispositivo NO conectado';
        status.className = 'status online';
        status.style.background = '#ff9800';
} else {
       status.textContent = '? Servicio con errores';
     status.className = 'status online';
        status.style.background = '#f44336';
           }
            })
            .catch(() => {
            const status = document.getElementById('status');
  status.textContent = '? Error al conectar';
        status.className = 'status online';
    status.style.background = '#f44336';
  });
    </script>
</body>
</html>";

      return Content(html, "text/html");
        }
    }
}
