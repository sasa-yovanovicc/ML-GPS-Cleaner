# ML GPS Cleaner

Interactive web app for cleaning GPS trajectories in (near) real‑time.

Backend: ASP.NET Core (.NET 8) + EF Core (MySQL Traccar schema).  
Frontend: React (Vite) + Leaflet map.

## How it works
* Loads raw GPS points for a device & selected day.
* Computes geo features (distance, speed, acceleration, bearing change, cross‑track error).
* Applies rule‑based outlier detection:
  - Speed limit
  - Acceleration limit
  - Bearing change rate
  - Cross‑track distance
  - Hampel (median ± σ) filter on speed
* Interpolates isolated outlier points to maintain a continuous “cleaned” path.
* Returns both raw & cleaned sets via: `GET /api/positions/device/{id}/compare`.
* Frontend overlays:
  - Raw normal points (orange path)
  - Raw points flagged as outliers (red markers)
  - Cleaned path (green)
  - Interpolated points (blue markers)
* Calendar shows active days; click a day to load and auto‑fit map.

### Algorithms (current rule set)
1. Speed Threshold Filter (KPH cap)
2. Acceleration Threshold Filter (Δv/Δt)
3. Bearing Change Rate Filter (|Δbearing| / Δt)
4. Cross‑Track Deviation Filter (perpendicular distance from local segment)
5. Hampel Filter (Median Absolute Deviation) on speed series
6. Linear Interpolation of isolated removed points (bridges small single‑point gaps)

ML (in progress / test): Principal Component Analysis (reconstruction error) and Isolation‑Forest style ensemble for anomaly score fusion.

## Real‑time cleaning concept
The current implementation cleans data per request (day window). Architecture is prepared to extend to streaming (SignalR) where each new point would:
1. Be appended to a sliding window.
2. Derive incremental features vs previous point(s).
3. Run the same rule set + (future) ML score.
4. Broadcast updated cleaned polyline & outlier markers to connected clients.

UI already provides a panel for upcoming ML parameters (PCA / IsolationForest, thresholds, window size, Hampel σ, etc.).

### ML workflow (ZIP + data ingestion in progress)
1. Features dataset (historical cleaned + raw) available as two format groups:
  - Tabular: CSV, JSON (lines), XLS, XLSX, Parquet, Feather
  - Track / device logs: GPX, KML, KMZ, GeoJSON, FIT, TCX, NMEA
  @ Tabular files load directly as rows/columns. Track/device formats are parsed into point sequences then enriched with derived features (distance, Δt, speed, accel, bearing, bearing change, cross‑track) before model use.
2. ZIP bundle structure (used for import/export):
```
model_bundle.zip
  /features/          # raw feature files (csv,json,xls,xlsx,parquet)
  /config/params.json # selected hyper‑params (windowSize, thresholds, hampelSigma,...)
  /model/pca.onnx     # serialized PCA (or ML.NET .zip)
  /model/iforest.bin  # isolation forest style artifact
  README.txt          # provenance / version info
```
3. Upload bundle (or individual files) via UI (drag&drop variant) -> backend unpacks & validates schema.
4. Backend trains or loads existing models; stores hash & metadata for reproducibility.
5. Scoring endpoint returns per‑point anomaly score + combined decision (rules ∪ ML) with reason codes.
6. Feedback (Accept / Reject) is logged and can schedule model retraining.

Supported ingestion formats (in test):
 - Tabular: CSV, JSON lines, XLS, XLSX, Parquet, Feather
 - Track / device: GPX, KML, KMZ, GeoJSON, FIT, TCX, NMEA
@ Track/device inputs undergo parsing + feature derivation (same pipeline) prior to scoring; tabular inputs assumed already flattened.

## Run locally
Prerequisites: .NET 8 SDK, Node 20+, MySQL (or use docker compose).

```
docker compose up --build
```
API: http://localhost:5000  
UI: http://localhost:5173

Manual:
```
dotnet build MLGpsCleaner.sln
dotnet run --project src/Backend/MLGpsCleaner.Api/MLGpsCleaner.Api.csproj
cd src/Frontend && npm install && npm run dev
```

Connection string env var:
```
ConnectionStrings__Traccar=server=localhost;port=3306;database=traccar;user=root;password=;TreatTinyAsBoolean=false
```

## License
MIT
