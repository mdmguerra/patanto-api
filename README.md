# Patanto API

API REST construida en **.NET 8** para buscar y analizar patentes utilizando la [PatentsView API](https://search.patentsview.org/api/v1/patent/) de la USPTO, con análisis de similitud impulsado por IA mediante [Groq](https://console.groq.com/) (modelo `llama-3.3-70b-versatile`).

## Características

- **Búsqueda directa de patentes**: Consulta personalizada a la API de PatentsView con control total sobre los parámetros de búsqueda.
- **Análisis de patentes por palabras clave**: Busca patentes relevantes por términos y campo de aplicación, y obtiene un análisis de similitud con IA para cada resultado.
- **Comparación de similitud entre patentes**: Compara una patente principal con un conjunto de patentes y recibe una evaluación de similitud generada por IA.
- **Swagger UI**: Documentación interactiva de la API disponible en la raíz del servidor.
- **Manejo global de errores**: Middleware centralizado para excepciones no controladas.
- **CORS abierto**: Permite solicitudes desde cualquier origen.

## Requisitos previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- API Key de [PatentsView](https://search.patentsview.org/)
- API Key de [Groq](https://console.groq.com/)

## Configuración

1. Clona el repositorio:
   ```bash
   git clone https://github.com/mdmguerra/patanto-api.git
   cd patanto-api
   ```

2. Edita `appsettings.json` (o usa [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)) con tus credenciales:
   ```json
   {
     "PatentsView": {
       "BaseUrl": "https://search.patentsview.org/api/v1/patent/",
       "ApiKey": "TU_API_KEY_PATENTSVIEW",
       "TimeoutSeconds": 30,
       "MaxResults": 50
     },
     "GrokSettings": {
       "ApiKey": "TU_API_KEY_GROQ"
     }
   }
   ```

   > **Recomendación**: Para no exponer credenciales en el repositorio, usa User Secrets en desarrollo:
   > ```bash
   > dotnet user-secrets set "PatentsView:ApiKey" "TU_API_KEY_PATENTSVIEW"
   > dotnet user-secrets set "GrokSettings:ApiKey" "TU_API_KEY_GROQ"
   > ```

3. Restaura las dependencias y ejecuta la aplicación:
   ```bash
   dotnet restore
   dotnet run
   ```

La API estará disponible en:
- HTTP: `http://localhost:5070`
- HTTPS: `https://localhost:7174`

La **Swagger UI** se abre directamente en `http://localhost:5070`.

## Endpoints

### `POST /api/Search`

Búsqueda directa en la API de PatentsView con parámetros personalizados.

**Cuerpo de la solicitud:**

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `q`   | object | ✅ | Criterio de búsqueda en formato JSON de PatentsView |
| `f`   | string[] | ❌ | Campos a retornar |
| `s`   | object[] | ❌ | Criterio de ordenamiento |
| `o`   | object | ❌ | Opciones de paginación |

**Ejemplo:**
```json
{
  "q": { "_text_all": { "patent_abstract": "machine learning" } },
  "f": ["patent_id", "patent_title", "patent_abstract", "patent_date"],
  "o": { "size": 5 }
}
```

---

### `POST /api/Analize`

Busca patentes por palabras clave y campo de aplicación, y analiza cada resultado con IA para determinar su similitud con la idea del usuario.

**Cuerpo de la solicitud:**

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `palabrasClaves` | string[] | ✅ | Palabras clave de la invención |
| `aplicacion` | string | ❌ | Campo o área de aplicación |

**Ejemplo:**
```json
{
  "palabrasClaves": ["renewable energy", "solar panel", "battery storage"],
  "aplicacion": "smart grid systems"
}
```

**Respuesta:**
```json
{
  "total_patents": 5,
  "search_keywords": ["renewable energy", "solar panel", "battery storage"],
  "application": "smart grid systems",
  "patents": [
    {
      "patent_id": "12345678",
      "patent_title": "Solar Energy Storage System",
      "patent_abstract": "...",
      "patent_date": "2023-05-10",
      "analysis": {
        "similarity_score": 82,
        "similarity_note": "Esta patente presenta una similitud del 82% con la idea del usuario...",
        "risk_level": "ALTO"
      }
    }
  ]
}
```

Los niveles de riesgo (`risk_level`) son:
- `ALTO` — similitud ≥ 80%
- `MEDIO` — similitud ≥ 50%
- `BAJO` — similitud ≥ 25%
- `MUY BAJO` — similitud < 25%

---

### `POST /api/Analize/similarity`

Compara una patente principal con múltiples patentes y devuelve un análisis de similitud generado por IA para cada comparación.

**Cuerpo de la solicitud:**

```json
{
  "patent": {
    "patent_number": "US1234567",
    "title": "Smart Water Filtration Device",
    "abstract": "A device for filtering water using ceramic membranes..."
  },
  "comparison_patents": [
    {
      "patent_number": "US9876543",
      "title": "Ceramic Membrane Water Filter",
      "abstract": "..."
    },
    {
      "patent_number": "US5555555",
      "title": "UV Water Purification System",
      "abstract": "..."
    }
  ]
}
```

**Respuesta:**
```json
{
  "similarity_analysis": [
    {
      "patent_number": "US9876543",
      "similarity_note": "Esta patente muestra una similitud técnica del 75%...",
      "similarity_score": 75
    },
    {
      "patent_number": "US5555555",
      "similarity_note": "Esta patente muestra una similitud técnica del 20%...",
      "similarity_score": 20
    }
  ]
}
```

## Tecnologías utilizadas

| Tecnología | Versión | Uso |
|------------|---------|-----|
| .NET | 8.0 | Framework principal |
| ASP.NET Core | 8.0 | API REST |
| Swashbuckle (Swagger) | 6.5.0 | Documentación interactiva |
| OpenAI SDK | 2.8.0 | Cliente para Groq API |
| PatentsView API | v1 | Base de datos de patentes USPTO |
| Groq (llama-3.3-70b-versatile) | — | Análisis de similitud con IA |

## Estructura del proyecto

```
patanto-api/
├── Configuration/
│   └── PatentsViewOptions.cs        # Opciones de configuración de PatentsView
├── Controllers/
│   ├── AnalizeController.cs         # Análisis de patentes con IA
│   └── SearchController.cs          # Búsqueda directa de patentes
├── Extensions/
│   └── ServiceCollectionExtensions.cs # Registro de servicios
├── Middleware/
│   └── GlobalExceptionHandlerMiddleware.cs # Manejo global de errores
├── Models/
│   ├── Grok/                        # Modelos de solicitud/respuesta de IA
│   ├── PatentsView/                 # Modelos de la API de PatentsView
│   └── Request/                     # Modelos de solicitudes de entrada
├── Services/
│   ├── Interfaces/
│   │   └── IPatentsViewService.cs   # Interfaz del servicio de patentes
│   ├── GrokAnalysisService.cs       # Servicio de análisis con Groq IA
│   └── PatentsViewService.cs        # Cliente de la API de PatentsView
├── appsettings.json                 # Configuración de la aplicación
└── Program.cs                       # Punto de entrada y configuración de servicios
```

## Licencia

Este proyecto es de uso privado. Consulta con el autor para más información.
