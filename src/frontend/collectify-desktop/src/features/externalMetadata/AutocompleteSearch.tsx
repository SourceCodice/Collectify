import { useEffect, useRef, useState } from "react";
import { collectifyClient } from "../../shared/api/collectifyClient";
import type { LiveMetadataDetails, LiveMetadataSearchResult } from "./types";
import { useExternalMetadataSearch } from "./useExternalMetadataSearch";

type AutocompleteSearchProps = {
  itemType: string;
  value: string;
  disabled?: boolean;
  placeholder?: string;
  onValueChange: (value: string) => void;
  onSelect: (details: LiveMetadataDetails) => void;
  onStatusMessage?: (message: string) => void;
};

export function AutocompleteSearch({
  itemType,
  value,
  disabled = false,
  placeholder = "Cerca nei metadati esterni",
  onValueChange,
  onSelect,
  onStatusMessage
}: AutocompleteSearchProps) {
  const [detailsLoadingId, setDetailsLoadingId] = useState<string | null>(null);
  const [detailsError, setDetailsError] = useState<string | null>(null);
  const detailsController = useRef<AbortController | null>(null);
  const search = useExternalMetadataSearch(itemType, value, !disabled);

  useEffect(() => {
    return () => {
      detailsController.current?.abort();
    };
  }, []);

  async function handleSelect(result: LiveMetadataSearchResult) {
    detailsController.current?.abort();
    const controller = new AbortController();
    detailsController.current = controller;
    setDetailsLoadingId(`${result.provider}:${result.externalId}`);
    setDetailsError(null);

    try {
      const details = await collectifyClient.getLiveExternalMetadataDetails(
        itemType,
        result.provider,
        result.externalId,
        controller.signal
      );

      onSelect(details);
      onStatusMessage?.(`Metadati importati da ${details.sourceName}.`);
    } catch (error) {
      if (controller.signal.aborted) {
        return;
      }

      const message = error instanceof Error ? error.message : "Dettagli metadata non disponibili.";
      setDetailsError(message);
      onStatusMessage?.(message);
    } finally {
      if (!controller.signal.aborted) {
        setDetailsLoadingId(null);
      }
    }
  }

  return (
    <div className="metadata-autocomplete">
      <input
        disabled={disabled}
        value={value}
        onChange={(event) => onValueChange(event.target.value)}
        placeholder={placeholder}
      />
      <div className="metadata-autocomplete__status" aria-live="polite">
        {search.isQueryTooShort && <span>Scrivi almeno {search.minimumQueryLength} caratteri per cercare.</span>}
        {search.status === "loading" && <span>Ricerca metadata in corso...</span>}
        {search.status === "error" && <span>{search.errorMessage}</span>}
        {detailsError && <span>{detailsError}</span>}
      </div>
      {search.status === "ready" && search.results.length > 0 && (
        <div className="metadata-autocomplete__results">
          {search.results.map((result) => {
            const resultKey = `${result.provider}:${result.externalId}`;
            const isLoading = detailsLoadingId === resultKey;

            return (
              <button
                key={resultKey}
                type="button"
                onClick={() => void handleSelect(result)}
                disabled={Boolean(detailsLoadingId)}
              >
                {result.imageUrl && <img src={result.imageUrl} alt="" />}
                <span>
                  <strong>{result.title}</strong>
                  <small>
                    {[result.providerName, result.subtitle ?? result.releaseDate].filter(Boolean).join(" - ")}
                  </small>
                  {result.description && <em>{result.description}</em>}
                </span>
                {isLoading && <b>Importo...</b>}
              </button>
            );
          })}
        </div>
      )}
      {search.status === "ready" && value.trim().length >= search.minimumQueryLength && search.results.length === 0 && (
        <div className="metadata-autocomplete__empty">Nessun suggerimento esterno disponibile.</div>
      )}
    </div>
  );
}
