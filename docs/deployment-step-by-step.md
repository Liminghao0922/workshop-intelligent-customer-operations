# Smart Call Center Azure 部署手册（Step by Step）

本文档面向 Azure 初学者，按顺序执行即可完成本项目的云端部署。

适用仓库：smart-call-center-quiz  
部署方式：Azure Developer CLI (azd) + Bicep + Azure Container Apps

---

## 0. 你将部署出什么

完成后会得到以下核心资源（同一资源组内）：

- 一个对外访问的 Gateway 容器应用
- 一个处理 post-call 队列事件的 Worker 容器应用
- 存储账号（含队列）
- Azure AI Search
- Key Vault
- Container Registry
- Azure Communication Services（ACS）资源主体
- Microsoft Foundry Account（AIServices）
- Microsoft Foundry Project

注意：

- 本模板会创建 ACS 资源本身。
- 本模板不会自动购买电话号码（PSTN）或配置 Direct Routing。
- 本模板不会自动把 ACS 连接串写入运行时变量，你需要在部署后手动设置。
- 本模板会自动创建 Foundry Account 与 Foundry Project。
- 模板会在 `azd provision` 后自动尝试创建 Foundry Agent（通过 `azure.yaml` 的 postprovision hook 调用脚本）。
- Voice Live 模型可由 Bicep 自动部署（前提是设置 `VOICE_LIVE_MODEL`、`VOICE_LIVE_MODEL_NAME`、`VOICE_LIVE_MODEL_VERSION`）。

项目中的部署定义来自以下文件：

- azure.yaml
- infra/main.bicep
- infra/modules/app.bicep

---

## 1. 部署前准备（一次性）

### 1.1 必备条件

请确认你具备：

- 可用 Azure 订阅
- 该订阅至少 Contributor 权限（推荐 Owner，避免 RBAC 赋权失败）
- Azure Cloud Shell 可用，且已具备 Azure CLI (`az`) 与 Azure Developer CLI (`azd`)。
- 如需本地镜像构建/推送，另行准备 Docker；本步骤默认不要求本机安装。

可选（用于完整电话流程）：

- Azure Communication Services (ACS) 可用连接串
- Foundry Agent 与模型可用配置

### 1.2 打开 Azure Cloud Shell 并切到项目根目录

在 Azure Cloud Shell (PowerShell) 中执行：

```powershell
cd $HOME\clouddrive\smart-call-center-quiz
```

### 1.3 登录 Azure CLI 和 azd（Cloud Shell）

```powershell
az login
azd auth login
```

如果你有多个订阅，先选择目标订阅：

```powershell
az account list --output table
az account set --subscription "<你的订阅ID或订阅名>"
az account show --output table
```

---

## 2. 创建部署环境（azd env）

### 2.1 创建环境

环境名建议使用英文小写，例如：`smart-call-center-dev`。

```powershell
azd env new smart-call-center-dev
```

### 2.2 设置部署区域

本项目默认区域是 `japaneast`，建议保持一致：

```powershell
azd env set AZURE_LOCATION japaneast
```

### 2.3 查看当前环境变量

```powershell
azd env get-values
```

---

## 3. 配置应用运行所需参数

至少建议设置以下变量（特别是 ACS 与 Foundry 相关）：

```powershell
azd env set ACS_CONNECTION_STRING "<你的ACS连接串>"
azd env set ACS_CALLBACK_SECRET "<你自定义的回调密钥>"
azd env set FOUNDRY_AGENT_MODEL "<Agent 使用的模型部署名，例如 gpt-4o-realtime-preview>"
azd env set VOICE_LIVE_MODEL "<模型部署名，例如 gpt-4o-realtime-preview>"
azd env set VOICE_LIVE_MODEL_NAME "<模型名，例如 gpt-4o-realtime-preview>"
azd env set VOICE_LIVE_MODEL_VERSION "<模型版本，例如 2025-03-01-preview>"
```

说明：

- 若你先做基础部署验证，也可以先不填完整 AI 参数，后续再补。
- 变量值会保存在当前 azd 环境中，不会自动写入代码文件。

---

## 3.5 Foundry 与 Voice Live（自动化 + 可选 Toolkit）

这一节是很多人最容易漏掉的地方。

本仓库的 Bicep 会创建 Foundry Account 和 Foundry Project；另外，当你提供模型参数时也会自动部署 Voice Live 模型。Agent 创建由部署脚本自动完成。

请按顺序执行：

### 3.5.1 自动创建 Agent（默认流程）

在 `azd provision` 完成后，会自动执行：

- `scripts/foundry/ensure-agents.ps1`

该脚本会在 Foundry Project 中确保以下两个 Agent 存在：

- 主对话 Agent（`smart-call-center-main`）
- Post-call Analytics Agent（`smart-call-center-analytics`）

并自动回写环境变量：

- `FOUNDRY_AGENT_ID`
- `FOUNDRY_ANALYTICS_AGENT_ID`

如果你想手动重跑：

```powershell
./scripts/foundry/ensure-agents.ps1
```

若脚本提示模型为空，请先设置：

```powershell
azd env set FOUNDRY_AGENT_MODEL "<模型部署名>"
```

### 3.5.2 部署 Voice Live 模型

Voice Live 模型现在由 Bicep 自动部署。你只需要在部署前提供模型参数：

```powershell
azd env set VOICE_LIVE_MODEL "<模型部署名，例如 gpt-4o-realtime-preview>"
azd env set VOICE_LIVE_MODEL_NAME "<模型名，例如 gpt-4o-realtime-preview>"
azd env set VOICE_LIVE_MODEL_VERSION "<模型版本，例如 2025-03-01-preview>"
```

若以上三个参数任意为空，模板会跳过模型部署。

### 3.5.3 把自动生成的 Agent ID 应用到运行时

```powershell
azd deploy
```

说明：

- `azd deploy` 用于把自动生成的 Agent ID 和其他变量更新到 Container Apps。
- 若这一步未完成，应用虽然能启动，但 AI 对话和 post-call 分析可能不可用或退化。

### 3.5.4 可选：使用 Foundry Toolkit 管理 Agent 配置

如果你希望把 Agent 的 prompt、tools、版本放入仓库做版本管理，建议使用 Foundry Toolkit：

1. 在本地用 Toolkit 调整 Agent 定义。
2. 将 Agent 定义文件提交到仓库。
3. 在 CI/CD 或本地执行同步命令更新 Project 中的 Agent。

建议策略：

- 日常自动化创建与兜底：`ensure-agents.ps1`
- 高级配置管理与团队协作：Foundry Toolkit

---

## 4. 部署资源（Provision）

先创建 Azure 资源：

```powershell
azd provision
```

成功后你会看到类似输出：

- Resource group 已创建
- Container Registry / Container Apps / Storage / Search / Key Vault 已完成

如果失败，先看第 8 节排错。

---

## 5. 部署应用（Deploy）

在资源创建完成后，部署应用代码：

```powershell
azd deploy
```

这个步骤会：

- 构建容器镜像
- 推送镜像到 ACR
- 更新 Gateway、API、Portal 与 Worker 的 Container App 版本

首次部署通常比后续慢，等待 5-15 分钟都正常。

---

## 6. 获取访问地址并验证

### 6.1 查看部署输出

```powershell
azd env get-values
```

重点看：

- `WEB_URL`
- `API_URL`
- `FRONTEND_URL`

### 6.2 打开应用

在浏览器访问：

- `https://<FRONTEND_URL>`

### 6.3 调用健康检查（示例）

```powershell
Invoke-RestMethod -Method Get -Uri "https://<WEB_URL>/healthz"
```

---

## 7. 接入 ACS 回调（电话场景必做）

### 7.1 先确认 ACS 是否已创建

执行：

```powershell
az resource list --resource-group "rg-smart-call-center-dev" --resource-type "Microsoft.Communication/communicationServices" --output table
```

如果你使用的环境名不是 `smart-call-center-dev`，把资源组名替换成 `rg-<你的环境名>`。

### 7.2 获取 ACS 连接串并写入 azd 环境

最适合初学者的方法：在 Azure Portal 打开刚创建的 Communication Services 资源，进入 `Keys` 页面，复制 `Connection string`。

然后执行：

```powershell
azd env set ACS_CONNECTION_STRING "<从Portal复制的连接串>"
azd env set ACS_CALLBACK_SECRET "<你自定义的回调密钥>"
azd deploy
```

说明：最后一条 `azd deploy` 是为了把新环境变量更新到已部署的容器应用。

### 7.3 电话能力准备（号码或路由）

二选一：

- 购买 ACS 电话号码（PSTN）
- 配置 Direct Routing（企业语音网关）

如果号码/路由未准备，Webhook 回调可测，但真实电话呼入无法完成。

在 ACS 或你的呼叫编排流程中，把回调地址配置为：

- `https://<WEB_URL>/api/acs/events`
- `https://<WEB_URL>/api/acs/callbacks/{callId}`

然后发起一通测试电话，检查：

- 是否收到事件
- 通话状态是否更新
- transcript 是否落库
- post-call 分析任务是否进入队列并被消费

---

## 8. 新手最常见问题与处理

### 8.1 `azd provision` 失败：权限不足

现象：报错包含 `AuthorizationFailed`、`roleAssignments/write` 等。  
处理：

1. 确认你在目标订阅有 Contributor 或 Owner。
2. 如果是企业订阅，联系管理员开通创建资源组/RBAC 赋权权限。
3. 等待权限传播 5-10 分钟后重试。

### 8.2 `azd deploy` 失败：容器镜像推送或拉取失败

现象：ACR push/pull 报错，或 Container App Revision 启动失败。  
处理：

1. 重试一次 `azd deploy`（RBAC 有时需要传播时间）。
2. 确认 Docker Desktop 正常运行。
3. 用 `azd logs` 或 Azure Portal 查看容器应用日志。

### 8.3 区域容量或服务不可用

现象：某些资源在 `japaneast` 创建失败。  
处理：

1. 换区域（例如 `eastus`）后重试：

```powershell
azd env set AZURE_LOCATION eastus
azd provision
azd deploy
```

1. 同时确认 ACS / Foundry 在目标区域可用。

### 8.4 部署成功但功能不完整

现象：页面打开了，但电话或 AI 分析不工作。  
处理：

1. 检查第 3 节变量是否都设置了。
2. 检查 ACS 回调 URL 是否配置正确。
3. 检查 Foundry Agent ID 与模型是否可用。
4. 检查 `VOICE_LIVE_MODEL_NAME` 和 `VOICE_LIVE_MODEL_VERSION` 是否是目标区域支持的组合。

---

## 9. 一键部署命令（可选）

如果你希望少步骤，也可直接：

```powershell
azd up
```

`azd up` = `provision + deploy`。  
但对初学者更推荐分两步执行，出错更容易定位。

---

## 10. 验收清单（Checklist）

部署完成后，逐项确认：

1. `azd env get-values` 能看到 `WEB_URL` 和 `FRONTEND_URL`。
2. 浏览器可打开 `https://<FRONTEND_URL>`。
3. Gateway 接口可响应。
4. ACS 回调已指向正确 URL。
5. 测试通话后可看到 transcript 与 post-call 分析链路执行。

---

## 11. 回滚与清理（避免继续计费）

如果这是临时环境，测试完建议删除：

```powershell
azd down
```

执行前请确认环境中没有需要保留的数据。

---

## 12. 推荐下一步

部署成功后，建议继续做三件事：

1. 配置 Application Insights 与告警规则。
2. 给生产环境单独创建 `prod` 的 azd 环境并分离密钥。
3. 给 ACS 回调增加签名校验与幂等处理，提升生产稳定性。
