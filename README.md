# APPLoja Remote Agent

Agente Windows para execução remota, controlada e auditável de tarefas de suporte do ecossistema APPLoja.

## Estado atual

O MVP inicial já contém:

- Worker Service para Windows;
- conexão WebSocket de saída com autenticação do dispositivo;
- protocolo tipado de jobs e resultados;
- registro fechado de ferramentas, sem shell genérico;
- bloqueio de jobs que exigem aprovação;
- diagnóstico de sistema (`system.info`);
- listagem de processos (`system.processes`);
- inventário de software instalado (`software.installed`);
- testes automatizados;
- CI em `windows-latest`.

A implementação desta etapa é propositalmente somente leitura. Instalação de software, manipulação de arquivos e automação visual serão adicionadas em etapas posteriores com políticas específicas de segurança e auditoria.

## Requisitos de desenvolvimento

- .NET SDK 8
- Windows 10/11 para executar o serviço completo

## Build

```powershell
dotnet restore RemoteAgent.sln
dotnet build RemoteAgent.sln --configuration Release
dotnet test RemoteAgent.sln --configuration Release
```

## Executar localmente

Por segurança o agente vem desativado. Configure por variáveis de ambiente antes de iniciar:

```powershell
$env:RemoteAgent__Enabled = "true"
$env:RemoteAgent__ServerUrl = "wss://seu-servidor/agent"
$env:RemoteAgent__DeviceId = "device-id"
$env:RemoteAgent__DeviceToken = "device-token"

dotnet run --project .\src\APPLoja.Remote.Service\APPLoja.Remote.Service.csproj
```

Não versione tokens de dispositivo.

## Ferramentas atuais

| Ação | Finalidade | Modifica o computador? |
| --- | --- | --- |
| `system.info` | Sistema operacional, arquitetura, CPU e discos | Não |
| `system.processes` | Processos em execução | Não |
| `software.installed` | Programas registrados no Windows | Não |

## Arquitetura e roadmap

Consulte [`docs/architecture.md`](docs/architecture.md).
