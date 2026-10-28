IF OBJECT_ID(N'dbo.JourneyProducts', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.JourneyModules', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.JourneyLessons', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Cohorts', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.CohortSessions', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.JourneyOrders', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.JourneyEnrollments', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.SessionBookings', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.ClientJourneyUploads', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.NotificationOutbox', N'U') IS NOT NULL
BEGIN
    PRINT N'Client journey operations schema already exists. No changes were required.';
    RETURN;
END;

IF OBJECT_ID(N'dbo.JourneyProducts', N'U') IS NOT NULL
   OR OBJECT_ID(N'dbo.JourneyModules', N'U') IS NOT NULL
   OR OBJECT_ID(N'dbo.JourneyLessons', N'U') IS NOT NULL
   OR OBJECT_ID(N'dbo.Cohorts', N'U') IS NOT NULL
   OR OBJECT_ID(N'dbo.CohortSessions', N'U') IS NOT NULL
   OR OBJECT_ID(N'dbo.JourneyOrders', N'U') IS NOT NULL
   OR OBJECT_ID(N'dbo.JourneyEnrollments', N'U') IS NOT NULL
   OR OBJECT_ID(N'dbo.SessionBookings', N'U') IS NOT NULL
   OR OBJECT_ID(N'dbo.ClientJourneyUploads', N'U') IS NOT NULL
   OR OBJECT_ID(N'dbo.NotificationOutbox', N'U') IS NOT NULL
BEGIN
    THROW 50001, 'The client journey schema is only partially installed. Review the existing tables before rerunning migration 007.', 1;
END;

CREATE TABLE dbo.JourneyProducts
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_JourneyProducts PRIMARY KEY DEFAULT NEWID(),
    Slug NVARCHAR(120) NOT NULL CONSTRAINT UQ_JourneyProducts_Slug UNIQUE,
    ProductType NVARCHAR(30) NOT NULL,
    TitleEn NVARCHAR(240) NOT NULL,
    TitleAr NVARCHAR(240) NOT NULL,
    SummaryEn NVARCHAR(1000) NOT NULL,
    SummaryAr NVARCHAR(1000) NOT NULL,
    Price DECIMAL(12,2) NOT NULL,
    Currency NCHAR(3) NOT NULL CONSTRAINT DF_JourneyProducts_Currency DEFAULT N'USD',
    DurationEn NVARCHAR(120) NOT NULL,
    DurationAr NVARCHAR(120) NOT NULL,
    CoverAssetKey NVARCHAR(120) NULL,
    ParentProductId UNIQUEIDENTIFIER NULL,
    IsBundle BIT NOT NULL CONSTRAINT DF_JourneyProducts_IsBundle DEFAULT 0,
    IsActive BIT NOT NULL CONSTRAINT DF_JourneyProducts_IsActive DEFAULT 1,
    SortOrder INT NOT NULL CONSTRAINT DF_JourneyProducts_SortOrder DEFAULT 0,
    CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_JourneyProducts_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_JourneyProducts_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_JourneyProducts_Parent FOREIGN KEY (ParentProductId) REFERENCES dbo.JourneyProducts(Id),
    CONSTRAINT CK_JourneyProducts_Type CHECK (ProductType IN (N'cohort', N'coaching', N'library', N'course')),
    CONSTRAINT CK_JourneyProducts_Price CHECK (Price >= 0)
);

CREATE TABLE dbo.JourneyModules
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_JourneyModules PRIMARY KEY DEFAULT NEWID(),
    ProductId UNIQUEIDENTIFIER NOT NULL,
    ModuleKey NVARCHAR(120) NOT NULL,
    TitleEn NVARCHAR(240) NOT NULL,
    TitleAr NVARCHAR(240) NOT NULL,
    DescriptionEn NVARCHAR(1000) NOT NULL,
    DescriptionAr NVARCHAR(1000) NOT NULL,
    SortOrder INT NOT NULL,
    UnlockDayOffset INT NOT NULL CONSTRAINT DF_JourneyModules_UnlockDayOffset DEFAULT 0,
    CONSTRAINT FK_JourneyModules_Product FOREIGN KEY (ProductId) REFERENCES dbo.JourneyProducts(Id),
    CONSTRAINT UQ_JourneyModules_Product_Key UNIQUE (ProductId, ModuleKey)
);

CREATE TABLE dbo.JourneyLessons
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_JourneyLessons PRIMARY KEY DEFAULT NEWID(),
    ModuleId UNIQUEIDENTIFIER NOT NULL,
    LessonKey NVARCHAR(120) NOT NULL,
    LessonType NVARCHAR(30) NOT NULL,
    TitleEn NVARCHAR(240) NOT NULL,
    TitleAr NVARCHAR(240) NOT NULL,
    ContentUrl NVARCHAR(1000) NULL,
    DownloadAssetKey NVARCHAR(120) NULL,
    DurationMinutes INT NOT NULL CONSTRAINT DF_JourneyLessons_Duration DEFAULT 0,
    SortOrder INT NOT NULL,
    IsPreview BIT NOT NULL CONSTRAINT DF_JourneyLessons_IsPreview DEFAULT 0,
    CONSTRAINT FK_JourneyLessons_Module FOREIGN KEY (ModuleId) REFERENCES dbo.JourneyModules(Id),
    CONSTRAINT UQ_JourneyLessons_Module_Key UNIQUE (ModuleId, LessonKey),
    CONSTRAINT CK_JourneyLessons_Type CHECK (LessonType IN (N'video', N'assessment', N'booking', N'download', N'live-session', N'upload'))
);

CREATE TABLE dbo.Cohorts
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Cohorts PRIMARY KEY DEFAULT NEWID(),
    ProductId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(240) NOT NULL,
    StartsAtUtc DATETIME2 NOT NULL,
    EndsAtUtc DATETIME2 NOT NULL,
    Capacity INT NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Cohorts_Status DEFAULT N'draft',
    CommunityType NVARCHAR(20) NULL,
    CommunityUrl NVARCHAR(1000) NULL,
    CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_Cohorts_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Cohorts_Product FOREIGN KEY (ProductId) REFERENCES dbo.JourneyProducts(Id),
    CONSTRAINT CK_Cohorts_Status CHECK (Status IN (N'draft', N'open', N'active', N'completed', N'cancelled')),
    CONSTRAINT CK_Cohorts_CommunityType CHECK (CommunityType IS NULL OR CommunityType IN (N'WhatsApp', N'Discord')),
    CONSTRAINT CK_Cohorts_Dates CHECK (EndsAtUtc > StartsAtUtc)
);

CREATE TABLE dbo.CohortSessions
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CohortSessions PRIMARY KEY DEFAULT NEWID(),
    CohortId UNIQUEIDENTIFIER NULL,
    SessionType NVARCHAR(40) NOT NULL,
    Focus NVARCHAR(120) NULL,
    TitleEn NVARCHAR(240) NOT NULL,
    TitleAr NVARCHAR(240) NOT NULL,
    StartsAtUtc DATETIME2 NOT NULL,
    EndsAtUtc DATETIME2 NOT NULL,
    Capacity INT NULL,
    MeetingUrl NVARCHAR(1000) NULL,
    ExternalCalendarEventId NVARCHAR(500) NULL,
    IsBookable BIT NOT NULL CONSTRAINT DF_CohortSessions_IsBookable DEFAULT 0,
    CONSTRAINT FK_CohortSessions_Cohort FOREIGN KEY (CohortId) REFERENCES dbo.Cohorts(Id),
    CONSTRAINT CK_CohortSessions_Dates CHECK (EndsAtUtc > StartsAtUtc)
);

CREATE TABLE dbo.JourneyOrders
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_JourneyOrders PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    ProductId UNIQUEIDENTIFIER NOT NULL,
    Amount DECIMAL(12,2) NOT NULL,
    Currency NCHAR(3) NOT NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_JourneyOrders_Status DEFAULT N'pending',
    PaymentProvider NVARCHAR(60) NULL,
    ProviderReference NVARCHAR(300) NULL,
    CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_JourneyOrders_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    PaidAtUtc DATETIME2 NULL,
    CONSTRAINT FK_JourneyOrders_User FOREIGN KEY (UserId) REFERENCES dbo.ClientAccounts(Id),
    CONSTRAINT FK_JourneyOrders_Product FOREIGN KEY (ProductId) REFERENCES dbo.JourneyProducts(Id),
    CONSTRAINT CK_JourneyOrders_Status CHECK (Status IN (N'pending', N'paid', N'failed', N'refunded', N'cancelled'))
);

CREATE TABLE dbo.JourneyEnrollments
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_JourneyEnrollments PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    ProductId UNIQUEIDENTIFIER NOT NULL,
    CohortId UNIQUEIDENTIFIER NULL,
    OrderId UNIQUEIDENTIFIER NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_JourneyEnrollments_Status DEFAULT N'active',
    EnrolledAtUtc DATETIME2 NOT NULL CONSTRAINT DF_JourneyEnrollments_EnrolledAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_JourneyEnrollments_User FOREIGN KEY (UserId) REFERENCES dbo.ClientAccounts(Id),
    CONSTRAINT FK_JourneyEnrollments_Product FOREIGN KEY (ProductId) REFERENCES dbo.JourneyProducts(Id),
    CONSTRAINT FK_JourneyEnrollments_Cohort FOREIGN KEY (CohortId) REFERENCES dbo.Cohorts(Id),
    CONSTRAINT FK_JourneyEnrollments_Order FOREIGN KEY (OrderId) REFERENCES dbo.JourneyOrders(Id),
    CONSTRAINT UQ_JourneyEnrollments_User_Product_Cohort UNIQUE (UserId, ProductId, CohortId)
);

CREATE TABLE dbo.SessionBookings
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SessionBookings PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    SessionId UNIQUEIDENTIFIER NULL,
    Focus NVARCHAR(120) NOT NULL,
    StartsAtUtc DATETIME2 NOT NULL,
    EndsAtUtc DATETIME2 NOT NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_SessionBookings_Status DEFAULT N'confirmed',
    MeetingUrl NVARCHAR(1000) NULL,
    ExternalCalendarEventId NVARCHAR(500) NULL,
    CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SessionBookings_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_SessionBookings_User FOREIGN KEY (UserId) REFERENCES dbo.ClientAccounts(Id),
    CONSTRAINT FK_SessionBookings_Session FOREIGN KEY (SessionId) REFERENCES dbo.CohortSessions(Id),
    CONSTRAINT CK_SessionBookings_Dates CHECK (EndsAtUtc > StartsAtUtc)
);

CREATE TABLE dbo.ClientJourneyUploads
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ClientJourneyUploads PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    EnrollmentId UNIQUEIDENTIFIER NULL,
    UploadType NVARCHAR(50) NOT NULL,
    AssetKey NVARCHAR(120) NOT NULL,
    OriginalFileName NVARCHAR(255) NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_ClientJourneyUploads_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ClientJourneyUploads_User FOREIGN KEY (UserId) REFERENCES dbo.ClientAccounts(Id),
    CONSTRAINT FK_ClientJourneyUploads_Enrollment FOREIGN KEY (EnrollmentId) REFERENCES dbo.JourneyEnrollments(Id),
    CONSTRAINT FK_ClientJourneyUploads_Asset FOREIGN KEY (AssetKey) REFERENCES dbo.Assets(AssetKey)
);

CREATE TABLE dbo.NotificationOutbox
(
    Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NotificationOutbox PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    Channel NVARCHAR(30) NOT NULL,
    TemplateKey NVARCHAR(120) NOT NULL,
    PayloadJson NVARCHAR(MAX) NOT NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_NotificationOutbox_Status DEFAULT N'pending',
    ProviderReference NVARCHAR(500) NULL,
    Attempts INT NOT NULL CONSTRAINT DF_NotificationOutbox_Attempts DEFAULT 0,
    CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_NotificationOutbox_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    SentAtUtc DATETIME2 NULL,
    LastError NVARCHAR(2000) NULL,
    CONSTRAINT FK_NotificationOutbox_User FOREIGN KEY (UserId) REFERENCES dbo.ClientAccounts(Id),
    CONSTRAINT CK_NotificationOutbox_Channel CHECK (Channel IN (N'email', N'calendar', N'whatsapp', N'discord')),
    CONSTRAINT CK_NotificationOutbox_PayloadJson CHECK (ISJSON(PayloadJson) = 1)
);

CREATE INDEX IX_JourneyProducts_Active_Sort ON dbo.JourneyProducts(IsActive, SortOrder);
CREATE INDEX IX_CohortSessions_Cohort_Start ON dbo.CohortSessions(CohortId, StartsAtUtc);
CREATE INDEX IX_JourneyOrders_User_Created ON dbo.JourneyOrders(UserId, CreatedAtUtc DESC);
CREATE INDEX IX_JourneyEnrollments_User_Status ON dbo.JourneyEnrollments(UserId, Status);
CREATE INDEX IX_SessionBookings_User_Start ON dbo.SessionBookings(UserId, StartsAtUtc);
CREATE INDEX IX_NotificationOutbox_Status_Created ON dbo.NotificationOutbox(Status, CreatedAtUtc);
