import { useState } from "react";
import { collectifyClient } from "../../shared/api/collectifyClient";

type DataTransferState = "idle" | "running" | "success" | "error";

type DataTransferPanelProps = {
  onDataImported: () => Promise<void>;
  onStatusMessage: (message: string) => void;
};

export function DataTransferPanel({ onDataImported, onStatusMessage }: DataTransferPanelProps) {
  const [state, setState] = useState<DataTransferState>("idle");
  const [message, setMessage] = useState("Backup, export e import lavorano solo sui file JSON locali.");
  const isRunning = state === "running";

  async function runAction(action: () => Promise<string>, successStatus: string) {
    setState("running");

    try {
      const nextMessage = await action();
      setState("success");
      setMessage(nextMessage);
      onStatusMessage(successStatus);
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : "Operazione dati non riuscita.";
      setState("error");
      setMessage(errorMessage);
      onStatusMessage(errorMessage);
    }
  }

  async function handleCreateBackup() {
    setMessage("Creazione backup in corso...");
    await runAction(async () => {
      const result = await collectifyClient.createBackup();
      return result.files.length > 0
        ? `Backup creato con ${result.files.length} file in ${result.backupDirectoryPath}.`
        : result.messages.join(" ") || "Backup completato, ma non sono stati copiati file.";
    }, "Backup locale creato.");
  }

  async function handleExportData() {
    setMessage("Preparazione export JSON...");
    await runAction(async () => {
      const fileName = await collectifyClient.exportData();
      return `Export creato: ${fileName}.`;
    }, "Export JSON pronto.");
  }

  async function handleImportData(files: FileList | null) {
    const file = files?.[0];
    if (!file) {
      return;
    }

    setMessage("Validazione e import del file JSON...");
    await runAction(async () => {
      const result = await collectifyClient.importData(file);
      await onDataImported();
      return `Import completato: ${result.importedCollections} collezioni e ${result.importedItems} elementi aggiunti. ${result.messages.join(" ")}`;
    }, "Import JSON completato.");
  }

  return (
    <section className="data-transfer-panel" aria-label="Backup export import">
      <div className="data-transfer-panel__header">
        <div>
          <span>Portabilita' dati</span>
          <strong>Backup, export e import JSON</strong>
        </div>
      </div>
      <div className="data-transfer-actions">
        <button className="secondary-action" disabled={isRunning} type="button" onClick={() => void handleCreateBackup()}>
          Crea backup
        </button>
        <button className="secondary-action" disabled={isRunning} type="button" onClick={() => void handleExportData()}>
          Esporta JSON
        </button>
        <label className={isRunning ? "import-action is-disabled" : "import-action"}>
          Importa JSON
          <input
            accept="application/json,.json"
            disabled={isRunning}
            type="file"
            onChange={(event) => {
              void handleImportData(event.currentTarget.files);
              event.currentTarget.value = "";
            }}
          />
        </label>
      </div>
      <p className={`data-transfer-message data-transfer-message--${state}`}>{message}</p>
    </section>
  );
}
