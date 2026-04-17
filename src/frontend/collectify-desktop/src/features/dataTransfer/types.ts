export type BackupFile = {
  kind: string;
  fileName: string;
  sourcePath: string;
  backupPath: string;
  sizeBytes: number;
};

export type DataBackupResponse = {
  createdAt: string;
  backupDirectoryPath: string;
  files: BackupFile[];
  messages: string[];
};

export type DataImportResponse = {
  importedCollections: number;
  importedItems: number;
  importedCategories: number;
  importedTags: number;
  importedAt: string;
  messages: string[];
};
