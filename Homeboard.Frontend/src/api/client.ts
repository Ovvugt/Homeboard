import axios, { AxiosError, type AxiosInstance } from 'axios'

export class ApiError extends Error {
  constructor(
    public status: number,
    public statusText: string,
    public body?: unknown
  ) {
    super(`${status} ${statusText}`)
    this.name = 'ApiError'
  }
}

const instance: AxiosInstance = axios.create()

instance.interceptors.response.use(
  res => res,
  (error: AxiosError) => {
    throw new ApiError(
      error.response?.status ?? 0,
      error.response?.statusText ?? '',
      error.response?.data,
    )
  }
)

export interface RequestOptions {
  method?: string
  body?: unknown
  params?: Record<string, string | number | boolean | undefined | null>
}

export function request<T>(url: string, options: RequestOptions = {}): Promise<T> {
  const { method = 'GET', body, params } = options
  return instance
    .request<T>({ url, method, data: body, params })
    .then(res => res.data)
}
