# Code Explanation & Architectural Overview

## 1. High-Level Architecture
FinanceOS follows a standard Client-Server architectural pattern:
* **Frontend (Client):** An Angular 18 Single Page Application (SPA) responsible for UI rendering, staging data files (CSV/Excel), collecting Jira credentials, and presenting dashboards.
* **Backend (Server):** An ASP.NET Core Web API application that acts as the computation and algorithmic engine. It parses file binaries, connects to Jira's REST API, runs cross-referencing algorithms for project costing and resource utilization, and dynamically outputs `.pptx` or `.xlsx` files.
* **Communication:** The frontend communicates via REST endpoints (`/api/*`). For long-running tasks like fetching Jira worklogs, it utilizes Server-Sent Events (SSE) via the `/api/jira/extract` endpoint to push incremental progress states.

## 2. Folder Structure
### 2.1 Backend (`/dotnet-backend`)
* `Program.cs`: The entry point. Configures dependency injection, CORS, and registers core services (`IAppState`, `IFileProcessingService`, etc.).
* `Controllers/`: Contains API controllers (`UploadController`, `CalculationController`, `SyncHealthController`, `JiraController`).
* `Services/`: The business logic layer.
    - `FinanceEngineService.cs`: Logic for project-level math and margin calculations.
    - `ResourceEngineService.cs`: Logic for resource utilization and productivity analysis.
    - `FileProcessingService.cs`: Handles CSV/Excel parsing using `CsvHelper` and `ClosedXML`.
    - `ExportService.cs`: Generates reports using `ClosedXML` (Excel) and `DocumentFormat.OpenXml` (PowerPoint).
    - `AppState.cs`: A singleton service that holds the in-memory state of uploaded data.
* `Models/DomainModels.cs`: Unified records and classes representing the domain data (Jira worklogs, RateCard, etc.).

### 2.2 Frontend (`/frontend/src/app`)
* `/components/`: Reusable, atomic UI widgets.
* `/pages/sync-center/`: Control panel for file uploads and sync health verification.
* `/pages/dashboard/`: Vision reporting views that consume calculated metrics.
* `/services/finance-api.service.ts`: Angular `HttpClient` wrapper for backend communication.

## 3. Key Logic & Algorithms

### 3.1 Server-Sent Events (SSE) for Jira Extraction
* **Implementation:** The `JiraController` uses `Response.WriteAsync` to stream data back to the client using the `text/event-stream` format. This allows the frontend to show real-time progress of long-running Jira extractions.

### 3.2 Automated Anomaly Detection
* **Fuzzy String Resolution:** Employs `FuzzySharp` (Levenshtein distance) to resolve minor typos between strict HR system names and informal Jira usernames during attendance reconciliation.
* **Sync Health:** The `SyncHealthController` cross-references in-memory buffers to identify orphaned resources or missing Jira credentials.

### 3.3 Executive Presentation Engine
* **Implementation:** Instead of using third-party JS libraries, the .NET backend uses the `DocumentFormat.OpenXml` SDK to programmatically build native PowerPoint slides. Charts are built as native MS Office vectors for maximum fidelity and interactivity.

## 4. Detailed Logic Flow: "Life of a Data Sync"

### Step 1: Ingestion & Parsing
1. **Upload:** Files are POSTed to `/api/upload/{type}`.
2. **Parsing:** `FileProcessingService` uses streams to read bytes and converts them to JSON-mapped models.
3. **State:** Data is stored in the `IAppState` singleton for the duration of the session.

### Step 2: The Core Algorithm
1. **Calculation:** When `/api/calculate` is called, the engine iterates through Jira logs.
2. **Capping:** Hours are capped based on `RequiredHours` from the resource list.
3. **Costing:** `Cost = CappedHours * HourlyRate` (calculated from RateCard monthly salary).

### Step 3: Output Generation
1. **Export:** The system builds a binary buffer representing an Excel or PowerPoint file.
2. **Streaming:** The file is streamed back to the user with appropriate MIME types (`application/vnd.openxmlformats-officedocument.*`).
