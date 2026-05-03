import { request } from './client'

export interface MinecraftStatusDto {
  host: string
  port: number
  online: boolean
  versionName: string | null
  protocolVersion: number | null
  playersOnline: number | null
  playersMax: number | null
  playerSample: string[] | null
  motd: string | null
  latencyMs: number | null
  faviconDataUri: string | null
  error: string | null
  fetchedUtc: string
}

export const minecraftApi = {
  get: (host: string, port?: number) =>
    request<MinecraftStatusDto>('/api/widgets/minecraft', {
      params: port ? { host, port } : { host },
    }),
}
