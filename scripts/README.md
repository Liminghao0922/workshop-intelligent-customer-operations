# Scripts

Utility scripts for setup, validation, and cleanup.

## Aspire Local Demo Scripts

- `setup-local.ps1` - checks tools, restores Aspire solution, installs docs dependencies
- `run-api.ps1` - starts Aspire AppHost (gateway + api + portal + worker)
- `run-frontend.ps1` - starts portal project only (standalone)
- `run-demo.ps1` - runs call simulation against Gateway endpoints

## Foundry Validation Script

- `foundry/validate-azure-mode.ps1` - validates non-mock azure mode runtime path through Gateway endpoints
