# FinanceOS | Executive Profitability & P&L Dashboard

# FinanceOS | Executive Profitability & P&L Dashboard

## 🚀 Deployment Guide
This project has been split into an Angular Frontend and an ASP.NET Core Web API Backend.

### 1. Backend Deployment (Render / Azure / AWS / Railway)
**Recommended Platform**: **[Render](https://render.com/)** (Web Service)

**Steps:**
1. Push this code to a GitHub repository.
2. Go to the Render Dashboard -> **New** -> **Web Service**.
3. Connect your GitHub repository and select the `dotnet-backend` directory as the Root Directory.
4. Render will detect the .NET environment. Set the build command to `dotnet build` and start command to `dotnet run`.
5. Create the following **Environment Variables** in Render:
   - `ASPNETCORE_URLS`: `http://0.0.0.0:10000` (or the port Render provides)
   - `JIRA_URL`: (Your Jira domain url)
   - `JIRA_EMAIL`: (Your Jira email)
   - `JIRA_API_TOKEN`: (Your Jira API token)
6. Click **Deploy**. Note the URL provided.

### 2. Frontend Deployment (Vercel / Netlify / Firebase)
**Recommended Platform**: **[Vercel](https://vercel.com/)**

**Steps:**
1. Go to the Vercel Dashboard -> **Add New** -> **Project**.
2. Connect your GitHub repository.
3. Select the `frontend` folder as the **Root Directory**. Vercel will detect Angular and configure build settings.
4. Open the **Environment Variables** section and add:
   - `API_BASE_URL`: The deployed URL of your .NET backend.
5. Click **Deploy**.

---

## 🛠️ Local Development

### Backend Setup (.NET)
1. Open a terminal and navigate to the `dotnet-backend` folder:
   ```bash
   cd dotnet-backend
   ```
2. Restore and Build:
   ```bash
   dotnet build
   ```
3. Start the server:
   ```bash
   dotnet run --urls "http://localhost:3001"
   ```

### Frontend Setup (Angular)
1. Open another terminal and navigate to the `frontend` folder:
   ```bash
   cd frontend
   ```
2. Install dependencies:
   ```bash
   npm install
   ```
3. Start the Angular development server:
   ```bash
   npm start
   ```
*(Frontend runs on `http://localhost:4200` locally and connects to the backend on port 3001).*
