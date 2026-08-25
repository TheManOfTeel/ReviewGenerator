# Review Generator

Review Generator creates fictional video game reviews from the local Amazon video game review dataset. The application uses .NET 10, Angular 22, and a second-order Markov model. Dataset ingestion happens when the API starts; no external model or network service is required.

## Requirements

- .NET SDK 10.0 or later
- Node.js 20.19 or later
- npm
- The dataset at `Source/DataSet/reviews_Video_Games_5.json.gz`

On macOS or Linux, trust the local HTTPS certificate once:

```bash
dotnet dev-certs https --trust
```

## Run the application

Run from `Source/`:

```bash
dotnet run
```

The ASP.NET host loads the dataset and starts the Angular development server through the SPA proxy.

- Web application: <https://localhost:44413>
- ASP.NET API: <https://localhost:7180>
- Generate endpoint: <https://localhost:7180/Api/generate>

## Build and test

```bash
cd Source/ClientApp
npm ci
npm run build
npm test -- --watch=false
```

From the repository root, run the .NET tests with:

```bash
dotnet test ReviewGenerator.sln
```
