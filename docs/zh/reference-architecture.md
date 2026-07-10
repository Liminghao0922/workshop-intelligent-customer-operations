# 参考架构

## 推荐设计

参考实现以 Microsoft Foundry 为中心。ACS 负责电话接入，Voice Live API 负责实时语音交互，Foundry Agent 负责对话策略，Azure AI Search 为回答提供知识 grounding，由事件驱动的 Azure Function 异步执行通话后分析。

```mermaid
flowchart LR
  Caller[客户来电] --> ACS[Azure Communication Services<br/>电话号码 / direct routing]
  ACS --> Gateway[通话网关<br/>Azure Container Apps]
  Gateway <--> VoiceLive[Voice Live API]
  VoiceLive <--> Agent[Microsoft Foundry Agent]
  Agent <--> Models[Foundry Models<br/>实时和分析模型]
  Agent --> Tools[Function tools<br/>工单和升级动作]
  Agent --> SearchTool[Azure AI Search tool<br/>Foundry project connection]
  SearchTool --> Search[(Azure AI Search<br/>多语言知识索引)]
  Gateway --> Blob[(Azure Storage<br/>通话产物)]
  Blob --> EventHub[(Azure Event Hubs<br/>call-ended)]
  EventHub --> Functions[Azure Functions (Event Hub trigger)<br/>Azure Container Apps]
  Functions --> AnalyticsAgent[Foundry analytics agent/model]
  AnalyticsAgent --> Blob
  Functions --> CRM[(Dynamics 365 Customer Service<br/>工单系统)]
  Blob --> Dashboard[Instructor console / Power BI]
```

## 实时通话流程

1. 客户拨打 ACS 号码或路由到 ACS 的号码。
2. ACS 将通话事件发送给通话网关。
3. 通话网关创建 Voice Live session，并把客户对话连接到 Foundry Agent。
4. Foundry Agent 使用 Foundry Models、Azure AI Search grounding 和 function tools 来回答、判断是否升级或创建工单。
5. 通话网关把通话产物保存到 Azure Storage。

## 通话后分析流程

1. 已完成通话产物的引用以事件方式写入 Azure Event Hubs。
2. Functions 调用 Foundry analytics agent/model，生成考虑脱敏的摘要、意图、情绪、实体、解决状态和行动项。
3. 结构化分析结果保存回 Storage，并展示到讲师控制台或 Power BI。

## 服务映射

| 需求 | Azure 服务 |
| --- | --- |
| 电话接入 | Azure Communication Services |
| 实时语音到语音 | Voice Live API |
| 对话编排 | Microsoft Foundry Agent |
| 模型访问 | Foundry Models |
| 知识 grounding | 连接到 Foundry 的 Azure AI Search tool |
| 应用托管 | Azure Container Apps |
| 通话后工作流 | Azure Container Apps 上的 Azure Functions (Event Hubs trigger) |
| 工单/案件管理 | Dynamics 365 Customer Service |
| 通话产物存储 | Azure Storage |
| 监控 | Application Insights 和 Log Analytics |
| 身份和访问 | Managed identity、RBAC、Key Vault |

## 多语言支持

- 为英文、日文、中文配置语音和模型指令。
- 在通话元数据中保存语言代码。
- 在 Azure AI Search 中索引多语言知识内容。
- 分语言评估语音坐席行为和通话后分析质量。
