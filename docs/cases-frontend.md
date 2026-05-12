# 案件前端設計 (issue #21 [5.5])

## 1. 目標

為「工作需求單」提供完整的全生命週期前端介面：發起、處理、簽核、作廢、重開、與衍生子流程。與 issue #11 的動態欄位、#13 的流程範本設計器、#8 的 RBAC 連動。

## 2. 檔案組織

```
web/src/api/
  cases.ts                    # API client + 型別
  mockCases.ts                # dev mock interceptor (in-memory state machine)
web/src/components/
  DynamicFieldRenderer.tsx    # 依 9 種 fieldType 切換 antd input/readonly view
web/src/pages/cases/
  CaseListPage.tsx            # 案件清單頁
  CaseCreatePage.tsx          # 發起表單
  CaseDetailPage.tsx          # 詳情頁（節點、欄位、軌跡、關聯 Tab）
  CaseActionButtons.tsx       # 動作按鈕區（含 6 個內嵌 modal）
```

## 3. 驗收條件對照

| 驗收條件 | 實作位置 |
| --- | --- |
| 案件發起表單（動態欄位） | `CaseCreatePage.tsx` + `DynamicFieldRenderer.tsx` |
| 案件詳情頁（節點進度、欄位、動作按鈕） | `CaseDetailPage.tsx` + Steps + Tabs + `CaseActionButtons.tsx` |
| 接單/指派/核准/退回/作廢 | `CaseActionButtons.tsx` |
| 預計完成時間設定與修改 | `CaseCreatePage` (首設) + `UpdateExpectedModal` |
| 權限控管按鈕顯示 | `useHasPermission` + 案件內部狀態檢查 |
| 衍生子流程與關聯顯示 | `SpawnChildModal` + `CaseDetailPage` Relations Tab |

## 4. Mock 狀態機

`mockCases.ts` 實作完整的 in-memory state machine，讓前端不依賴後端即可走所有 flow。
切換到真實後端：`web/.env.development` 設 `VITE_USE_MOCK_CASES=false`。
