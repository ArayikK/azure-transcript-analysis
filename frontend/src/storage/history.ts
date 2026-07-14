import type { AnalyzeRequest, AnalyzeResponse, HistoryItem } from '../types';

/**
 * History persistence — browser localStorage.
 *
 * Every successful analysis is saved here so the History page and
 * /transcription/:id keep working across page reloads and browser restarts.
 * Note: localStorage is per-browser/per-machine. If the team later wants
 * shared history, this module is the only file to replace with real API
 * calls (the React Query hooks already treat it as an async source).
 */

const STORAGE_KEY = 'transcript_history_v1';
const MAX_ITEMS = 100; // avoid growing without bound

function readAll(): HistoryItem[] {
    try {
        const raw = localStorage.getItem(STORAGE_KEY);
        if (!raw) return [];
        const parsed: unknown = JSON.parse(raw);
        return Array.isArray(parsed) ? (parsed as HistoryItem[]) : [];
    } catch {
        return []; // corrupted storage should never crash the app
    }
}

function writeAll(items: HistoryItem[]): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(items));
}

/** Newest first. */
export function listHistoryItems(): HistoryItem[] {
    return readAll().sort(
        (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
    );
}

export function getHistoryItem(id: string): HistoryItem | null {
    return readAll().find((item) => item.id === id) ?? null;
}

export function addHistoryItem(request: AnalyzeRequest, response: AnalyzeResponse): HistoryItem {
    const item: HistoryItem = {
        id: crypto.randomUUID(),
        createdAt: new Date().toISOString(),
        request,
        response,
    };
    const items = [item, ...readAll()].slice(0, MAX_ITEMS);
    writeAll(items);
    return item;
}

export function removeHistoryItem(id: string): void {
    writeAll(readAll().filter((item) => item.id !== id));
}

export function clearHistory(): void {
    localStorage.removeItem(STORAGE_KEY);
}
