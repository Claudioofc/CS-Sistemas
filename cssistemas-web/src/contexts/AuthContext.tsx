import { createContext, useCallback, useContext, useEffect, useState } from 'react'
import { apiGet, apiPatch, apiPost } from '../api/client'

const TOKEN_KEY = 'cssistemas_token'

/** 0 = CPF, 1 = CNPJ (enum do backend). */
export type DocumentType = 0 | 1

export type User = {
  id: string
  email: string
  name: string
  profilePhotoUrl: string | null
  documentType: DocumentType | null
  documentNumber: string | null
  isAdmin: boolean
}

export type RegisterResult = { ok: true } | { ok: false; message: string; errors?: { campo: string; mensagem: string }[] }

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
}

const AuthContext = createContext<AuthContextType | null>(null)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [token, setToken] = useState<string | null>(() => localStorage.getItem(TOKEN_KEY))
  const [user, setUser] = useState<User | null>(null)
  const [subscriptionStatus, setSubscriptionStatus] = useState<SubscriptionStatus | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  const fetchSubscriptionStatus = useCallback(async () => {
    const t = localStorage.getItem(TOKEN_KEY)
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
    const t = localStorage.getItem(TOKEN_KEY)
    if (!t) {
      setUser(null)
      setIsLoading(false)
      return
    }
    const res = await apiGet<{ id: string; email: string; name: string; profilePhotoUrl: string | null; documentType?: number | null; documentNumber?: string | null; isAdmin?: boolean }>('/api/auth/me', t)
    if (res.ok) {
      setToken(t)
      setUser({
        id: res.data.id,
        email: res.data.email,
        name: res.data.name,
        profilePhotoUrl: res.data.profilePhotoUrl ?? null,
        documentType: res.data.documentType != null ? (res.data.documentType as DocumentType) : null,
        documentNumber: res.data.documentNumber ?? null,
        isAdmin: res.data.isAdmin ?? false,
      })
    } else {
      setToken(null)
      setUser(null)
      setSubscriptionStatus(null)
      localStorage.removeItem(TOKEN_KEY)
    }
    setIsLoading(false)
  }, [])

  useEffect(() => {
    if (user && token) fetchSubscriptionStatus()
    else setSubscriptionStatus(null)
  }, [user, token, fetchSubscriptionStatus])

  useEffect(() => {
    fetchUser()
  }, [fetchUser])

  const login = useCallback(async (email: string, password: string) => {
    const res = await apiPost<{ email: string; password: string }, { token: string; email: string; name: string; profilePhotoUrl?: string | null }>(
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
    localStorage.setItem(TOKEN_KEY, res.data.token)
    setToken(res.data.token)
    // Dados iniciais do login; fetchUser() em seguida traz perfil completo (incluindo foto persistida)
    setUser({
      id: '',
      email: res.data.email,
      name: res.data.name,
      profilePhotoUrl: res.data.profilePhotoUrl ?? null,
      documentType: null,
      documentNumber: null,
      isAdmin: false,
    })
    await fetchUser()
    return { ok: true }
  }, [fetchUser])

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
        { token: string; email: string; name: string; profilePhotoUrl?: string | null }
      >(
        '/api/auth/register',
        { name, email, password, documentType, documentNumber: docDigits }
      )
      if (!res.ok) {
        const err = res.error as { mensagem?: string; message?: string; erros?: { campo: string; mensagem: string }[] }
        return { ok: false, message: err.mensagem ?? err.message ?? 'Erro ao criar conta.', errors: err.erros }
      }
      localStorage.setItem(TOKEN_KEY, res.data.token)
      setToken(res.data.token)
      setUser({
        id: '',
        email: res.data.email,
        name: res.data.name,
        profilePhotoUrl: res.data.profilePhotoUrl ?? null,
        documentType: null,
        documentNumber: null,
        isAdmin: false,
      })
      await fetchUser()
      return { ok: true }
    } catch {
      return { ok: false, message: 'Verifique sua conexão e tente novamente.' }
    }
  }, [fetchUser])

  const updateProfile = useCallback(async (data: { name: string; profilePhotoUrl?: string | null; documentType?: DocumentType | null; documentNumber?: string | null }) => {
    const t = localStorage.getItem(TOKEN_KEY)
    if (!t) return { ok: false, message: 'Não autenticado.' }
    const res = await apiPatch<typeof data, { id: string; email: string; name: string; profilePhotoUrl: string | null; documentType: number | null; documentNumber: string | null }>(
      '/api/auth/profile',
      { name: data.name, profilePhotoUrl: data.profilePhotoUrl ?? null, documentType: data.documentType ?? null, documentNumber: data.documentNumber ?? null },
      t
    )
    if (!res.ok) {
      const err = res.error as { mensagem?: string; message?: string; erros?: { campo: string; mensagem: string }[] }
      return { ok: false, message: err.mensagem ?? err.message ?? 'Erro ao atualizar.', errors: err.erros }
    }
    setUser({
      id: res.data.id,
      email: res.data.email,
      name: res.data.name,
      profilePhotoUrl: res.data.profilePhotoUrl ?? null,
      documentType: res.data.documentType != null ? (res.data.documentType as DocumentType) : null,
      documentNumber: res.data.documentNumber ?? null,
      isAdmin: (res.data as { isAdmin?: boolean }).isAdmin ?? false,
    })
    return { ok: true }
  }, [])

  const logout = useCallback(() => {
    localStorage.removeItem(TOKEN_KEY)
    setToken(null)
    setUser(null)
    setSubscriptionStatus(null)
  }, [])

  return (
    <AuthContext.Provider value={{ token, user, isLoading, subscriptionStatus, fetchSubscriptionStatus, login, register, logout, fetchUser, setUser, updateProfile }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
