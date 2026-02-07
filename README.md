# Atlas FUEGO Admin Panel

Panel de administración para gestionar los registros del evento **Cóctel de Agentes FUEGO**.

## Waykee Documentation

```
Sistema 110264: Atlas FUEGO
└── Módulo 110269: AtlasFuegoAdmin (Panel de Administración)
    ├── 110270: Index.cshtml (Dashboard de registros)
    └── 110271: Login.cshtml (Autenticación)
```

## URLs

- **Producción**: https://fuegoadmin.powerera.com
- **Servidor**: ws-todini.powerera.com
- **Puerto**: 5077

## Características

### Dashboard Principal
- **Total de registros**: Contador general
- **Asistirán**: Cantidad de confirmados que asistirán
- **Con foto**: Registros que tienen foto capturada
- **Check-in**: Cantidad de personas que ya llegaron al evento

### Funcionalidades
- Visualización de fotos de invitados (thumbnail con zoom)
- Filtro de búsqueda por nombre o empresa
- Exportación a Excel
- Fechas en hora local del navegador (conversión de UTC)
- Indicadores visuales de estado (badges)

### Autenticación
- Login con usuario y contraseña
- Usuarios: admin, enrique
- Sesión con cookies

## Tecnologías

- **Backend**: ASP.NET Core 9 con Razor Pages
- **Base de datos**: SQL Server (Entity Framework Core)
- **Excel**: ClosedXML
- **UI**: Bootstrap 5 + FontAwesome
- **Hosting**: Linux (systemd service)

## Estructura del Proyecto

```
AtlasFuegoAdmin/
├── Data/
│   └── AppDbContext.cs          # Contexto de Entity Framework
├── Models/
│   └── PreRegistro.cs           # Modelo de datos (compartido)
├── Pages/
│   ├── Index.cshtml             # Dashboard principal
│   ├── Index.cshtml.cs          # Code-behind con lógica
│   ├── Login.cshtml             # Página de login
│   └── Login.cshtml.cs          # Lógica de autenticación
├── wwwroot/
│   └── css/                     # Estilos
└── Program.cs                   # Configuración y autenticación
```

## Conexión a Fotos

Las fotos se obtienen del micrositio de registro:
```
FotoBaseUrl = "https://fuego.powerera.com"
```

## Deployment

```bash
# Publicar
dotnet publish -c Release -r linux-x64 --self-contained false -o ./publish-linux

# Desplegar
rsync -avz -e "ssh -i ~/.ssh/id_claude_automation" ./publish-linux/ earaiza@ws-todini.powerera.com:/var/www/atlasfuegoadmin/

# Reiniciar servicio
ssh earaiza@ws-todini.powerera.com "sudo systemctl restart atlasfuegoadmin"
```

## Base de Datos

Comparte la misma base de datos que el micrositio:
- **Server**: dbdev.powerera.com
- **Database**: Atlas2026
- **Tabla**: PreRegistros

## GitHub

- **Repositorio**: https://github.com/earaizapowerera/AtlasFuego

## Sistema Relacionado

Este panel administra los registros creados por el micrositio:
- **Micrositio**: https://fuego.powerera.com
- **Repo Micrositio**: https://github.com/earaizapowerera/micrositio-atlasfuego
