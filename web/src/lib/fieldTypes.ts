/**
 * 自訂欄位類型目錄（前端常數）。
 *
 * 對應後端 #7 [3.1.1] 的 `FieldDefinition.FieldType`。後端只儲存類型 key 字串，UI 標籤、icon、config 欄位由前端維護。
 *
 * **新增類型流程**：
 * 1. 在 `FieldType` 加上 union literal
 * 2. 在 `FIELD_TYPES` 加 `FieldTypeDef`
 * 3. 後端 #7 不需改：`FieldType` 為 string 欄位、`Config` 為 JSON 欄位
 */

/**
 * 支援的欄位類型 union。
 *
 * 命名規則：全小寫、底線分隔。對應 `FieldDefinition.FieldType`（後端儲存）。
 */
export type FieldType =
  | 'text'
  | 'textarea'
  | 'number'
  | 'date'
  | 'datetime'
  | 'boolean'
  | 'select'
  | 'multiselect'
  | 'user';

/**
 * 一個欄位類型的描述。
 *
 * `configKeys` 列出該類型可用的 `FieldConfig` 屬性 key，UI 在編輯 drawer 中只顯示對應欄位。
 */
export interface FieldTypeDef {
  key: FieldType;
  label: string;
  /** 簡短說明（給 UI tooltip 用） */
  description: string;
  /**
   * 該類型支援的 config 屬性 key 子集。
   * UI 編輯時依此清單顯示對應的設定欄位（例如 select 顯示 options 編輯器、number 顯示 min/max）。
   */
  configKeys: ReadonlyArray<keyof FieldConfig>;
}

/**
 * 欄位 config（型別特定）。
 *
 * 對應後端 `FieldDefinition.Config` JSON 欄位。所有屬性皆 optional，依 `fieldType` 決定哪些有效。
 */
export interface FieldConfig {
  /** select / multiselect：可選項清單 */
  options?: ReadonlyArray<{ value: string; label: string }>;
  /** number：最小值 */
  min?: number;
  /** number：最大值 */
  max?: number;
  /** number：小數位數（預設 0） */
  precision?: number;
  /** textarea：顯示列數（預設 4） */
  rows?: number;
  /** text：最大長度 */
  maxLength?: number;
  /** date / datetime：日期格式提示（例如 'YYYY-MM-DD'） */
  format?: string;
  /** select / multiselect：是否允許使用者輸入新項（自由文字） */
  allowCustom?: boolean;
  /** user：是否限定特定角色（roleIds） */
  restrictedToRoleIds?: ReadonlyArray<string>;
}

/**
 * 欄位類型 catalog。issue 推進時可逐步擴充。
 *
 * UI 使用方式：
 * - 編輯 drawer 的「欄位類型」select 列舉這份 catalog
 * - 切換類型時依 configKeys 顯示對應的 config 子表單
 * - 清單頁的型別欄顯示 label
 */
export const FIELD_TYPES: ReadonlyArray<FieldTypeDef> = [
  {
    key: 'text',
    label: '單行文字',
    description: '單行短文字輸入。可設定最大長度。',
    configKeys: ['maxLength'],
  },
  {
    key: 'textarea',
    label: '多行文字',
    description: '多行文字輸入框（textarea）。可設定顯示列數。',
    configKeys: ['rows', 'maxLength'],
  },
  {
    key: 'number',
    label: '數字',
    description: '整數或小數。可設定上下限與精度。',
    configKeys: ['min', 'max', 'precision'],
  },
  {
    key: 'date',
    label: '日期',
    description: '僅日期（無時間）。',
    configKeys: ['format'],
  },
  {
    key: 'datetime',
    label: '日期時間',
    description: '日期含時間。',
    configKeys: ['format'],
  },
  {
    key: 'boolean',
    label: '是／否',
    description: '布林值，UI 顯示為開關或勾選框。',
    configKeys: [],
  },
  {
    key: 'select',
    label: '單選下拉',
    description: '從預先定義的選項中選一個。',
    configKeys: ['options', 'allowCustom'],
  },
  {
    key: 'multiselect',
    label: '多選下拉',
    description: '從預先定義的選項中選多個（複選）。',
    configKeys: ['options', 'allowCustom'],
  },
  {
    key: 'user',
    label: '使用者',
    description: '從系統使用者清單中選一位。可限定特定角色。',
    configKeys: ['restrictedToRoleIds'],
  },
];

/** 把欄位類型 key 解析回 FieldTypeDef（找不到回 null） */
export function findFieldType(key: string): FieldTypeDef | null {
  return FIELD_TYPES.find((t) => t.key === key) ?? null;
}
