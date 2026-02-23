IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [FirstName] nvarchar(100) NULL,
        [LastName] nvarchar(100) NULL,
        [IDVerified] bit NOT NULL DEFAULT CAST(0 AS bit),
        [VerificationTier] int NOT NULL,
        [IDDocumentUrl] nvarchar(max) NULL,
        [IDDocumentSubmittedAt] datetime2 NULL,
        [PhoneVerificationCode] nvarchar(max) NULL,
        [PhoneVerificationExpiry] datetime2 NULL,
        [EmailVerificationToken] nvarchar(max) NULL,
        [EmailVerificationExpiry] datetime2 NULL,
        [AverageRating] decimal(3,2) NULL,
        [LocationUrl] nvarchar(1000) NULL,
        [Location] nvarchar(500) NULL,
        [Latitude] decimal(10,8) NULL,
        [Longitude] decimal(11,8) NULL,
        [Country] nvarchar(max) NULL,
        [ProfilePictureUrl] nvarchar(500) NULL,
        [Bio] nvarchar(2000) NULL,
        [Skills] nvarchar(1000) NULL,
        [Role] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(450) NULL,
        [UpdatedBy] nvarchar(450) NULL,
        [CreatedBy] nvarchar(450) NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE TABLE [Bookings] (
        [Id] int NOT NULL IDENTITY,
        [AgreedRate] decimal(18,2) NOT NULL,
        [StartTime] datetime2 NULL,
        [EndTime] datetime2 NULL,
        [Status] nvarchar(450) NOT NULL,
        [TaskId] int NOT NULL,
        [WorkerId] nvarchar(450) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_Bookings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Bookings_AspNetUsers_WorkerId] FOREIGN KEY ([WorkerId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE TABLE [Tasks] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Category] nvarchar(50) NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [BudgetType] nvarchar(50) NOT NULL,
        [Budget] decimal(18,2) NOT NULL,
        [EstimatedHours] decimal(10,2) NULL,
        [DueDate] datetime2 NULL,
        [StartDate] datetime2 NULL,
        [Location] nvarchar(500) NULL,
        [LocationUrl] nvarchar(1000) NULL,
        [Latitude] decimal(10,8) NULL,
        [Longitude] decimal(11,8) NULL,
        [Country] nvarchar(100) NULL,
        [City] nvarchar(100) NULL,
        [IsRemote] bit NOT NULL,
        [WorkersNeeded] int NOT NULL,
        [RequiredSkills] nvarchar(1000) NULL,
        [AttachmentUrls] nvarchar(max) NULL,
        [PosterId] nvarchar(450) NOT NULL,
        [AssignedAt] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        [CancellationReason] nvarchar(500) NULL,
        [ViewCount] int NOT NULL,
        [IsFeatured] bit NOT NULL,
        [IsUrgent] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(max) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [CreatedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_Tasks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Tasks_AspNetUsers_PosterId] FOREIGN KEY ([PosterId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE TABLE [AppUserTaskItem] (
        [AssignedTasksId] int NOT NULL,
        [AssignedWorkerId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AppUserTaskItem] PRIMARY KEY ([AssignedTasksId], [AssignedWorkerId]),
        CONSTRAINT [FK_AppUserTaskItem_AspNetUsers_AssignedWorkerId] FOREIGN KEY ([AssignedWorkerId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AppUserTaskItem_Tasks_AssignedTasksId] FOREIGN KEY ([AssignedTasksId]) REFERENCES [Tasks] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE TABLE [TaskApplications] (
        [Id] int NOT NULL IDENTITY,
        [TaskItemId] int NOT NULL,
        [ProposedBudget] decimal(18,2) NOT NULL,
        [EstimatedHours] decimal(10,2) NULL,
        [Message] nvarchar(2000) NULL,
        [Status] nvarchar(50) NOT NULL,
        [ViewedAt] datetime2 NULL,
        [RejectionReason] nvarchar(500) NULL,
        [RespondedAt] datetime2 NULL,
        [WorkerId] nvarchar(450) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(max) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [CreatedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_TaskApplications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TaskApplications_AspNetUsers_WorkerId] FOREIGN KEY ([WorkerId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TaskApplications_Tasks_TaskItemId] FOREIGN KEY ([TaskItemId]) REFERENCES [Tasks] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AppUserTaskItem_AssignedWorkerId] ON [AppUserTaskItem] ([AssignedWorkerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUsers_CreatedAt] ON [AspNetUsers] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUsers_IDVerified] ON [AspNetUsers] ([IDVerified]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUsers_IsDeleted] ON [AspNetUsers] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Bookings_CreatedAt] ON [Bookings] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Bookings_Status] ON [Bookings] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Bookings_TaskId] ON [Bookings] ([TaskId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Bookings_WorkerId] ON [Bookings] ([WorkerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TaskApplications_Status] ON [TaskApplications] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TaskApplications_TaskItemId] ON [TaskApplications] ([TaskItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_TaskApplications_TaskItemId_WorkerId] ON [TaskApplications] ([TaskItemId], [WorkerId]) WHERE [WorkerId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TaskApplications_WorkerId] ON [TaskApplications] ([WorkerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tasks_Category] ON [Tasks] ([Category]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tasks_CreatedAt] ON [Tasks] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tasks_DueDate] ON [Tasks] ([DueDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tasks_IsFeatured] ON [Tasks] ([IsFeatured]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tasks_IsUrgent] ON [Tasks] ([IsUrgent]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tasks_Latitude_Longitude] ON [Tasks] ([Latitude], [Longitude]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tasks_PosterId] ON [Tasks] ([PosterId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tasks_Status] ON [Tasks] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tasks_Status_Category] ON [Tasks] ([Status], [Category]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218093826_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260218093826_InitialCreate', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218134410_mmmm'
)
BEGIN
    EXEC sp_rename N'[Bookings].[TaskId]', N'TaskItemId', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218134410_mmmm'
)
BEGIN
    EXEC sp_rename N'[Bookings].[IX_Bookings_TaskId]', N'IX_Bookings_TaskItemId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218134410_mmmm'
)
BEGIN
    ALTER TABLE [Bookings] ADD CONSTRAINT [FK_Bookings_Tasks_TaskItemId] FOREIGN KEY ([TaskItemId]) REFERENCES [Tasks] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218134410_mmmm'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260218134410_mmmm', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260219120437_addposterid'
)
BEGIN
    ALTER TABLE [Bookings] ADD [PosterId] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260219120437_addposterid'
)
BEGIN
    CREATE INDEX [IX_Bookings_PosterId] ON [Bookings] ([PosterId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260219120437_addposterid'
)
BEGIN
    ALTER TABLE [Bookings] ADD CONSTRAINT [FK_Bookings_AspNetUsers_PosterId] FOREIGN KEY ([PosterId]) REFERENCES [AspNetUsers] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260219120437_addposterid'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260219120437_addposterid', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260219135103_AddDisputeEntity'
)
BEGIN
    CREATE TABLE [Disputes] (
        [Id] int NOT NULL IDENTITY,
        [BookingId] int NOT NULL,
        [RaisedBy] nvarchar(450) NOT NULL,
        [Reason] nvarchar(max) NOT NULL,
        [Status] int NOT NULL,
        [Resolution] nvarchar(max) NULL,
        [ResolutionType] int NULL,
        [WorkerPercentage] int NULL,
        [ResolvedBy] nvarchar(450) NULL,
        [ResolvedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Disputes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Disputes_AspNetUsers_RaisedBy] FOREIGN KEY ([RaisedBy]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Disputes_AspNetUsers_ResolvedBy] FOREIGN KEY ([ResolvedBy]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Disputes_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260219135103_AddDisputeEntity'
)
BEGIN
    CREATE INDEX [IX_Disputes_BookingId] ON [Disputes] ([BookingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260219135103_AddDisputeEntity'
)
BEGIN
    CREATE INDEX [IX_Disputes_RaisedBy] ON [Disputes] ([RaisedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260219135103_AddDisputeEntity'
)
BEGIN
    CREATE INDEX [IX_Disputes_ResolvedBy] ON [Disputes] ([ResolvedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260219135103_AddDisputeEntity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260219135103_AddDisputeEntity', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260219150244_mmmmmmm'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260219150244_mmmmmmm', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260219150744_AddPosterIdToBooking'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260219150744_AddPosterIdToBooking', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220085309_edit_Status'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260220085309_edit_Status', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220135635_addRating'
)
BEGIN
    ALTER TABLE [Bookings] ADD [PosterComment] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220135635_addRating'
)
BEGIN
    ALTER TABLE [Bookings] ADD [PosterRating] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220135635_addRating'
)
BEGIN
    ALTER TABLE [Bookings] ADD [WorkerComment] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220135635_addRating'
)
BEGIN
    ALTER TABLE [Bookings] ADD [WorkerRating] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220135635_addRating'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260220135635_addRating', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220135704_addRatingg'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260220135704_addRatingg', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220141418_addRate'
)
BEGIN
    DECLARE @var sysname;
    SELECT @var = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Bookings]') AND [c].[name] = N'PosterComment');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Bookings] DROP CONSTRAINT [' + @var + '];');
    ALTER TABLE [Bookings] DROP COLUMN [PosterComment];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220141418_addRate'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Bookings]') AND [c].[name] = N'PosterRating');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Bookings] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [Bookings] DROP COLUMN [PosterRating];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220141418_addRate'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Bookings]') AND [c].[name] = N'WorkerComment');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Bookings] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [Bookings] DROP COLUMN [WorkerComment];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220141418_addRate'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Bookings]') AND [c].[name] = N'WorkerRating');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Bookings] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [Bookings] DROP COLUMN [WorkerRating];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220141418_addRate'
)
BEGIN
    CREATE TABLE [Rating] (
        [Id] int NOT NULL IDENTITY,
        [RaterId] nvarchar(450) NOT NULL,
        [RateeId] nvarchar(450) NOT NULL,
        [Score] int NOT NULL,
        [bookingId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Rating] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Rating_AspNetUsers_RateeId] FOREIGN KEY ([RateeId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Rating_AspNetUsers_RaterId] FOREIGN KEY ([RaterId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Rating_Bookings_bookingId] FOREIGN KEY ([bookingId]) REFERENCES [Bookings] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220141418_addRate'
)
BEGIN
    CREATE INDEX [IX_Rating_bookingId] ON [Rating] ([bookingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220141418_addRate'
)
BEGIN
    CREATE INDEX [IX_Rating_RateeId] ON [Rating] ([RateeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220141418_addRate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Rating_RaterId_RateeId_bookingId] ON [Rating] ([RaterId], [RateeId], [bookingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220141418_addRate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260220141418_addRate', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260221085725_payment'
)
BEGIN
    CREATE TABLE [Payment] (
        [Id] int NOT NULL IDENTITY,
        [BookingId] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [StripePaymentIntentId] nvarchar(255) NOT NULL,
        [Status] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ReleasedAt] datetime2 NULL,
        CONSTRAINT [PK_Payment] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payment_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260221085725_payment'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Payment_BookingId] ON [Payment] ([BookingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260221085725_payment'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260221085725_payment', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260221121432_payment2'
)
BEGIN
    ALTER TABLE [Payment] ADD [UpdatedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260221121432_payment2'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260221121432_payment2', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260222102808_add_prop_Hasvisa'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [HasVisa] bit NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260222102808_add_prop_Hasvisa'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260222102808_add_prop_Hasvisa', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260222142352_add_Entity_Message'
)
BEGIN
    CREATE TABLE [Message] (
        [Id] int NOT NULL IDENTITY,
        [Content] nvarchar(1000) NOT NULL,
        [bookingId] int NOT NULL,
        [SenderId] nvarchar(450) NOT NULL,
        [SentAt] datetime2 NOT NULL,
        [IsRead] bit NOT NULL,
        CONSTRAINT [PK_Message] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Message_AspNetUsers_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Message_Bookings_bookingId] FOREIGN KEY ([bookingId]) REFERENCES [Bookings] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260222142352_add_Entity_Message'
)
BEGIN
    CREATE INDEX [IX_Message_bookingId] ON [Message] ([bookingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260222142352_add_Entity_Message'
)
BEGIN
    CREATE INDEX [IX_Message_SenderId] ON [Message] ([SenderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260222142352_add_Entity_Message'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260222142352_add_Entity_Message', N'9.0.10');
END;

COMMIT;
GO

