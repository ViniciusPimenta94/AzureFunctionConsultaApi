# ConsultaAPI - Azure Function

Este projeto é uma Azure Function desenvolvida em C# que consulta uma API externa e processa a resposta para fins de integração.

## 🚀 Funcionalidades

- Consulta automatizada a API externa
- Tratamento e exibição da resposta
- Baseado em Azure Functions

## 🧰 Requisitos

- .NET 6 SDK ou superior
- Azure Functions Core Tools
- Visual Studio 2022 (recomendado)
- Azure Storage Emulator ou conta de armazenamento real (em produção)

## ▶️ Como Executar

1. Restaurar os pacotes:
```bash
dotnet restore
```

2. Executar localmente:
```bash
func start
```

3. Ou rodar como app .NET:
```bash
dotnet run --project FunctionApp/FunctionApp.csproj
```

## 📁 Estrutura

- `Function.cs`: lógica da Azure Function
- `FunctionApp.csproj`: definição do projeto .NET
- `local.settings.json`: configurações locais (não deve ser commitado)
