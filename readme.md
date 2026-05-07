# GasBet Function

Azure Function App fuer geplante Hintergrundjobs im GasBet-Projekt. Die Function App fuehrt zeitgesteuert interne Backend-Endpunkte aus, um Quoten zu aktualisieren, Events zu erzeugen, Events zu sperren, Ergebnisse aufzulösen und alte Snapshots zu löschen.

## Tech Stack

- .NET 10
- Azure Functions v4
- Azure Functions Isolated Worker
- Timer Trigger
- HttpClient Factory
- Application Insights

## Was die Function App macht

Die Function App enthält mehrere Timer-Trigger, die interne API-Endpunkte des Backends per `POST` aufrufen. Zur Authentifizierung wird ein interner API-Key über den Header `X-Internal-Key` mitgegeben.

Aktuell sind folgende Jobs vorhanden:

| Function | Zeitplan | Zweck | Endpoint |
| --- | --- | --- | --- |
| `PollPrices` | jede Minute | Holt aktuelle Preis-/Quotendaten | `/api/internal/poll-prices` |
| `CreateEvents` | stündlich um Minute 0 | Erzeugt neue Events | `/api/internal/create-events` |
| `LockEvents` | stündlich bei Minute 0, Sekunde 5 | Sperrt Events rechtzeitig vor Start | `/api/internal/lock-events` |
| `ResolveEvents` | stündlich bei Minute 0, Sekunde 10 | Löst abgeschlossene Events auf | `/api/internal/resolve-events` |
| `DeleteOldSnapshots` | täglich um 03:00 | Bereinigt alte Snapshot-Daten | `/api/internal/delete-old-snapshots` |

## Architektur in Kurzform

1. Azure Functions startet den jeweiligen Timer-Trigger.
2. Die Function liest `BackendApi:BaseUrl` und `InternalApi:Key` aus der Konfiguration.
3. Ueber `HttpClientFactory` wird ein `POST` Request an den internen Backend-Endpunkt gesendet.
4. Fehler werden geloggt und als Exception geworfen, erfolgreiche Antworten werden protokolliert.

## Lokale Entwicklung

### Voraussetzungen

- .NET 10 SDK
- Azure Functions Core Tools v4
- Azurite oder ein anderer lokaler Storage-Ersatz, falls `UseDevelopmentStorage=true` verwendet wird

### Lokale Konfiguration

Lege für die lokale Entwicklung eine `local.settings.json` mit eigenen Werten an. Beispiel:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "BackendApi:BaseUrl": "http://localhost:5037",
    "InternalApi:Key": "replace-me"
  }
}
```

### Starten

```bash
dotnet restore
func start
```

Alternativ:

```bash
dotnet build
```

und dann über die Azure Functions Tools oder Visual Studio starten.

## Deployment

Die App ist für ein Azure-Deployment als Linux Function App ausgelegt. Sensible Deploy-Dateien wie Publish-Profile oder lokale Settings sollten nicht in ein öffentliches Portfolio-Repository aufgenommen werden.

## Portfolio-Kontext

Dieses Projekt zeigt:

- geplante Backend-Automatisierung mit Azure Functions
- Trennung von Scheduler und eigentlicher Business-Logik im Backend
- abgesicherte interne Endpunktaufrufe per Konfiguration und Header
- observability-orientiertes Logging mit Application Insights

## Verbesserungsmöglichkeiten

- Retry-Strategien und Timeouts explizit konfigurieren
- Typed oder named `HttpClient` Registrierung einführen
- Fehlerbehandlung differenzieren statt generischer `Exception`
- Health- und Monitoring-Dokumentation ergänzen
