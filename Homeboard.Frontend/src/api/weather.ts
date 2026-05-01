import { request } from './client'

export interface WeatherDto {
  latitude: number
  longitude: number
  temperatureC: number
  apparentTemperatureC: number | null
  weatherCode: number | null
  windSpeedKmh: number | null
  humidity: number | null
  fetchedUtc: string
}

export const weatherApi = {
  get: (lat: number, lon: number) =>
    request<WeatherDto>('/api/widgets/weather', { params: { lat, lon } }),
}
