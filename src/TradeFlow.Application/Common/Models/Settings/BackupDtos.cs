using System;
using System.Collections.Generic;

namespace TradeFlow.Application.Common.Models.Settings;

public class BackupOptions
{
    public bool IncludeDatabase { get; set; } = true;
    public bool IncludeUploads { get; set; } = true;
}

public class BackupResult
{
    public bool Succeeded { get; set; }
    public string? BackupId { get; set; }
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public long FileSizeBytes { get; set; }
    public string? ContentType { get; set; } = "application/zip";
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public BackupManifest? Manifest { get; set; }
}

public class BackupMetadataDto
{
    public DateTime CreatedAt { get; set; }
    public string? FileName { get; set; }
    public long FileSizeBytes { get; set; }
    public string? CreatedBy { get; set; }
    public string? ScopeDescription { get; set; }
}

public class BackupManifest
{
    public string AppName { get; set; } = "TradeFlow ERP";
    public string AppVersion { get; set; } = "1.0.0";
    public DateTime BackupDate { get; set; } = DateTime.UtcNow;
    public string DatabaseName { get; set; } = string.Empty;
    public string DatabaseFormat { get; set; } = "PostgreSQL Custom Archive (pg_dump -Fc)";
    public int UploadedFilesCount { get; set; }
    public long TotalSizeBytes { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public List<string> IncludedComponents { get; set; } = new();
}
