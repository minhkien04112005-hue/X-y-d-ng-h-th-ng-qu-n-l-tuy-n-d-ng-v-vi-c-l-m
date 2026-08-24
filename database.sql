USE master;
GO

IF DB_ID('RecruitmentDB') IS NOT NULL
BEGIN
    ALTER DATABASE RecruitmentDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RecruitmentDB;
END
GO

CREATE DATABASE RecruitmentDB;
GO

USE RecruitmentDB;
GO

CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Email VARCHAR(150) NOT NULL,
    PasswordHash NVARCHAR(500) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);
GO

CREATE TABLE Jobs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    Deadline DATETIME NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    IsDeleted BIT NOT NULL DEFAULT 0
);
GO

CREATE TABLE Candidates (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Email VARCHAR(150) NOT NULL,
    Phone VARCHAR(20),
    IsDeleted BIT NOT NULL DEFAULT 0
);
GO

CREATE TABLE Applications (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    JobId INT NOT NULL,
    CandidateId INT NOT NULL,
    Status VARCHAR(20) NOT NULL,
    AppliedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL,

    CONSTRAINT FK_Applications_Jobs
        FOREIGN KEY (JobId) REFERENCES Jobs(Id),

    CONSTRAINT FK_Applications_Candidates
        FOREIGN KEY (CandidateId) REFERENCES Candidates(Id),

    CONSTRAINT UQ_Applications_Job_Candidate
        UNIQUE (JobId, CandidateId)
);
GO

-- Dữ liệu mẫu để sau khi đăng nhập có thể thấy ngay một vài tin tuyển dụng.
INSERT INTO Jobs (Title, Description, Deadline)
VALUES
    (N'.NET Backend Developer', N'Phát triển Web API và xử lý nghiệp vụ bằng ASP.NET Core.', DATEADD(DAY, 30, GETDATE())),
    (N'Frontend Developer', N'Xây dựng giao diện web thân thiện và responsive.', DATEADD(DAY, 45, GETDATE()));
GO
