import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { analyzeTranscript } from '../api/transcript';
import {
    addHistoryItem,
    clearHistory,
    getHistoryItem,
    listHistoryItems,
    removeHistoryItem,
} from '../storage/history';
import type { AnalyzeRequest, HistoryItem } from '../types';

const HISTORY_KEY = ['history'];

/**
 * Analyze mutation: POSTs to the backend, then saves the result to history.
 * Returns the saved HistoryItem so pages can link to /transcription/:id.
 */
export function useAnalyze() {
    const queryClient = useQueryClient();
    return useMutation<HistoryItem, unknown, AnalyzeRequest>({
        mutationFn: async (request) => {
            const response = await analyzeTranscript(request);
            return addHistoryItem(request, response);
        },
        onSuccess: () => {
            void queryClient.invalidateQueries({ queryKey: HISTORY_KEY });
        },
    });
}

/** History list (newest first). */
export function useHistory() {
    return useQuery({ queryKey: HISTORY_KEY, queryFn: listHistoryItems });
}

/** One saved analysis by id (null when the id is unknown). */
export function useHistoryItem(id: string | undefined) {
    return useQuery({
        queryKey: [...HISTORY_KEY, id],
        queryFn: () => getHistoryItem(id ?? ''),
        enabled: Boolean(id),
    });
}

/** Delete one item / clear all, refreshing the list afterwards. */
export function useDeleteHistory() {
    const queryClient = useQueryClient();
    const invalidate = () => void queryClient.invalidateQueries({ queryKey: HISTORY_KEY });
    return {
        removeItem: (id: string) => {
            removeHistoryItem(id);
            invalidate();
        },
        clearAll: () => {
            clearHistory();
            invalidate();
        },
    };
}
