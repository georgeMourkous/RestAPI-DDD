dotnet ef --versionIF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
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
    WHERE [MigrationId] = N'20260429215759_InitialCreate'
)
BEGIN
    CREATE TABLE [PackageCategory] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(255) NOT NULL,
        [SortOrder] int NOT NULL,
        [Visible] bit NOT NULL,
        CONSTRAINT [PK_PackageCategory] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429215759_InitialCreate'
)
BEGIN
    CREATE TABLE [Service] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(255) NOT NULL,
        [Description] nvarchar(2000) NULL,
        CONSTRAINT [PK_Service] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429215759_InitialCreate'
)
BEGIN
    CREATE TABLE [Package] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(255) NOT NULL,
        [PackageCategoryId] int NOT NULL,
        [Description] nvarchar(2000) NULL,
        [Created] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [Start] datetime2 NULL,
        [Expire] datetime2 NULL,
        [IsQuantityAllowed] bit NOT NULL,
        CONSTRAINT [PK_Package] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Package_PackageCategory_PackageCategoryId] FOREIGN KEY ([PackageCategoryId]) REFERENCES [PackageCategory] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429215759_InitialCreate'
)
BEGIN
    CREATE TABLE [PackageFrequency] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(255) NOT NULL,
        [Frequency] int NOT NULL,
        [PackageId] int NOT NULL,
        [IsActive] bit NOT NULL,
        [Created] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_PackageFrequency] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PackageFrequency_Package_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [Package] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429215759_InitialCreate'
)
BEGIN
    CREATE TABLE [PackageService] (
        [Id] int NOT NULL IDENTITY,
        [PackageId] int NOT NULL,
        [ServiceId] int NOT NULL,
        [DefaultInstances] int NOT NULL,
        [MinimumInstances] int NOT NULL,
        [MaximumInstances] int NULL,
        CONSTRAINT [PK_PackageService] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PackageService_Package_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [Package] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PackageService_Service_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Service] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429215759_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name', N'SortOrder', N'Visible') AND [object_id] = OBJECT_ID(N'[PackageCategory]'))
        SET IDENTITY_INSERT [PackageCategory] ON;
    EXEC(N'INSERT INTO [PackageCategory] ([Id], [Name], [SortOrder], [Visible])
    VALUES (1, N''Default'', 1, CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name', N'SortOrder', N'Visible') AND [object_id] = OBJECT_ID(N'[PackageCategory]'))
        SET IDENTITY_INSERT [PackageCategory] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429215759_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'Name') AND [object_id] = OBJECT_ID(N'[Service]'))
        SET IDENTITY_INSERT [Service] ON;
    EXEC(N'INSERT INTO [Service] ([Id], [Description], [Name])
    VALUES (1, N''Default seeded service value.'', N''Core Service'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'Name') AND [object_id] = OBJECT_ID(N'[Service]'))
        SET IDENTITY_INSERT [Service] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429215759_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Package_Name] ON [Package] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429215759_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Package_PackageCategoryId] ON [Package] ([PackageCategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429215759_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PackageCategory_Name] ON [PackageCategory] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429215759_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PackageFrequency_PackageId_Name] ON [PackageFrequency] ([PackageId], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429215759_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PackageService_PackageId_ServiceId] ON [PackageService] ([PackageId], [ServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429215759_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PackageService_ServiceId] ON [PackageService] ([ServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429215759_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Service_Name] ON [Service] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429215759_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429215759_InitialCreate', N'10.0.7');
END;

COMMIT;
GO

