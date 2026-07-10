# 演示指南

## 本地演示

本地应用默认运行在 mock 模式，不需要 Azure 凭据，可用于测试控制台和 API 流程。

```powershell
cd smart-call-center-quiz
dotnet run --project src/aspire/IntelligentCustomerOperations.AppHost
```

打开终端中显示的 Aspire dashboard，进入 `gateway` 服务的地址。

## 演示步骤

1. 选择英文、日文或中文。
2. 点击 **Start simulated call**。
3. 展示客户与 AI 语音坐席的对话。
4. 展示 AI 转人工判断和 CRM 工单。
5. 展示通话后分析：PII 脱敏、摘要、意图、情绪、实体、行动项和仪表板指标。
6. 把每个演示面板对应回架构图中的组件。

## 与真实 Azure 实现的对应关系

| 演示面板 | 真实实现 |
| --- | --- |
| 实时通话转写 | Azure Communication Services 加 Voice Live API |
| 知识库回答 | Foundry Agent 加 Azure AI Search tool |
| 转人工卡片 | Foundry function tool 调用 CRM/联络中心 API |
| 通话产物存储 | Azure Storage |
| 通话后流程 | Azure Container Apps 上的 Azure Functions (Event Hubs trigger) |
| 摘要和情绪 | Foundry analytics agent/model |
| 仪表板卡片 | Instructor console 或 Power BI |

## 讲师提示

本地 mock 模式是确定性的，适合课堂使用。部署路径使用 ACS、Voice Live API、Foundry Agent/Models、Azure AI Search、Azure Storage、Azure Container Apps 上的事件驱动 Azure Functions 和 CRM/tool API。详情见 `docs/deployment.md`。

