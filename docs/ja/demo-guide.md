# デモガイド

## ローカルデモ

ローカルアプリは既定で mock モードで動作するため、Azure 資格情報なしでコンソールと API フローを確認できます。

```powershell
cd smart-call-center-quiz
dotnet run --project src/aspire/IntelligentCustomerOperations.AppHost
```

ターミナルに表示される Aspire dashboard の URL を開き、`gateway` エンドポイントにアクセスします。

## デモで見せること

1. 英語、日本語、中国語を選択します。
2. **Start simulated call** をクリックします。
3. 顧客と AI 音声エージェントの会話を見せます。
4. AI の引き継ぎ判断と CRM チケットを見せます。
5. 通話後分析を見せます: PII マスキング、要約、意図、感情、エンティティ、アクション項目、ダッシュボード指標。
6. 各デモパネルをアーキテクチャ図のコンポーネントに対応付けて説明します。

## 実際の Azure 実装との対応

| デモパネル | 実装 |
| --- | --- |
| リアルタイム文字起こし | Azure Communication Services と Voice Live API |
| ナレッジ回答 | Foundry Agent と Azure AI Search tool |
| 引き継ぎカード | Foundry function tool による CRM/コンタクトセンター API 呼び出し |
| 通話成果物の保存 | Azure Storage |
| 通話後フロー | Azure Container Apps 上の Azure Functions (Event Hubs trigger) |
| 要約と感情 | Foundry analytics agent/model |
| ダッシュボードカード | Instructor console または Power BI |

## 講師メモ

ローカル mock モードは授業で安全に使えるよう決定論的に作られています。デプロイ経路では ACS、Voice Live API、Foundry Agent/Models、Azure AI Search、Azure Storage、Azure Container Apps 上のイベント駆動 Azure Functions、CRM/tool API を使用します。詳細は `docs/deployment.md` を参照してください。

