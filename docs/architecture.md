# Arquitetura inicial

## Objetivo

O APPLoja Remote Agent é o componente local Windows responsável por executar tarefas remotas, controladas e auditáveis solicitadas pelo ecossistema APPLoja.

A arquitetura evita expor um shell irrestrito ao agente de IA. Toda operação remota deve existir como uma ferramenta tipada e registrada explicitamente.

## Componentes atuais

### APPLoja.Remote.Core

Contém:

- contratos do protocolo de jobs;
- abstração `IRemoteTool`;
- `ToolRegistry`, responsável por permitir somente ações registradas;
- ferramentas de diagnóstico somente leitura.

Ferramentas iniciais:

- `system.info`;
- `system.processes`;
- `software.installed`.

### APPLoja.Remote.Service

Worker Service para Windows responsável por:

1. iniciar como serviço;
2. abrir conexão WebSocket de saída com o backend;
3. autenticar o dispositivo por `DeviceId` + token;
4. enviar o evento `agent.hello`;
5. receber jobs JSON;
6. delegar a execução ao `ToolRegistry`;
7. devolver `RemoteJobResult`.

O serviço é desativado por padrão (`RemoteAgent:Enabled=false`).

## Modelo de segurança

Regras já aplicadas nesta etapa:

1. sem shell genérico;
2. allowlist implícita pelo registro de ferramentas;
3. jobs desconhecidos são rejeitados;
4. jobs com `RequiresApproval=true` são rejeitados até existir fluxo explícito de aprovação;
5. limite configurável para tamanho das mensagens recebidas;
6. conexão iniciada pelo dispositivo, sem necessidade de porta de entrada no computador do cliente;
7. autenticação por Bearer token no WebSocket;
8. segredos não são versionados.

## Configuração

A configuração pode ser fornecida por `appsettings.json`, `appsettings.Local.json` ou variáveis de ambiente do .NET.

Exemplo por ambiente:

```text
RemoteAgent__Enabled=true
RemoteAgent__ServerUrl=wss://servidor/agent
RemoteAgent__DeviceId=device-id
RemoteAgent__DeviceToken=secret
```

## Protocolo inicial

Job recebido:

```json
{
  "id": "job-123",
  "action": "system.info",
  "parameters": null,
  "createdAt": "2026-08-20T17:00:00Z",
  "requiresApproval": false
}
```

Resultado:

```json
{
  "jobId": "job-123",
  "action": "system.info",
  "success": true,
  "data": {},
  "error": null,
  "completedAt": "2026-08-20T17:00:01Z"
}
```

## Próximas etapas

1. pareamento de dispositivo e emissão/rotação de credenciais;
2. persistência local segura do token;
3. heartbeat e presença online;
4. idempotência e confirmação de recebimento de jobs;
5. trilha de auditoria assinada;
6. ferramenta de download com allowlist, hash e validação de assinatura;
7. ferramentas de arquivos com escopo de diretórios;
8. WinGet/MSI/EXE controlados;
9. Office Deployment Tool;
10. processo separado para Desktop/UI Automation;
11. fluxo de aprovação humana;
12. Computer Use como fallback visual.

## Princípio de execução

A ordem preferencial das automações será:

1. API/ferramenta tipada;
2. PowerShell/WinGet/MSI/ODT controlado;
3. Windows UI Automation;
4. Computer Use por visão.

Computer Use não deve substituir operações determinísticas disponíveis por ferramentas estruturadas.
