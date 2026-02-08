/**
 * Base URL da API (DRY). Em produção, usa mesma origem da página (rotas atualizadas automaticamente).
 * Se VITE_API_URL estiver definido, usa esse valor; senão, no browser usa window.location.origin
 * para que links públicos (ex.: cancelar pelo e-mail) sempre chamem a API no mesmo host.
 * Em dev com Vite, '' faz o proxy encaminhar /api para a API.
 */
const getBaseUrl = (): string => {
  const env = import.meta.env.VITE_API_URL
  if (env != null && String(env).trim() !== '') return String(env).trim().replace(/\/$/, '')
  if (typeof window !== 'undefined' && window.location?.origin)
    return window.location.origin
  return ''
}

export type ValidationErrorResponse = {
  mensagem: string
  erros?: { campo: string; mensagem: string }[]
}

/** Caminho da API de cancelamento público (DRY). */
export const API_PUBLIC_BOOKING_CANCELAR = '/api/public/booking/cancelar'

function buildUrl(path: string): string {
  const base = getBaseUrl()
  const p = path.startsWith('/') ? path : `/${path}`
  return base ? `${base}${p}` : p
}

/**
 * Retorna a URL absoluta da foto de perfil (para exibir em img src quando a API está em outro origin).
 * Adiciona ?v= para evitar cache do navegador (foto atualizada após novo login).
 */
export function getProfilePhotoUrl(url: string | null | undefined, cacheBust?: string): string | null {
  if (!url?.trim()) return null
  const u = url.trim()
  const base = u.startsWith('http://') || u.startsWith('https://') ? u : getBaseUrl() + (u.startsWith('/') ? u : `/${u}`)
  if (cacheBust) return base + (base.includes('?') ? '&' : '?') + 'v=' + encodeURIComponent(cacheBust)
  return base
}

function authHeaders(token: string | null): HeadersInit {
  const h: HeadersInit = { 'Content-Type': 'application/json' }
  if (token) (h as Record<string, string>)['Authorization'] = `Bearer ${token}`
  return h
}

export async function apiGet<TResponse>(
  path: string,
  token: string | null
): Promise<{ ok: true; data: TResponse } | { ok: false; status: number }> {
  const res = await fetch(buildUrl(path), { method: 'GET', headers: authHeaders(token) })
  const data = await res.json().catch(() => ({})) as TResponse
  if (res.ok) return { ok: true, data }
  return { ok: false, status: res.status }
}

export async function apiPost<TBody, TResponse>(
  path: string,
  body: TBody
): Promise<{ ok: true; data: TResponse } | { ok: false; status: number; error: ValidationErrorResponse | { message?: string } }> {
  const res = await fetch(buildUrl(path), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
  const data = await res.json().catch(() => ({})) as Record<string, unknown>
  if (res.ok) return { ok: true, data: data as TResponse }
  // Log para diagnóstico quando for reset-password e der erro
  if (path.includes('reset-password') && !res.ok) {
    console.warn('[apiPost] reset-password erro:', { status: res.status, url: buildUrl(path), body: data })
  }
  const mensagem = (data?.mensagem ?? data?.Mensagem ?? data?.message ?? data?.Message ?? data?.detail ?? data?.title) as string | undefined
  const erros = (data?.erros ?? data?.Erros) as { campo?: string; mensagem?: string }[] | undefined
  // ProblemDetails (model validation) usa "errors" como objeto: { "Campo": ["msg1", "msg2"] }
  const errorsObj = data?.errors as Record<string, string[]> | undefined
  const problemDetailsMsg = errorsObj && typeof errorsObj === 'object'
    ? Object.values(errorsObj).flat().filter(Boolean).join(' ')
    : ''
  const message = mensagem ?? (problemDetailsMsg || (res.status >= 500 ? 'Erro no servidor. Tente novamente.' : 'Um ou mais campos estão inválidos.'))
  const baseError = erros ? { mensagem: message, erros } : { message }
  const extra = {
    ...(data?.attemptsRemaining != null && { attemptsRemaining: data.attemptsRemaining as number }),
    ...(data?.locked !== undefined && { locked: data.locked as boolean }),
    ...(data?.lockoutEnd != null && { lockoutEnd: data.lockoutEnd as string }),
  }
  return {
    ok: false,
    status: res.status,
    error: { ...baseError, ...extra } as ValidationErrorResponse | { message?: string; attemptsRemaining?: number; locked?: boolean; lockoutEnd?: string },
  }
}

/** Upload de arquivo (multipart) com token. Retorna { ok, data } ou { ok: false, status, error }. */
export async function apiUploadProfilePhoto(
  formData: FormData,
  token: string | null
): Promise<{ ok: true; data: { url: string } } | { ok: false; status: number; error: { message?: string } }> {
  const res = await fetch(buildUrl('/api/auth/profile-photo'), {
    method: 'POST',
    headers: token ? { Authorization: `Bearer ${token}` } : {},
    body: formData,
  })
  const data = await res.json().catch(() => ({})) as { url?: string; message?: string }
  if (res.ok) return { ok: true, data: { url: data.url ?? '' } }
  return { ok: false, status: res.status, error: { message: data.message ?? 'Erro ao enviar imagem.' } }
}

/** POST com token (ex.: checkout Mercado Pago). */
export async function apiPostWithAuth<TBody, TResponse>(
  path: string,
  body: TBody,
  token: string | null
): Promise<{ ok: true; data: TResponse } | { ok: false; status: number; error: ValidationErrorResponse | { message?: string } }> {
  const res = await fetch(buildUrl(path), {
    method: 'POST',
    headers: authHeaders(token),
    body: JSON.stringify(body),
  })
  const data = await res.json().catch(() => ({})) as Record<string, unknown>
  if (res.ok) return { ok: true, data: data as TResponse }
  const mensagem = (data?.mensagem ?? data?.message) as string | undefined
  const message = mensagem ?? (res.status >= 500 ? 'Erro no servidor. Tente novamente.' : 'Erro ao processar.')
  const detail = (data?.detail as string | undefined) ?? undefined
  return { ok: false, status: res.status, error: { message, ...(detail ? { detail } : {}) } }
}

export async function apiPatch<TBody, TResponse>(
  path: string,
  body: TBody,
  token: string | null
): Promise<{ ok: true; data: TResponse } | { ok: false; status: number; error: ValidationErrorResponse | { message?: string } }> {
  const res = await fetch(buildUrl(path), {
    method: 'PATCH',
    headers: authHeaders(token),
    body: JSON.stringify(body),
  })
  const data = await res.json().catch(() => ({})) as Record<string, unknown>
  if (res.ok) return { ok: true, data: data as TResponse }
  const mensagem = (data?.mensagem ?? data?.message) as string | undefined
  const erros = (data?.erros) as { campo?: string; mensagem?: string }[] | undefined
  return {
    ok: false,
    status: res.status,
    error: (erros ? { mensagem: mensagem ?? 'Um ou mais campos estão inválidos.', erros } : { message: mensagem }) as ValidationErrorResponse | { message?: string },
  }
}

export async function apiPut<TBody, TResponse>(
  path: string,
  body: TBody,
  token: string | null
): Promise<{ ok: true; data: TResponse } | { ok: false; status: number; error: ValidationErrorResponse | { message?: string } }> {
  const res = await fetch(buildUrl(path), {
    method: 'PUT',
    headers: authHeaders(token),
    body: JSON.stringify(body),
  })
  const data = await res.json().catch(() => ({})) as Record<string, unknown>
  if (res.ok) return { ok: true, data: data as TResponse }
  const mensagem = (data?.mensagem ?? data?.message) as string | undefined
  const erros = (data?.erros) as { campo?: string; mensagem?: string }[] | undefined
  return {
    ok: false,
    status: res.status,
    error: (erros ? { mensagem: mensagem ?? 'Um ou mais campos estão inválidos.', erros } : { message: mensagem }) as ValidationErrorResponse | { message?: string },
  }
}

export async function apiDelete(
  path: string,
  token: string | null
): Promise<{ ok: true } | { ok: false; status: number; error?: { message?: string } }> {
  const res = await fetch(buildUrl(path), { method: 'DELETE', headers: authHeaders(token) })
  if (res.ok) return { ok: true }
  const data = await res.json().catch(() => ({})) as Record<string, unknown>
  const message = (data?.mensagem ?? data?.message) as string | undefined
  return { ok: false, status: res.status, error: { message } }
}
