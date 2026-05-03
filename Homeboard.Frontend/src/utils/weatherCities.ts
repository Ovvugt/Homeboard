export interface WeatherCity {
  name: string
  country: string
  lat: number
  lon: number
}

export const WEATHER_CITIES: WeatherCity[] = [
  { name: 'Honolulu', country: 'US', lat: 21.3099, lon: -157.8581 },
  { name: 'Anchorage', country: 'US', lat: 61.2181, lon: -149.9003 },
  { name: 'Vancouver', country: 'CA', lat: 49.2827, lon: -123.1207 },
  { name: 'Los Angeles', country: 'US', lat: 34.0522, lon: -118.2437 },
  { name: 'Mexico City', country: 'MX', lat: 19.4326, lon: -99.1332 },
  { name: 'Chicago', country: 'US', lat: 41.8781, lon: -87.6298 },
  { name: 'Toronto', country: 'CA', lat: 43.6532, lon: -79.3832 },
  { name: 'New York', country: 'US', lat: 40.7128, lon: -74.0060 },
  { name: 'Lima', country: 'PE', lat: -12.0464, lon: -77.0428 },
  { name: 'Buenos Aires', country: 'AR', lat: -34.6037, lon: -58.3816 },
  { name: 'São Paulo', country: 'BR', lat: -23.5505, lon: -46.6333 },
  { name: 'Reykjavík', country: 'IS', lat: 64.1466, lon: -21.9426 },
  { name: 'London', country: 'UK', lat: 51.5074, lon: -0.1278 },
  { name: 'Lagos', country: 'NG', lat: 6.5244, lon: 3.3792 },
  { name: 'Paris', country: 'FR', lat: 48.8566, lon: 2.3522 },
  { name: 'Madrid', country: 'ES', lat: 40.4168, lon: -3.7038 },
  { name: 'Amsterdam', country: 'NL', lat: 52.3676, lon: 4.9041 },
  { name: 'Berlin', country: 'DE', lat: 52.5200, lon: 13.4050 },
  { name: 'Rome', country: 'IT', lat: 41.9028, lon: 12.4964 },
  { name: 'Stockholm', country: 'SE', lat: 59.3293, lon: 18.0686 },
  { name: 'Cape Town', country: 'ZA', lat: -33.9249, lon: 18.4241 },
  { name: 'Cairo', country: 'EG', lat: 30.0444, lon: 31.2357 },
  { name: 'Athens', country: 'GR', lat: 37.9838, lon: 23.7275 },
  { name: 'Istanbul', country: 'TR', lat: 41.0082, lon: 28.9784 },
  { name: 'Moscow', country: 'RU', lat: 55.7558, lon: 37.6173 },
  { name: 'Dubai', country: 'AE', lat: 25.2048, lon: 55.2708 },
  { name: 'Mumbai', country: 'IN', lat: 19.0760, lon: 72.8777 },
  { name: 'Delhi', country: 'IN', lat: 28.7041, lon: 77.1025 },
  { name: 'Bangkok', country: 'TH', lat: 13.7563, lon: 100.5018 },
  { name: 'Singapore', country: 'SG', lat: 1.3521, lon: 103.8198 },
  { name: 'Hong Kong', country: 'HK', lat: 22.3193, lon: 114.1694 },
  { name: 'Shanghai', country: 'CN', lat: 31.2304, lon: 121.4737 },
  { name: 'Seoul', country: 'KR', lat: 37.5665, lon: 126.9780 },
  { name: 'Tokyo', country: 'JP', lat: 35.6762, lon: 139.6503 },
  { name: 'Sydney', country: 'AU', lat: -33.8688, lon: 151.2093 },
  { name: 'Auckland', country: 'NZ', lat: -36.8485, lon: 174.7633 },
]

export function matchCity(lat: number | null | undefined, lon: number | null | undefined): WeatherCity | null {
  if (lat == null || lon == null) return null
  const latStr = lat.toFixed(4)
  const lonStr = lon.toFixed(4)
  return WEATHER_CITIES.find(c => c.lat.toFixed(4) === latStr && c.lon.toFixed(4) === lonStr) ?? null
}

export function cityKey(c: WeatherCity): string {
  return `${c.lat.toFixed(4)},${c.lon.toFixed(4)}`
}
