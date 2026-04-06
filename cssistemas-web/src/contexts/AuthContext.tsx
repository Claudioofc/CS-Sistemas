import { createContext, useCallback, useContext, useEffect, useRef, useState } from 'react'
import { apiGet, apiPatch, apiPost, apiPostWithAuth } from '../api/client'

export type DocumentType = 0 | 1

export type User = {
  id: string
  email: string
  name: string
  profilePhotoUrl: string | null
  documentType: DocumentType | null
  documentNumber: string | null
  isAdmin: boolean
  showWelcomeBanner: boolean
  emailVerified: boolean
}

export type RegisterResult = { ok: true; requiresEmailVerification: true; pendingEmail: string } | { ok: false; message: string; errors?: { campo: string; mensagem: string }[] }

export type SubscriptionStatus = {
  hasAccess: boolean
  endsAt: string | null
  isTrial: boolean
  daysRemaining: number | null
}

type AuthContextType = {
  token: string | null
  user: User | null
  isLoading: boolean
  subscriptionStatus: SubscriptionStatus | null
  fetchSubscriptionStatus: () => Promise<void>
  login: (email: string, password: string) => Promise<{ ok: boolean; message?: string; attemptsRemaining?: number; locked?: boolean }>
  register: (name: string, email: string, password: string, documentType: 0 | 1, documentNumber: string) => Promise<RegisterResult>
  logout: () => void
  fetchUser: () => Promise<void>
  setUser: (u: User | null) => void
  updateProfile: (data: { name: string; profilePhotoUrl?: string | null; documentType?: DocumentType | null; documentNumber?: string | null }) => Promise<{ ok: boolean; message?: string; errors?: { campo: string; mensagem: string }[] }>
  dismissWelcomeBanner: () => Promise<void>
  verifyEmail: (email: string, code: string) => Promise<{ ok: boolean; message?: string }>
}

const AuthContext = createContext<AuthContextType | null>(null)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  // Token armazenado apenas em memória (nunca em sessionStorage/localStorage)
  // A persistência da sessão usa o cookie HttpOnly cssistemas_auth via restore-session
  const [token, setTokenState] = useState<string | null>(null)
  const tokenRef = useRef<string | null>(null)
  const [user, setUser] = useState<User | null>(null)
  const [subscriptionStatus, setSubscriptionStatus] = useState<SubscriptionStatus | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  const setToken = useCallback((t: string | null) => {
    tokenRef.current = t
    setTokenState(t)
  }, [])

  const fetchSubscriptionStatus = useCallback(async () => {
    const t = tokenRef.current
    if (!t) {
      setSubscriptionStatus(null)
      return
    }
    const res = await apiGet<{ hasAccess: boolean; endsAt: string | null; isTrial: boolean; daysRemaining: number | null }>('/api/subscription/status', t)
    if (res.ok) {
      setSubscriptionStatus({
        hasAccess: res.data.hasAccess,
        endsAt: res.data.endsAt ?? null,
        isTrial: res.data.isTrial,
        daysRemaining: res.data.daysRemaining ?? null,
      })
    } else {
      setSubscriptionStatus(null)
    }
  }, [])

  const fetchUser = useCallback(async () => {
    try {
      // Restaura sessão via cookie HttpOnly (sem ler sessionStorage)
      const restoreRes = await fetch('/api/auth/restore-session', {
        method: 'GET',
        credentials: 'include',
      })
      if (!restoreRes.ok) {
        setToken(null)
        setUser(null)
        setIsLoading(false)
        return
      }
      const restoreData = await restoreRes.json() as { token: string }
      setToken(restoreData.token)

      // Busca dados completos do usuário com o token recém-emitido
      const meRes = await apiGet<{ id: string; email: string; name: string; profilePhotoUrl: string | null; documentType?: number | null; documentNumber?: string | null; isAdmin?: boolean; showWelcomeBanner?: boolean; emailVerified?: boolean }>('/api/auth/me', restoreData.token)
      if (meRes.ok) {
        setUser({
          id: meRes.data.id,
          email: meRes.data.email,
          name: meRes.data.name,
          profilePhotoUrl: meRes.data.profilePhotoUrl ?? null,
          documentType: meRes.data.documentType != null ? (meRes.data.documentType as DocumentType) : null,
          documentNumber: meRes.data.documentNumber ?? null,
          isAdmin: meRes.data.isAdmin ?? false,
          showWelcomeBanner: meRes.data.showWelcomeBanner ?? false,
          emailVerified: meRes.data.emailVerified ?? true,
        })
      } else {
        setToken(null)
        setUser(null)
      }
    } catch {
      setToken(null)
      setUser(null)
    }
    setIsLoading(false)
  }, [setToken])

  useEffect(() => {
    if (user && tokenRef.current) fetchSubscriptionStatus()
    else setSubscriptionStatus(null)
  }, [user, token, fetchSubscriptionStatus])

  useEffect(() => {
    fetchUser()
  }, [fetchUser])

  const login = useCallback(async (email: string, password: string) => {
    const res = await apiPost<{ email: string; password: string }, { token: string; email: string; name?: string; profilePhotoUrl?: string | null }>(
      '/api/auth/login',
      { email, password }
    )
    if (!res.ok) {
      const err = res.error as { message?: string; mensagem?: string; attemptsRemaining?: number; locked?: boolean }
      return {
        ok: false,
        message: err.message ?? err.mensagem ?? 'Erro ao entrar.',
        attemptsRemaining: err.attemptsRemaining,
        locked: err.locked,
      }
    }
    setToken(res.data.token)
    setUser({
      id: '',
      email: res.data.email,
      name: res.data.name ?? '',
      profilePhotoUrl: res.data.profilePhotoUrl ?? null,
      documentType: null,
      documentNumber: null,
      isAdmin: false,
      showWelcomeBanner: true,
      emailVerified: true,
    })
    await fetchUser()
    return { ok: true }
  }, [fetchUser, setToken])

  const register = useCallback(async (
    name: string,
    email: string,
    password: string,
    documentType: 0 | 1,
    documentNumber: string
  ): Promise<RegisterResult> => {
    try {
      const docDigits = documentNumber.replace(/\D/g, '').trim()
      const res = await apiPost<
        { name: string; email: string; password: string; documentType: number; documentNumber: string },
        { requiresEmailVerification: boolean; email: string }
      >(
        '/api/auth/register',
        { name, email, password, documentType, documentNumber: docDigits }
      )
      if (!res.ok) {
        const err = res.error as { mensagem?: string; message?: string; erros?: { campo: string; mensagem: string }[] }
        return { ok: false, message: err.mensagem ?? err.message ?? 'Erro ao criar conta.', errors: err.erros }
      }
      return { ok: true, requiresEmailVerification: true, pendingEmail: res.data.email }
    } catch {
      return { ok: false, message: 'Verifique sua conexão e tente novamente.' }
    }
  }, [])

  const updateProfile = useCallback(async (payload: { name: string; profilePhotoUrl?: string | null; documentType?: DocumentType | null; documentNumber?: string | null }) => {
    const t = tokenRef.current
    if (!t) return { ok: false, message: 'Não autenticado.' }
    type ProfileResponse = { id: string; email: string; name: string; profilePhotoUrl: string | null; documentType: number | null; documentNumber: string | null; isAdmin?: boolean; showWelcomeBanner?: boolean; emailVerified?: boolean }
    const res = await apiPatch<typeof payload, ProfileResponse>(
      '/api/auth/profile',
      { name: payload.name, profilePhotoUrl: payload.profilePhotoUrl ?? null, documentType: payload.documentType ?? null, documentNumber: payload.documentNumber ?? null },
      t
    )
    if (!res.ok) {
      const err = res.error as { mensagem?: string; message?: string; erros?: { campo: string; mensagem: string }[] }
      return { ok: false, message: err.mensagem ?? err.message ?? 'Erro ao atualizar.', errors: err.erros }
    }
    const profile = res.data
    setUser({
      id: profile.id,
      email: profile.email,
      name: profile.name,
      profilePhotoUrl: profile.profilePhotoUrl ?? null,
      documentType: profile.documentType != null ? (profile.documentType as DocumentType) : null,
      documentNumber: profile.documentNumber ?? null,
      isAdmin: profile.isAdmin ?? false,
      showWelcomeBanner: profile.showWelcomeBanner ?? false,
      emailVerified: profile.emailVerified ?? true,
    })
    return { ok: true }
  }, [])

  const dismissWelcomeBanner = useCallback(async () => {
    const t = tokenRef.current
    if (!t || !user) return
    const res = await apiPostWithAuth<Record<string, never>, unknown>('/api/auth/welcome-banner-dismissed', {}, t)
    if (res.ok && user) {
      setUser({ ...user, showWelcomeBanner: false })
    }
  }, [user])

  const verifyEmail = useCallback(async (email: string, code: string) => {
    const res = await apiPost<{ email: string; code: string }, { token: string; email: string; name: string; profilePhotoUrl?: string | null }>(
      '/api/auth/verify-email',
      { email, code }
    )
    if (!res.ok) {
      const err = res.error as { message?: string; mensagem?: string }
      return { ok: false, message: err.message ?? err.mensagem ?? 'Código inválido ou expirado.' }
    }
    setToken(res.data.token)
    setUser({
      id: '',
      email: res.data.email,
      name: res.data.name,
      profilePhotoUrl: res.data.profilePhotoUrl ?? null,
      documentType: null,
      documentNumber: null,
      isAdmin: false,
      showWelcomeBanner: true,
      emailVerified: true,
    })
    await fetchUser()
    return { ok: true }
  }, [fetchUser, setToken])

  const logout = useCallback(() => {
    setToken(null)
    setUser(null)
    setSubscriptionStatus(null)
    // Limpa o cookie HttpOnly no servidor (fire-and-forget)
    fetch('/api/auth/logout', { method: 'POST', credentials: 'include' }).catch(() => undefined)
  }, [setToken])

  return (
    <AuthContext.Provider value={{ token, user, isLoading, subscriptionStatus, fetchSubscriptionStatus, login, register, logout, fetchUser, setUser, updateProfile, dismissWelcomeBanner, verifyEmail }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
