using System;
using System.Collections.Generic;

namespace TradeFlow.Application.Common.Models.Settings;

public class BackupOptions
{
    public bool IncludeDatabase { get; set; } = true;
    public bool IncludeUploads { get; set; } = true;
    public bool IncludeAuditLogs { get; set; } = true;
    public string? CreatedBy { get; set; }
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
    public TradeFlowBackupManifest? Manifest { get; set; }
}

public class BackupMetadataDto
{
    public DateTime CreatedAt { get; set; }
    public string? FileName { get; set; }
    public long FileSizeBytes { get; set; }
    public string? CreatedBy { get; set; }
    public string? ScopeDescription { get; set; }
}

public class BackupFileEntry
{
    public string Path { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class TradeFlowBackupManifest
{
    public string FormatVersion { get; set; } = "tradeflow-backup-v2.0";
    public string Version => FormatVersion;
    public string AppName { get; set; } = "TradeFlow ERP";
    public string AppVersion { get; set; } = "1.0.0";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string BackupId { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public string DatabaseProvider { get; set; } = "PostgreSQL";
    public string DatabaseName { get; set; } = string.Empty;
    public Dictionary<string, int> EntityCounts { get; set; } = new();
    public int FileCount { get; set; }
    public long TotalFileSizeBytes { get; set; }
    public List<BackupFileEntry> Files { get; set; } = new();
    public Dictionary<string, string> PayloadChecksums { get; set; } = new();
    public List<string> SupportedImportModes { get; set; } = new() { "CleanRestore", "SelectiveMerge", "Overwrite" };
    public List<string> ExcludedSensitiveInfo { get; set; } = new() { "UserPasswordHashes", "RawPrivateSigningKeys", "DbConnectionStrings" };
    public List<string> IncludedComponents { get; set; } = new();
}

// Backward compatibility alias
public class BackupManifest : TradeFlowBackupManifest
{
    public DateTime BackupDate { get => CreatedAt; set => CreatedAt = value; }
    public int UploadedFilesCount { get => FileCount; set => FileCount = value; }
    public long TotalSizeBytes { get => TotalFileSizeBytes; set => TotalFileSizeBytes = value; }
    public string DatabaseFormat { get; set; } = "TradeFlow Portable JSON Payloads (v2.0)";
}

public enum ConflictResolutionAction
{
    KeepExisting = 1,     // Giữ dữ liệu hiện tại
    Overwrite = 2,        // Ghi đè bằng dữ liệu backup
    Replace = 3,          // Thay thế bản ghi trùng
    ImportAsNew = 4,      // Nhập thành bản ghi mới (sinh mã mới)
    Skip = 5              // Bỏ qua bản ghi backup này
}

public class BackupCategoryPreviewDto
{
    public string CategoryName { get; set; } = string.Empty;
    public List<string> EntityTypes { get; set; } = new();
    public int TotalInBackup { get; set; }
    public int NewCount { get; set; }
    public int MatchedCount { get; set; }
    public int ConflictCount { get; set; }
}

public class BackupRecordConflictDto
{
    public string EntityType { get; set; } = string.Empty;
    public string EntityDisplayName { get; set; } = string.Empty;
    public string BusinessKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string CurrentSummary { get; set; } = string.Empty;
    public string BackupSummary { get; set; } = string.Empty;
    public DateTime? CurrentModifiedAt { get; set; }
    public DateTime? BackupModifiedAt { get; set; }
    public bool IsExactMatch { get; set; }
    public ConflictResolutionAction SelectedAction { get; set; } = ConflictResolutionAction.KeepExisting;
}

public class BackupValidationResult
{
    public bool IsValid { get; set; }
    public string? StagingId { get; set; }
    public string? StagingPath { get; set; }
    public string? StagingDirectoryPath { get => StagingPath; set => StagingPath = value; }
    public TradeFlowBackupManifest? Manifest { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public int TotalRecordsInBackup { get; set; }
    public int TotalFilesInBackup { get; set; }
    public int TotalFiles => TotalFilesInBackup;
    public long TotalFileSizeBytes { get; set; }
    public List<BackupCategoryPreviewDto> Categories { get; set; } = new();
    public List<BackupRecordConflictDto> DetectedConflicts { get; set; } = new();
    public int FileDuplicatesCount { get; set; }
    public int FileConflictsCount { get; set; }
    public bool IsDatabaseEmpty => Categories.Sum(c => c.MatchedCount + c.ConflictCount) == 0;
    public ConflictResolutionAction FileConflictAction { get; set; } = ConflictResolutionAction.KeepExisting;
}

public class ConflictResolutionDecisionDto
{
    public string EntityType { get; set; } = string.Empty;
    public string BusinessKey { get; set; } = string.Empty;
    public ConflictResolutionAction Action { get; set; } = ConflictResolutionAction.KeepExisting;
}

public class ImportExecutionPlan
{
    public string StagingId { get; set; } = string.Empty;
    public string? StagingDirectoryPath { get; set; }
    public ConflictResolutionAction GlobalConflictPolicy { get; set; } = ConflictResolutionAction.KeepExisting;
    public Dictionary<string, ConflictResolutionAction> CategoryPolicies { get; set; } = new();
    public Dictionary<string, ConflictResolutionAction> RecordSpecificDecisions { get; set; } = new();
    public ConflictResolutionAction FileConflictPolicy { get; set; } = ConflictResolutionAction.KeepExisting;
    public ConflictResolutionAction FileConflictAction { get => FileConflictPolicy; set => FileConflictPolicy = value; }
    public bool ReplaceAllData { get; set; } = false;
    public BackupValidationResult? ValidationResult { get; set; }
    public List<ConflictResolutionDecisionDto> Decisions { get; set; } = new();
}

public class CategoryRestoreSummary
{
    public string CategoryName { get; set; } = string.Empty;
    public int Processed { get; set; }
    public int Imported { get; set; }
    public int Overwritten { get; set; }
    public int Retained { get; set; }
    public int Skipped { get; set; }
}

public class RestoreResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public long DurationMs { get; set; }
    public long ExecutionDurationMs { get => DurationMs; set => DurationMs = value; }
    public int TotalProcessed { get; set; }
    public int TotalRecordsProcessed { get => TotalProcessed; set => TotalProcessed = value; }
    public int NewRecordsImported { get; set; }
    public int ExistingRecordsRetained { get; set; }
    public int RecordsOverwritten { get; set; }
    public int RecordsReplaced { get; set; }
    public int RecordsSkipped { get; set; }
    public int FilesImported { get; set; }
    public int FilesRestored { get => FilesImported; set => FilesImported = value; }
    public int FilesRetained { get; set; }
    public int FilesSkipped { get; set; }
    public int AuditLogId { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, CategoryRestoreSummary> SummaryByCategory { get; set; } = new();
    public IEnumerable<CategoryRestoreSummary> CategorySummaries => SummaryByCategory.Values;
}
