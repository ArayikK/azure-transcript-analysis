import axios from 'axios';

/**
 * The single axios instance used for all backend calls.
 * Base URL: VITE_API_URL from .env, or the backend's default dev port.
 */
export const apiClient = axios.create({
    baseURL: import.meta.env.VITE_API_URL || '',
    timeout: 60_000,
});

/**
 * Turns any thrown error into a message we can show to the user.
 * The backend returns plain-text explanations for 400/401/503/500 —
 * surface those directly when present.
 */
export function getApiErrorMessage(error: unknown): string {
    if (axios.isAxiosError(error)) {
        const data: unknown = error.response?.data;
        if (data) {
            if (typeof data === 'string' && data.trim().length > 0) {
                return data;
            }
            if (typeof data === 'object' && data !== null) {
                const obj = data as Record<string, unknown>;
                const msg = obj.message || obj.error || obj.details;
                if (typeof msg === 'string' && msg.trim().length > 0) {
                    return msg;
                }
                try {
                    return JSON.stringify(data);
                } catch {
                    // ignore fallback
                }
            }
        }
        if (error.response) {
            return `The server answered with status ${error.response.status}.`;
        }
        return error.message || 'Cannot reach the backend. Is the server running?';
    }
    return error instanceof Error ? error.message : 'Something unexpected went wrong.';
}
