import type { AuthSession, RegistryMod } from './types';

export class TheUnlockerClient {
  constructor(
    private readonly baseUrl = '/go-api',
    private readonly token?: string,
  ) {}

  async health(): Promise<Record<string, unknown>> {
    return this.get('/api/v1/health');
  }

  async mods(): Promise<RegistryMod[]> {
    return this.get('/api/v1/mods');
  }

  async mod(id: string): Promise<RegistryMod> {
    return this.get(`/api/v1/mods/${encodeURIComponent(id)}`);
  }

  async me(): Promise<AuthSession> {
    return this.get('/api/v1/me');
  }

  async createInstall(modId: string): Promise<Record<string, unknown>> {
    return this.post('/api/v1/installs', { modId });
  }

  private async get<T>(path: string): Promise<T> {
    const response = await fetch(`${this.baseUrl}${path}`, { headers: this.headers() });
    return this.parse<T>(response);
  }

  private async post<T>(path: string, body: unknown): Promise<T> {
    const response = await fetch(`${this.baseUrl}${path}`, {
      method: 'POST',
      headers: { ...this.headers(), 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
    return this.parse<T>(response);
  }

  private headers(): Record<string, string> {
    return this.token ? { Authorization: `Bearer ${this.token}` } : {};
  }

  private async parse<T>(response: Response): Promise<T> {
    if (!response.ok) {
      throw new Error(`TheUnlocker API returned ${response.status}`);
    }
    return response.json();
  }
}
