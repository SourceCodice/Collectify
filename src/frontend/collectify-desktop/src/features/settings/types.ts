export type AppSettings = {
  dataRootPath: string;
  dataFilePath: string;
  imagesPath: string;
  backupsPath: string;
  settingsFilePath: string;
  theme: string;
  automaticBackupEnabled: boolean;
  language: string;
  locale: string;
  createdAt: string;
  updatedAt: string;
};

export type UpdateAppSettingsPayload = {
  dataRootPath?: string;
  theme?: string;
  automaticBackupEnabled?: boolean;
  language?: string;
};
