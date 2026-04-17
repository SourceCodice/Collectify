import { useEffect, useRef, useState } from "react";
import { collectifyClient } from "../../shared/api/collectifyClient";
import type { LiveMetadataSearchResult } from "./types";

export type LiveMetadataSearchState = "idle" | "loading" | "ready" | "error";

const minimumQueryLength = 3;
const debounceDelayMs = 320;

export function useExternalMetadataSearch(itemType: string, query: string, providerId?: string, enabled = true) {
  const [results, setResults] = useState<LiveMetadataSearchResult[]>([]);
  const [status, setStatus] = useState<LiveMetadataSearchState>("idle");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const requestVersion = useRef(0);

  useEffect(() => {
    const normalizedItemType = itemType.trim();
    const normalizedQuery = query.trim();

    if (!enabled || !normalizedItemType || normalizedQuery.length < minimumQueryLength) {
      requestVersion.current += 1;
      setResults([]);
      setStatus("idle");
      setErrorMessage(null);
      return;
    }

    const currentVersion = requestVersion.current + 1;
    requestVersion.current = currentVersion;
    const controller = new AbortController();
    const timeoutId = window.setTimeout(async () => {
      setStatus("loading");
      setErrorMessage(null);

      try {
        const response = await collectifyClient.searchLiveExternalMetadata(
          normalizedItemType,
          normalizedQuery,
          providerId,
          controller.signal
        );

        if (requestVersion.current === currentVersion) {
          setResults(response);
          setStatus("ready");
        }
      } catch (error) {
        if (controller.signal.aborted || requestVersion.current !== currentVersion) {
          return;
        }

        setResults([]);
        setStatus("error");
        setErrorMessage(error instanceof Error ? error.message : "Ricerca metadata non disponibile.");
      }
    }, debounceDelayMs);

    return () => {
      window.clearTimeout(timeoutId);
      controller.abort();
    };
  }, [enabled, itemType, providerId, query]);

  return {
    results,
    status,
    errorMessage,
    isQueryTooShort: query.trim().length > 0 && query.trim().length < minimumQueryLength,
    minimumQueryLength
  };
}
