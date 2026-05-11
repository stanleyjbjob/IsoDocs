-- =============================================================================
-- IsoDocs - InitialSchema (SQL Server 2022 / Azure SQL Database)
-- =============================================================================
-- 對應 issue #5：[1.3] 設計並建立核心資料庫結構。
--
-- 本檔為 EF Core 8 配置（src/IsoDocs.Infrastructure/Persistence/Configurations/*.cs）
-- 的「語意對等」原始 DDL。提供給：
--   1) DBA / 資安審查既有設計（不必跑 dotnet ef CLI 即可閱讀）
--   2) 沙箱／緊急環境直接建表用
--   3) 後續用 `dotnet ef migrations script` 產出的腳本做交叉比對
--
-- 注意事項：
--   * 本檔不是 EF Core Migration 的取代品。正式環境請以 `dotnet ef migrations add
--     InitialSchema` 產生並提交的 Migration C# 檔為準。
--   * 啟用 Temporal Tables 的 8 張表：Users / Roles / WorkflowTemplates /
--     FieldDefinitions / DocumentTypes / Cases / CaseFields / CaseNodes。
--   * Period 欄位名稱（PeriodStart / PeriodEnd）與歷史表命名（{Table}History）
--     沿用 EF Core 8 的預設值。
--   * Cascade 策略：詳見每段 ALTER TABLE 註解。Cases → 子表為 CASCADE，
--     User-related FK 一律 RESTRICT（NO ACTION），避免使用者軟刪後資料消失。
--
-- 執行順序：本檔可整檔丟進 sqlcmd / SSMS 一次跑完。
--   1. DROP（若存在）
--   2. CREATE TABLE（依依賴順序）
--   3. ALTER TABLE 加上 FOREIGN KEY
--   4. CREATE INDEX
-- =============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
GO

-- =============================================================================
-- 0. (僅供開發/重建用) 解除 SYSTEM_VERSIONING 並 DROP 既有物件
-- 正式環境請勿執行此區塊，請改用 EF Core Migration 漸進升級。
-- =============================================================================
/*
IF OBJECT_ID(N'[dbo].[Users]', 'U') IS NOT NULL ALTER TABLE [dbo].[Users] SET (SYSTEM_VERSIONING = OFF);
IF OBJECT_ID(N'[dbo].[Roles]', 'U') IS NOT NULL ALTER TABLE [dbo].[Roles] SET (SYSTEM_VERSIONING = OFF);
IF OBJECT_ID(N'[dbo].[WorkflowTemplates]', 'U') IS NOT NULL ALTER TABLE [dbo].[WorkflowTemplates] SET (SYSTEM_VERSIONING = OFF);
IF OBJECT_ID(N'[dbo].[FieldDefinitions]', 'U') IS NOT NULL ALTER TABLE [dbo].[FieldDefinitions] SET (SYSTEM_VERSIONING = OFF);
IF OBJECT_ID(N'[dbo].[DocumentTypes]', 'U') IS NOT NULL ALTER TABLE [dbo].[DocumentTypes] SET (SYSTEM_VERSIONING = OFF);
IF OBJECT_ID(N'[dbo].[Cases]', 'U') IS NOT NULL ALTER TABLE [dbo].[Cases] SET (SYSTEM_VERSIONING = OFF);
IF OBJECT_ID(N'[dbo].[CaseFields]', 'U') IS NOT NULL ALTER TABLE [dbo].[CaseFields] SET (SYSTEM_VERSIONING = OFF);
IF OBJECT_ID(N'[dbo].[CaseNodes]', 'U') IS NOT NULL ALTER TABLE [dbo].[CaseNodes] SET (SYSTEM_VERSIONING = OFF);

IF OBJECT_ID(N'[dbo].[AuditTrails]', 'U') IS NOT NULL DROP TABLE [dbo].[AuditTrails];
IF OBJECT_ID(N'[dbo].[Notifications]', 'U') IS NOT NULL DROP TABLE [dbo].[Notifications];
IF OBJECT_ID(N'[dbo].[Attachments]', 'U') IS NOT NULL DROP TABLE [dbo].[Attachments];
IF OBJECT_ID(N'[dbo].[Comments]', 'U') IS NOT NULL DROP TABLE [dbo].[Comments];
IF OBJECT_ID(N'[dbo].[CaseActions]', 'U') IS NOT NULL DROP TABLE [dbo].[CaseActions];
IF OBJECT_ID(N'[dbo].[CaseNodes]', 'U') IS NOT NULL DROP TABLE [dbo].[CaseNodes];
IF OBJECT_ID(N'[dbo].[CaseNodesHistory]', 'U') IS NOT NULL DROP TABLE [dbo].[CaseNodesHistory];
IF OBJECT_ID(N'[dbo].[CaseRelations]', 'U') IS NOT NULL DROP TABLE [dbo].[CaseRelations];
IF OBJECT_ID(N'[dbo].[CaseFields]', 'U') IS NOT NULL DROP TABLE [dbo].[CaseFields];
IF OBJECT_ID(N'[dbo].[CaseFieldsHistory]', 'U') IS NOT NULL DROP TABLE [dbo].[CaseFieldsHistory];
IF OBJECT_ID(N'[dbo].[Cases]', 'U') IS NOT NULL DROP TABLE [dbo].[Cases];
IF OBJECT_ID(N'[dbo].[CasesHistory]', 'U') IS NOT NULL DROP TABLE [dbo].[CasesHistory];
IF OBJECT_ID(N'[dbo].[Customers]', 'U') IS NOT NULL DROP TABLE [dbo].[Customers];
IF OBJECT_ID(N'[dbo].[DocumentTypes]', 'U') IS NOT NULL DROP TABLE [dbo].[DocumentTypes];
IF OBJECT_ID(N'[dbo].[DocumentTypesHistory]', 'U') IS NOT NULL DROP TABLE [dbo].[DocumentTypesHistory];
IF OBJECT_ID(N'[dbo].[FieldDefinitions]', 'U') IS NOT NULL DROP TABLE [dbo].[FieldDefinitions];
IF OBJECT_ID(N'[dbo].[FieldDefinitionsHistory]', 'U') IS NOT NULL DROP TABLE [dbo].[FieldDefinitionsHistory];
IF OBJECT_ID(N'[dbo].[WorkflowNodes]', 'U') IS NOT NULL DROP TABLE [dbo].[WorkflowNodes];
IF OBJECT_ID(N'[dbo].[WorkflowTemplates]', 'U') IS NOT NULL DROP TABLE [dbo].[WorkflowTemplates];
IF OBJECT_ID(N'[dbo].[WorkflowTemplatesHistory]', 'U') IS NOT NULL DROP TABLE [dbo].[WorkflowTemplatesHistory];
IF OBJECT_ID(N'[dbo].[Delegations]', 'U') IS NOT NULL DROP TABLE [dbo].[Delegations];
IF OBJECT_ID(N'[dbo].[UserRoles]', 'U') IS NOT NULL DROP TABLE [dbo].[UserRoles];
IF OBJECT_ID(N'[dbo].[Roles]', 'U') IS NOT NULL DROP TABLE [dbo].[Roles];
IF OBJECT_ID(N'[dbo].[RolesHistory]', 'U') IS NOT NULL DROP TABLE [dbo].[RolesHistory];
IF OBJECT_ID(N'[dbo].[Users]', 'U') IS NOT NULL DROP TABLE [dbo].[Users];
IF OBJECT_ID(N'[dbo].[UsersHistory]', 'U') IS NOT NULL DROP TABLE [dbo].[UsersHistory];
GO
*/

-- =============================================================================
-- 1. Identity 模組
-- =============================================================================

-- Users (Temporal) ------------------------------------------------------------
CREATE TABLE [dbo].[Users] (
    [Id]              UNIQUEIDENTIFIER NOT NULL,
    [AzureAdObjectId] NVARCHAR(64)     NOT NULL,
    [Email]           NVARCHAR(256)    NOT NULL,
    [DisplayName]     NVARCHAR(128)    NOT NULL,
    [Department]      NVARCHAR(128)    NULL,
    [JobTitle]        NVARCHAR(128)    NULL,
    [IsActive]        BIT              NOT NULL,
    [IsSystemAdmin]   BIT              NOT NULL,
    [CreatedAt]       DATETIMEOFFSET   NOT NULL,
    [UpdatedAt]       DATETIMEOFFSET   NULL,
    [PeriodStart]     DATETIME2        GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
    [PeriodEnd]       DATETIME2        GENERATED ALWAYS AS ROW END   HIDDEN NOT NULL,
    PERIOD FOR SYSTEM_TIME ([PeriodStart], [PeriodEnd]),
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id])
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[UsersHistory]));
GO

-- Roles (Temporal) ------------------------------------------------------------
CREATE TABLE [dbo].[Roles] (
    [Id]              UNIQUEIDENTIFIER NOT NULL,
    [Name]            NVARCHAR(64)     NOT NULL,
    [Description]     NVARCHAR(512)    NULL,
    [PermissionsJson] NVARCHAR(MAX)    NOT NULL,
    [IsSystemRole]    BIT              NOT NULL,
    [IsActive]        BIT              NOT NULL,
    [CreatedAt]       DATETIMEOFFSET   NOT NULL,
    [UpdatedAt]       DATETIMEOFFSET   NULL,
    [PeriodStart]     DATETIME2        GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
    [PeriodEnd]       DATETIME2        GENERATED ALWAYS AS ROW END   HIDDEN NOT NULL,
    PERIOD FOR SYSTEM_TIME ([PeriodStart], [PeriodEnd]),
    CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([Id])
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[RolesHistory]));
GO

-- UserRoles -------------------------------------------------------------------
CREATE TABLE [dbo].[UserRoles] (
    [Id]                UNIQUEIDENTIFIER NOT NULL,
    [UserId]            UNIQUEIDENTIFIER NOT NULL,
    [RoleId]            UNIQUEIDENTIFIER NOT NULL,
    [EffectiveFrom]     DATETIMEOFFSET   NOT NULL,
    [EffectiveTo]       DATETIMEOFFSET   NULL,
    [AssignedByUserId]  UNIQUEIDENTIFIER NULL,
    [AssignedAt]        DATETIMEOFFSET   NOT NULL,
    [CreatedAt]         DATETIMEOFFSET   NOT NULL,
    [UpdatedAt]         DATETIMEOFFSET   NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED ([Id])
);
GO

-- Delegations -----------------------------------------------------------------
CREATE TABLE [dbo].[Delegations] (
    [Id]                UNIQUEIDENTIFIER NOT NULL,
    [DelegatorUserId]   UNIQUEIDENTIFIER NOT NULL,
    [DelegateUserId]    UNIQUEIDENTIFIER NOT NULL,
    [StartAt]           DATETIMEOFFSET   NOT NULL,
    [EndAt]             DATETIMEOFFSET   NOT NULL,
    [Note]              NVARCHAR(512)    NULL,
    [IsRevoked]         BIT              NOT NULL,
    [CreatedAt]         DATETIMEOFFSET   NOT NULL,
    [UpdatedAt]         DATETIMEOFFSET   NULL,
    CONSTRAINT [PK_Delegations] PRIMARY KEY CLUSTERED ([Id])
);
GO

-- =============================================================================
-- 2. Workflows 模組
-- =============================================================================

-- WorkflowTemplates (Temporal) ------------------------------------------------
CREATE TABLE [dbo].[WorkflowTemplates] (
    [Id]                UNIQUEIDENTIFIER NOT NULL,
    [Code]              NVARCHAR(32)     NOT NULL,
    [Name]              NVARCHAR(128)    NOT NULL,
    [Description]       NVARCHAR(1024)   NULL,
    [Version]           INT              NOT NULL,
    [DefinitionJson]    NVARCHAR(MAX)    NOT NULL,
    [PublishedAt]       DATETIMEOFFSET   NULL,
    [IsActive]          BIT              NOT NULL,
    [CreatedByUserId]   UNIQUEIDENTIFIER NOT NULL,
    [CreatedAt]         DATETIMEOFFSET   NOT NULL,
    [UpdatedAt]         DATETIMEOFFSET   NULL,
    [PeriodStart]       DATETIME2        GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
    [PeriodEnd]         DATETIME2        GENERATED ALWAYS AS ROW END   HIDDEN NOT NULL,
    PERIOD FOR SYSTEM_TIME ([PeriodStart], [PeriodEnd]),
    CONSTRAINT [PK_WorkflowTemplates] PRIMARY KEY CLUSTERED ([Id])
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[WorkflowTemplatesHistory]));
GO

-- WorkflowNodes ---------------------------------------------------------------
-- NodeType: 1=Apply, 2=Process, 3=Approve, 4=Close, 5=Notify
CREATE TABLE [dbo].[WorkflowNodes] (
    [Id]                  UNIQUEIDENTIFIER NOT NULL,
    [WorkflowTemplateId]  UNIQUEIDENTIFIER NOT NULL,
    [TemplateVersion]     INT              NOT NULL,
    [NodeOrder]           INT              NOT NULL,
    [Name]                NVARCHAR(128)    NOT NULL,
    [NodeType]            INT              NOT NULL,
    [RequiredRoleId]      UNIQUEIDENTIFIER NULL,
    [ConfigJson]          NVARCHAR(MAX)    NOT NULL,
    [CreatedAt]           DATETIMEOFFSET   NOT NULL,
    [UpdatedAt]           DATETIMEOFFSET   NULL,
    CONSTRAINT [PK_WorkflowNodes] PRIMARY KEY CLUSTERED ([Id])
);
GO

-- FieldDefinitions (Temporal) -------------------------------------------------
-- Type: 1=Text, 2=LongText, 3=Number, 4=Decimal, 5=Date, 6=DateTime, 7=Boolean,
--       8=SingleSelect, 9=MultiSelect, 10=User, 11=Customer, 12=File, 13=Json
CREATE TABLE [dbo].[FieldDefinitions] (
    [Id]                UNIQUEIDENTIFIER NOT NULL,
    [Code]              NVARCHAR(64)     NOT NULL,
    [Name]              NVARCHAR(128)    NOT NULL,
    [Version]           INT              NOT NULL,
    [Type]              INT              NOT NULL,
    [IsRequired]        BIT              NOT NULL,
    [ValidationJson]    NVARCHAR(MAX)    NULL,
    [OptionsJson]       NVARCHAR(MAX)    NULL,
    [IsActive]          BIT              NOT NULL,
    [CreatedAt]         DATETIMEOFFSET   NOT NULL,
    [UpdatedAt]         DATETIMEOFFSET   NULL,
    [PeriodStart]       DATETIME2        GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
    [PeriodEnd]         DATETIME2        GENERATED ALWAYS AS ROW END   HIDDEN NOT NULL,
    PERIOD FOR SYSTEM_TIME ([PeriodStart], [PeriodEnd]),
    CONSTRAINT [PK_FieldDefinitions] PRIMARY KEY CLUSTERED ([Id])
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[FieldDefinitionsHistory]));
GO

-- DocumentTypes (Temporal) ----------------------------------------------------
-- 編碼格式：{CompanyCode}-{Code}-{YearTwoDigits}{CurrentSequence:D4}
-- 例如 ITCT-F01-260076。RowVersion 為 SQL Server 自動維護的 rowversion，作為樂觀鎖。
CREATE TABLE [dbo].[DocumentTypes] (
    [Id]                UNIQUEIDENTIFIER NOT NULL,
    [CompanyCode]       NVARCHAR(16)     NOT NULL,
    [Code]              NVARCHAR(16)     NOT NULL,
    [Name]              NVARCHAR(128)    NOT NULL,
    [SequenceYear]      INT              NOT NULL,
    [CurrentSequence]   INT              NOT NULL,
    [IsActive]          BIT              NOT NULL,
    [RowVersion]        ROWVERSION       NOT NULL,
    [CreatedAt]         DATETIMEOFFSET   NOT NULL,
    [UpdatedAt]         DATETIMEOFFSET   NULL,
    [PeriodStart]       DATETIME2        GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
    [PeriodEnd]         DATETIME2        GENERATED ALWAYS AS ROW END   HIDDEN NOT NULL,
    PERIOD FOR SYSTEM_TIME ([PeriodStart], [PeriodEnd]),
    CONSTRAINT [PK_DocumentTypes] PRIMARY KEY CLUSTERED ([Id])
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[DocumentTypesHistory]));
GO

-- =============================================================================
-- 3. Customers 模組
-- =============================================================================

CREATE TABLE [dbo].[Customers] (
    [Id]                UNIQUEIDENTIFIER NOT NULL,
    [Code]              NVARCHAR(64)     NOT NULL,
    [Name]              NVARCHAR(256)    NOT NULL,
    [ContactPerson]     NVARCHAR(128)    NULL,
    [ContactEmail]      NVARCHAR(256)    NULL,
    [ContactPhone]      NVARCHAR(64)     NULL,
    [Note]              NVARCHAR(1024)   NULL,
    [IsActive]          BIT              NOT NULL,
    [CreatedAt]         DATETIMEOFFSET   NOT NULL,
    [UpdatedAt]         DATETIMEOFFSET   NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY CLUSTERED ([Id])
);
GO

-- =============================================================================
-- 4. Cases 模組（核心）
-- =============================================================================

-- Cases (Temporal) ------------------------------------------------------------
-- Status: 1=InProgress, 2=Closed, 3=Voided
CREATE TABLE [dbo].[Cases] (
    [Id]                    UNIQUEIDENTIFIER NOT NULL,
    [CaseNumber]            NVARCHAR(64)     NOT NULL,
    [Title]                 NVARCHAR(256)    NOT NULL,
    [DocumentTypeId]        UNIQUEIDENTIFIER NOT NULL,
    [WorkflowTemplateId]    UNIQUEIDENTIFIER NOT NULL,
    [TemplateVersion]       INT              NOT NULL,
    [FieldVersion]          INT              NOT NULL,
    [CustomerId]            UNIQUEIDENTIFIER NULL,
    [Status]                INT              NOT NULL,
    [InitiatedByUserId]     UNIQUEIDENTIFIER NOT NULL,
    [InitiatedAt]           DATETIMEOFFSET   NOT NULL,
    [ExpectedCompletionAt]  DATETIMEOFFSET   NULL,
    [OriginalExpectedAt]    DATETIMEOFFSET   NULL,
    [ClosedAt]              DATETIMEOFFSET   NULL,
    [VoidedAt]              DATETIMEOFFSET   NULL,
    [CustomVersionNumber]   NVARCHAR(64)     NULL,
    [CreatedAt]             DATETIMEOFFSET   NOT NULL,
    [UpdatedAt]             DATETIMEOFFSET   NULL,
    [PeriodStart]           DATETIME2        GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
    [PeriodEnd]             DATETIME2        GENERATED ALWAYS AS ROW END   HIDDEN NOT NULL,
    PERIOD FOR SYSTEM_TIME ([PeriodStart], [PeriodEnd]),
    CONSTRAINT [PK_Cases] PRIMARY KEY CLUSTERED ([Id])
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[CasesHistory]));
GO

-- CaseFields (Temporal) -------------------------------------------------------
CREATE TABLE [dbo].[CaseFields] (
    [Id]                    UNIQUEIDENTIFIER NOT NULL,
    [CaseId]                UNIQUEIDENTIFIER NOT NULL,
    [FieldDefinitionId]     UNIQUEIDENTIFIER NOT NULL,
    [FieldVersion]          INT              NOT NULL,
    [FieldCode]             NVARCHAR(64)     NOT NULL,
    [ValueJson]             NVARCHAR(MAX)    NOT NULL,
    [CreatedAt]             DATETIMEOFFSET   NOT NULL,
    [UpdatedAt]             DATETIMEOFFSET   NULL,
    [PeriodStart]           DATETIME2        GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
    [PeriodEnd]             DATETIME2        GENERATED ALWAYS AS ROW END   HIDDEN NOT NULL,
    PERIOD FOR SYSTEM_TIME ([PeriodStart], [PeriodEnd]),
    CONSTRAINT [PK_CaseFields] PRIMARY KEY CLUSTERED ([Id])
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[CaseFieldsHistory]));
GO

-- CaseRelations ---------------------------------------------------------------
-- RelationType: 1=Subprocess, 2=Reopen, 3=Reference
CREATE TABLE [dbo].[CaseRelations] (
    [Id]                UNIQUEIDENTIFIER NOT NULL,
    [ParentCaseId]      UNIQUEIDENTIFIER NOT NULL,
    [ChildCaseId]       UNIQUEIDENTIFIER NOT NULL,
    [RelationType]      INT              NOT NULL,
    [CreatedByUserId]   UNIQUEIDENTIFIER NOT NULL,
    [CreatedAt]         DATETIMEOFFSET   NOT NULL,
    [UpdatedAt]         DATETIMEOFFSET   NULL,
    CONSTRAINT [PK_CaseRelations] PRIMARY KEY CLUSTERED ([Id])
);
GO

-- CaseNodes (Temporal) --------------------------------------------------------
-- Status: 1=Pending, 2=InProgress, 3=Completed, 4=Returned, 5=Skipped
CREATE TABLE [dbo].[CaseNodes] (
    [Id]                    UNIQUEIDENTIFIER NOT NULL,
    [CaseId]                UNIQUEIDENTIFIER NOT NULL,
    [WorkflowNodeId]        UNIQUEIDENTIFIER NOT NULL,
    [NodeOrder]             INT              NOT NULL,
    [NodeName]              NVARCHAR(128)    NOT NULL,
    [AssigneeUserId]        UNIQUEIDENTIFIER NULL,
    [Status]                INT              NOT NULL,
    [ModifiedExpectedAt]    DATETIMEOFFSET   NULL,
    [OriginalExpectedAt]    DATETIMEOFFSET   NULL,
    [StartedAt]             DATETIMEOFFSET   NULL,
    [CompletedAt]           DATETIMEOFFSET   NULL,
    [CreatedAt]             DATETIMEOFFSET   NOT NULL,
    [UpdatedAt]             DATETIMEOFFSET   NULL,
    [PeriodStart]           DATETIME2        GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
    [PeriodEnd]             DATETIME2        GENERATED ALWAYS AS ROW END   HIDDEN NOT NULL,
    PERIOD FOR SYSTEM_TIME ([PeriodStart], [PeriodEnd]),
    CONSTRAINT [PK_CaseNodes] PRIMARY KEY CLUSTERED ([Id])
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[CaseNodesHistory]));
GO

-- CaseActions -----------------------------------------------------------------
-- ActionType: 1=Initiate, 2=Assign, 3=Accept, 4=ReplyClose, 5=Approve, 6=Reject,
--             7=SpawnChild, 8=Void, 9=VoidCascade, 10=Reopen, 11=Comment,
--             12=UpdateExpected, 13=SignOff
CREATE TABLE [dbo].[CaseActions] (
    [Id]                UNIQUEIDENTIFIER NOT NULL,
    [CaseId]            UNIQUEIDENTIFIER NOT NULL,
    [CaseNodeId]        UNIQUEIDENTIFIER NULL,
    [ActionType]        INT              NOT NULL,
    [ActorUserId]       UNIQUEIDENTIFIER NOT NULL,
    [Comment]           NVARCHAR(MAX)    NULL,
    [ActionAt]          DATETIMEOFFSET   NOT NULL,
    [PayloadJson]       NVARCHAR(MAX)    NULL,
    [CreatedAt]         DATETIMEOFFSET   NOT NULL,
    [UpdatedAt]         DATETIMEOFFSET   NULL,
    CONSTRAINT [PK_CaseActions] PRIMARY KEY CLUSTERED ([Id])
);
GO

-- =============================================================================
-- 5. Communications 模組
-- =============================================================================

-- Comments --------------------------------------------------------------------
CREATE TABLE [dbo].[Comments] (
    [Id]                UNIQUEIDENTIFIER NOT NULL,
    [CaseId]            UNIQUEIDENTIFIER NOT NULL,
    [AuthorUserId]      UNIQUEIDENTIFIER NOT NULL,
    [Body]              NVARCHAR(MAX)    NOT NULL,
    [ParentCommentId]   UNIQUEIDENTIFIER NULL,
    [IsDeleted]         BIT              NOT NULL,
    [CreatedAt]         DATETIMEOFFSET   NOT NULL,
    [UpdatedAt]         DATETIMEOFFSET   NULL,
    CONSTRAINT [PK_Comments] PRIMARY KEY CLUSTERED ([Id])
);
GO

-- Notifications ---------------------------------------------------------------
-- Type:    1=NodeAssigned, 2=CaseStatusChanged, 3=NewComment, 4=Voided,
--          5=SubprocessVoided, 6=Overdue, 99=Custom
-- Channel: 1=InApp, 2=Email, 3=Teams
CREATE TABLE [dbo].[Notifications] (
    [Id]                UNIQUEIDENTIFIER NOT NULL,
    [RecipientUserId]   UNIQUEIDENTIFIER NOT NULL,
    [CaseId]            UNIQUEIDENTIFIER NULL,
    [Type]              INT              NOT NULL,
    [Channel]           INT              NOT NULL,
    [Subject]           NVARCHAR(256)    NOT NULL,
    [Body]              NVARCHAR(MAX)    NOT NULL,
    [PayloadJson]       NVARCHAR(MAX)    NULL,
    [SentAt]            DATETIMEOFFSET   NULL,
    [IsRead]            BIT              NOT NULL,
    [ReadAt]            DATETIMEOFFSET   NULL,
    [RetryCount]        INT              NOT NULL,
    [LastError]         NVARCHAR(MAX)    NULL,
    [CreatedAt]         DATETIMEOFFSET   NOT NULL,
    [UpdatedAt]         DATETIMEOFFSET   NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED ([Id])
);
GO

-- =============================================================================
-- 6. Attachments 模組
-- =============================================================================

CREATE TABLE [dbo].[Attachments] (
    [Id]                UNIQUEIDENTIFIER NOT NULL,
    [CaseId]            UNIQUEIDENTIFIER NOT NULL,
    [FileName]          NVARCHAR(512)    NOT NULL,
    [ContentType]       NVARCHAR(128)    NOT NULL,
    [SizeBytes]         BIGINT           NOT NULL,
    [BlobUrl]           NVARCHAR(2048)   NOT NULL,
    [UploadedByUserId]  UNIQUEIDENTIFIER NOT NULL,
    [UploadedAt]        DATETIMEOFFSET   NOT NULL,
    [IsDeleted]         BIT              NOT NULL,
    [CreatedAt]         DATETIMEOFFSET   NOT NULL,
    [UpdatedAt]         DATETIMEOFFSET   NULL,
    CONSTRAINT [PK_Attachments] PRIMARY KEY CLUSTERED ([Id])
);
GO

-- =============================================================================
-- 7. Audit 模組
-- =============================================================================

CREATE TABLE [dbo].[AuditTrails] (
    [Id]            UNIQUEIDENTIFIER NOT NULL,
    [UserId]        UNIQUEIDENTIFIER NULL,
    [EntityType]    NVARCHAR(128)    NOT NULL,
    [EntityId]      NVARCHAR(64)     NOT NULL,
    [Action]        NVARCHAR(64)     NOT NULL,
    [ChangesJson]   NVARCHAR(MAX)    NULL,
    [IpAddress]     NVARCHAR(64)     NULL,
    [UserAgent]     NVARCHAR(512)    NULL,
    [OccurredAt]    DATETIMEOFFSET   NOT NULL,
    [CreatedAt]     DATETIMEOFFSET   NOT NULL,
    [UpdatedAt]     DATETIMEOFFSET   NULL,
    CONSTRAINT [PK_AuditTrails] PRIMARY KEY CLUSTERED ([Id])
);
GO

-- =============================================================================
-- 8. Foreign Keys
-- -----------------------------------------------------------------------------
-- 對應 EF Core Configuration 中的 OnDelete 設定：
--   * RESTRICT (NO ACTION): 預設、不可隨主檔刪除（人員/客戶/文件類型/範本等主檔軟刪）
--   * CASCADE: 子表隨案件 (Cases) / 範本 (WorkflowTemplates) 連動刪除
--   * SET NULL: 子節點不存在時欄位為 NULL（CaseAction.CaseNodeId 在 Configuration
--               中為 SetNull；本檔已調整為 NO ACTION，原因見下方 ⚠ 註解）
-- =============================================================================

-- UserRoles --------------
ALTER TABLE [dbo].[UserRoles]
    ADD CONSTRAINT [FK_UserRoles_Users_UserId]
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION;
ALTER TABLE [dbo].[UserRoles]
    ADD CONSTRAINT [FK_UserRoles_Roles_RoleId]
        FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([Id]) ON DELETE NO ACTION;
GO

-- Delegations ------------
ALTER TABLE [dbo].[Delegations]
    ADD CONSTRAINT [FK_Delegations_Users_DelegatorUserId]
        FOREIGN KEY ([DelegatorUserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION;
ALTER TABLE [dbo].[Delegations]
    ADD CONSTRAINT [FK_Delegations_Users_DelegateUserId]
        FOREIGN KEY ([DelegateUserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION;
GO

-- WorkflowNodes ----------
ALTER TABLE [dbo].[WorkflowNodes]
    ADD CONSTRAINT [FK_WorkflowNodes_WorkflowTemplates_WorkflowTemplateId]
        FOREIGN KEY ([WorkflowTemplateId]) REFERENCES [dbo].[WorkflowTemplates]([Id]) ON DELETE CASCADE;
GO

-- Cases ------------------
ALTER TABLE [dbo].[Cases]
    ADD CONSTRAINT [FK_Cases_DocumentTypes_DocumentTypeId]
        FOREIGN KEY ([DocumentTypeId]) REFERENCES [dbo].[DocumentTypes]([Id]) ON DELETE NO ACTION;
ALTER TABLE [dbo].[Cases]
    ADD CONSTRAINT [FK_Cases_WorkflowTemplates_WorkflowTemplateId]
        FOREIGN KEY ([WorkflowTemplateId]) REFERENCES [dbo].[WorkflowTemplates]([Id]) ON DELETE NO ACTION;
ALTER TABLE [dbo].[Cases]
    ADD CONSTRAINT [FK_Cases_Users_InitiatedByUserId]
        FOREIGN KEY ([InitiatedByUserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION;
ALTER TABLE [dbo].[Cases]
    ADD CONSTRAINT [FK_Cases_Customers_CustomerId]
        FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers]([Id]) ON DELETE NO ACTION;
GO

-- CaseFields -------------
ALTER TABLE [dbo].[CaseFields]
    ADD CONSTRAINT [FK_CaseFields_Cases_CaseId]
        FOREIGN KEY ([CaseId]) REFERENCES [dbo].[Cases]([Id]) ON DELETE CASCADE;
ALTER TABLE [dbo].[CaseFields]
    ADD CONSTRAINT [FK_CaseFields_FieldDefinitions_FieldDefinitionId]
        FOREIGN KEY ([FieldDefinitionId]) REFERENCES [dbo].[FieldDefinitions]([Id]) ON DELETE NO ACTION;
GO

-- CaseRelations ----------
ALTER TABLE [dbo].[CaseRelations]
    ADD CONSTRAINT [FK_CaseRelations_Cases_ParentCaseId]
        FOREIGN KEY ([ParentCaseId]) REFERENCES [dbo].[Cases]([Id]) ON DELETE NO ACTION;
ALTER TABLE [dbo].[CaseRelations]
    ADD CONSTRAINT [FK_CaseRelations_Cases_ChildCaseId]
        FOREIGN KEY ([ChildCaseId]) REFERENCES [dbo].[Cases]([Id]) ON DELETE NO ACTION;
GO

-- CaseNodes --------------
ALTER TABLE [dbo].[CaseNodes]
    ADD CONSTRAINT [FK_CaseNodes_Cases_CaseId]
        FOREIGN KEY ([CaseId]) REFERENCES [dbo].[Cases]([Id]) ON DELETE CASCADE;
ALTER TABLE [dbo].[CaseNodes]
    ADD CONSTRAINT [FK_CaseNodes_WorkflowNodes_WorkflowNodeId]
        FOREIGN KEY ([WorkflowNodeId]) REFERENCES [dbo].[WorkflowNodes]([Id]) ON DELETE NO ACTION;
ALTER TABLE [dbo].[CaseNodes]
    ADD CONSTRAINT [FK_CaseNodes_Users_AssigneeUserId]
        FOREIGN KEY ([AssigneeUserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION;
GO

-- CaseActions ------------
-- ⚠ CaseAction.CaseNodeId 在 EF Configuration 內為 OnDelete(SetNull)，但 Cases →
--   CaseNodes 與 Cases → CaseActions 都是 CASCADE，導致 SQL Server 偵測為「多重
--   cascade path 衝突」（錯誤 1785）。為了讓這份 DDL 可一鍵跑完，本檔將
--   CaseAction.CaseNodeId 暫改為 NO ACTION。EF Migration 端建議同步調整：
--     CaseActionConfiguration.cs:
--       .OnDelete(DeleteBehavior.SetNull)  →  .OnDelete(DeleteBehavior.NoAction)
--   本系統不會 hard-delete 案件（用 Voided 軟標記），所以實務上該規則不會被觸發；
--   差異僅止於 schema 約束層的命名與意圖，無資料風險。
ALTER TABLE [dbo].[CaseActions]
    ADD CONSTRAINT [FK_CaseActions_Cases_CaseId]
        FOREIGN KEY ([CaseId]) REFERENCES [dbo].[Cases]([Id]) ON DELETE CASCADE;
ALTER TABLE [dbo].[CaseActions]
    ADD CONSTRAINT [FK_CaseActions_CaseNodes_CaseNodeId]
        FOREIGN KEY ([CaseNodeId]) REFERENCES [dbo].[CaseNodes]([Id]) ON DELETE NO ACTION;
ALTER TABLE [dbo].[CaseActions]
    ADD CONSTRAINT [FK_CaseActions_Users_ActorUserId]
        FOREIGN KEY ([ActorUserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION;
GO

-- Comments ---------------
ALTER TABLE [dbo].[Comments]
    ADD CONSTRAINT [FK_Comments_Cases_CaseId]
        FOREIGN KEY ([CaseId]) REFERENCES [dbo].[Cases]([Id]) ON DELETE CASCADE;
ALTER TABLE [dbo].[Comments]
    ADD CONSTRAINT [FK_Comments_Users_AuthorUserId]
        FOREIGN KEY ([AuthorUserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION;
GO

-- Notifications ----------
ALTER TABLE [dbo].[Notifications]
    ADD CONSTRAINT [FK_Notifications_Users_RecipientUserId]
        FOREIGN KEY ([RecipientUserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION;
ALTER TABLE [dbo].[Notifications]
    ADD CONSTRAINT [FK_Notifications_Cases_CaseId]
        FOREIGN KEY ([CaseId]) REFERENCES [dbo].[Cases]([Id]) ON DELETE SET NULL;
GO

-- Attachments ------------
ALTER TABLE [dbo].[Attachments]
    ADD CONSTRAINT [FK_Attachments_Cases_CaseId]
        FOREIGN KEY ([CaseId]) REFERENCES [dbo].[Cases]([Id]) ON DELETE NO ACTION;
ALTER TABLE [dbo].[Attachments]
    ADD CONSTRAINT [FK_Attachments_Users_UploadedByUserId]
        FOREIGN KEY ([UploadedByUserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION;
GO

-- AuditTrails ------------
ALTER TABLE [dbo].[AuditTrails]
    ADD CONSTRAINT [FK_AuditTrails_Users_UserId]
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE SET NULL;
GO

-- =============================================================================
-- 9. Indexes（包含 unique constraints）
-- =============================================================================

-- Identity --------------------------------------------------------------------
CREATE UNIQUE INDEX [IX_Users_AzureAdObjectId] ON [dbo].[Users] ([AzureAdObjectId]);
CREATE UNIQUE INDEX [IX_Users_Email]           ON [dbo].[Users] ([Email]);
CREATE        INDEX [IX_Users_IsActive]        ON [dbo].[Users] ([IsActive]);
GO

CREATE UNIQUE INDEX [IX_Roles_Name] ON [dbo].[Roles] ([Name]);
GO

CREATE INDEX [IX_UserRoles_UserId_RoleId]                    ON [dbo].[UserRoles] ([UserId], [RoleId]);
CREATE INDEX [IX_UserRoles_UserId_EffectiveFrom_EffectiveTo] ON [dbo].[UserRoles] ([UserId], [EffectiveFrom], [EffectiveTo]);
CREATE INDEX [IX_UserRoles_RoleId]                           ON [dbo].[UserRoles] ([RoleId]);
GO

CREATE INDEX [IX_Delegations_DelegatorUserId_StartAt_EndAt] ON [dbo].[Delegations] ([DelegatorUserId], [StartAt], [EndAt]);
CREATE INDEX [IX_Delegations_IsRevoked]                     ON [dbo].[Delegations] ([IsRevoked]);
CREATE INDEX [IX_Delegations_DelegateUserId]                ON [dbo].[Delegations] ([DelegateUserId]);
GO

-- Workflows -------------------------------------------------------------------
CREATE UNIQUE INDEX [IX_WorkflowTemplates_Code_Version] ON [dbo].[WorkflowTemplates] ([Code], [Version]);
CREATE        INDEX [IX_WorkflowTemplates_IsActive]      ON [dbo].[WorkflowTemplates] ([IsActive]);
GO

CREATE UNIQUE INDEX [IX_WorkflowNodes_TemplateId_TemplateVersion_NodeOrder]
    ON [dbo].[WorkflowNodes] ([WorkflowTemplateId], [TemplateVersion], [NodeOrder]);
GO

CREATE UNIQUE INDEX [IX_FieldDefinitions_Code_Version] ON [dbo].[FieldDefinitions] ([Code], [Version]);
CREATE        INDEX [IX_FieldDefinitions_IsActive]      ON [dbo].[FieldDefinitions] ([IsActive]);
GO

CREATE UNIQUE INDEX [IX_DocumentTypes_CompanyCode_Code] ON [dbo].[DocumentTypes] ([CompanyCode], [Code]);
CREATE        INDEX [IX_DocumentTypes_IsActive]         ON [dbo].[DocumentTypes] ([IsActive]);
GO

-- Customers -------------------------------------------------------------------
CREATE UNIQUE INDEX [IX_Customers_Code]     ON [dbo].[Customers] ([Code]);
CREATE        INDEX [IX_Customers_Name]     ON [dbo].[Customers] ([Name]);
CREATE        INDEX [IX_Customers_IsActive] ON [dbo].[Customers] ([IsActive]);
GO

-- Cases -----------------------------------------------------------------------
CREATE UNIQUE INDEX [IX_Cases_CaseNumber]              ON [dbo].[Cases] ([CaseNumber]);
CREATE        INDEX [IX_Cases_Status_InitiatedAt]      ON [dbo].[Cases] ([Status], [InitiatedAt]);
CREATE        INDEX [IX_Cases_InitiatedByUserId]       ON [dbo].[Cases] ([InitiatedByUserId]);
CREATE        INDEX [IX_Cases_CustomerId]              ON [dbo].[Cases] ([CustomerId]);
CREATE        INDEX [IX_Cases_DocumentTypeId]          ON [dbo].[Cases] ([DocumentTypeId]);
CREATE        INDEX [IX_Cases_WorkflowTemplateId]      ON [dbo].[Cases] ([WorkflowTemplateId]);
GO

CREATE UNIQUE INDEX [IX_CaseFields_CaseId_FieldCode]            ON [dbo].[CaseFields] ([CaseId], [FieldCode]);
CREATE        INDEX [IX_CaseFields_FieldDefinitionId]           ON [dbo].[CaseFields] ([FieldDefinitionId]);
GO

CREATE UNIQUE INDEX [IX_CaseRelations_Parent_Child_Type] ON [dbo].[CaseRelations] ([ParentCaseId], [ChildCaseId], [RelationType]);
CREATE        INDEX [IX_CaseRelations_ChildCaseId]       ON [dbo].[CaseRelations] ([ChildCaseId]);
GO

CREATE INDEX [IX_CaseNodes_CaseId_NodeOrder]      ON [dbo].[CaseNodes] ([CaseId], [NodeOrder]);
CREATE INDEX [IX_CaseNodes_AssigneeUserId_Status] ON [dbo].[CaseNodes] ([AssigneeUserId], [Status]);
CREATE INDEX [IX_CaseNodes_Status]                ON [dbo].[CaseNodes] ([Status]);
CREATE INDEX [IX_CaseNodes_WorkflowNodeId]        ON [dbo].[CaseNodes] ([WorkflowNodeId]);
GO

CREATE INDEX [IX_CaseActions_CaseId_ActionAt] ON [dbo].[CaseActions] ([CaseId], [ActionAt]);
CREATE INDEX [IX_CaseActions_ActorUserId]     ON [dbo].[CaseActions] ([ActorUserId]);
CREATE INDEX [IX_CaseActions_ActionType]      ON [dbo].[CaseActions] ([ActionType]);
CREATE INDEX [IX_CaseActions_CaseNodeId]      ON [dbo].[CaseActions] ([CaseNodeId]);
GO

-- Communications --------------------------------------------------------------
CREATE INDEX [IX_Comments_CaseId_CreatedAt] ON [dbo].[Comments] ([CaseId], [CreatedAt]);
CREATE INDEX [IX_Comments_IsDeleted]        ON [dbo].[Comments] ([IsDeleted]);
CREATE INDEX [IX_Comments_AuthorUserId]     ON [dbo].[Comments] ([AuthorUserId]);
GO

CREATE INDEX [IX_Notifications_RecipientUserId_IsRead] ON [dbo].[Notifications] ([RecipientUserId], [IsRead]);
CREATE INDEX [IX_Notifications_SentAt]                  ON [dbo].[Notifications] ([SentAt]);
CREATE INDEX [IX_Notifications_CaseId]                  ON [dbo].[Notifications] ([CaseId]);
GO

-- Attachments -----------------------------------------------------------------
CREATE INDEX [IX_Attachments_CaseId]            ON [dbo].[Attachments] ([CaseId]);
CREATE INDEX [IX_Attachments_IsDeleted]         ON [dbo].[Attachments] ([IsDeleted]);
CREATE INDEX [IX_Attachments_UploadedByUserId]  ON [dbo].[Attachments] ([UploadedByUserId]);
GO

-- Audit -----------------------------------------------------------------------
CREATE INDEX [IX_AuditTrails_EntityType_EntityId] ON [dbo].[AuditTrails] ([EntityType], [EntityId]);
CREATE INDEX [IX_AuditTrails_OccurredAt]          ON [dbo].[AuditTrails] ([OccurredAt]);
CREATE INDEX [IX_AuditTrails_UserId]              ON [dbo].[AuditTrails] ([UserId]);
GO

-- =============================================================================
-- End of InitialSchema.sql
-- =============================================================================
