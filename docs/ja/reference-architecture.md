# 参考アーキテクチャ

## 推奨設計

参考実装は Azure AI Foundry を中心に構成します。ACS が電話連携を担当し、Voice Live API がリアルタイム音声対話を処理し、Foundry Agent が会話ポリシーを管理します。Azure AI Search は回答の grounding を提供し、イベント駆動の Azure Function が通話後分析を非同期に実行します。

```mermaid
flowchart LR
  Caller[顧客の通話] --> ACS[Azure Communication Services<br/>電話番号 / direct routing]
  ACS --> Gateway[通話ゲートウェイ<br/>Azure Container Apps]
  Gateway <--> VoiceLive[Voice Live API]
  VoiceLive <--> Agent[Azure AI Foundry Agent]
  Agent <--> Models[Foundry Models<br/>リアルタイム + 分析モデル]
  Agent --> Tools[Function tools<br/>チケットとエスカレーション]
  Agent --> SearchTool[Azure AI Search tool<br/>Foundry project connection]
  SearchTool --> Search[(Azure AI Search<br/>多言語ナレッジ索引)]
  Gateway --> Blob[(Azure Storage<br/>通話成果物)]
  Blob --> Queue[(Azure Storage Queue<br/>post-call-jobs)]
  Queue --> Functions[Azure Functions (queue trigger)<br/>Azure Container Apps]
  Functions --> AnalyticsAgent[Foundry analytics agent/model]
  AnalyticsAgent --> Blob
  Functions --> CRM[(Dynamics 365 Customer Service<br/>チケット管理)]
  Blob --> Dashboard[Instructor console / Power BI]
```

## リアルタイム通話フロー

1. 顧客が ACS 番号、または ACS にルーティングされた番号へ発信します。
2. ACS が通話イベントを通話ゲートウェイへ送信します。
3. 通話ゲートウェイが Voice Live session を作成し、顧客会話を Foundry Agent へ接続します。
4. Foundry Agent は Foundry Models、Azure AI Search grounding、function tools を使って回答、エスカレーション判断、チケット作成を行います。
5. 通話ゲートウェイは通話成果物を Azure Storage に保存します。

## 通話後分析フロー

1. 完了した通話成果物は Azure Storage Queue にイベントとして送信されます。
2. Functions が Foundry analytics agent/model を呼び出し、マスキングを考慮した要約、意図、感情、エンティティ、解決状況、アクション項目を生成します。
3. 構造化された分析結果を Storage に保存し、講師コンソールまたは Power BI に表示します。

## サービス対応表

| 要件 | Azure サービス |
| --- | --- |
| 電話連携 | Azure Communication Services |
| リアルタイム音声対音声 | Voice Live API |
| 会話オーケストレーション | Azure AI Foundry Agent |
| モデルアクセス | Foundry Models |
| ナレッジ grounding | Foundry に接続された Azure AI Search tool |
| アプリホスティング | Azure Container Apps |
| 通話後ワークフロー | Azure Container Apps 上の Azure Functions (queue trigger) |
| チケット/ケース管理 | Dynamics 365 Customer Service |
| 通話成果物の保存 | Azure Storage |
| 監視 | Application Insights と Log Analytics |
| ID とアクセス | Managed identity、RBAC、Key Vault |

## 多言語サポート

- 英語、日本語、中国語の音声とモデル指示を設定します。
- 通話メタデータに言語コードを保存します。
- Azure AI Search に多言語ナレッジをインデックスします。
- 音声エージェントの挙動と通話後分析品質を言語別に評価します。
