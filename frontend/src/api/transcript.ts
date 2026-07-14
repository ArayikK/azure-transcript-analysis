import { apiClient } from './client';
import type { AnalyzeRequest, AnalyzeResponse } from '../types';

/** Calls the backend's analyze endpoint. Throws on any non-2xx response. */
export async function analyzeTranscript(request: AnalyzeRequest): Promise<AnalyzeResponse> {
    const { data } = await apiClient.post<AnalyzeResponse>('/api/transcript/analyze', request);
    return data;
}
