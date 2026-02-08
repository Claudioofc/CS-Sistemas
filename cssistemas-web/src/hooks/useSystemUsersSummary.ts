import { useEffect, useState } from 'react'
import { apiGet } from '../api/client'
import type { AdminUserItem } from '../types/api'

/** Contagens de clientes do sistema (DRY entre AdminDashboard e Clientes). */
function getCounts(users: AdminUserItem[]) {
  const premiumCount = users.filter((u) => (u.subscriptionLabel ?? 'Gratuito') === 'Premium').length
  const gratuitosCount = users.filter((u) => (u.subscriptionLabel ?? 'Gratuito') === 'Gratuito').length
  return { total: users.length, premiumCount, gratuitosCount }
}

export function useSystemUsersSummary(token: string | null, enabled: boolean) {
  const [users, setUsers] = useState<AdminUserItem[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!token || !enabled) {
      setUsers([])
      setError(null)
      return
    }
    setLoading(true)
    setError(null)
    apiGet<AdminUserItem[]>('/api/admin/users', token).then((res) => {
      setLoading(false)
      if (res.ok) setUsers(res.data)
      else setError(res.status === 403 ? 'Acesso negado.' : 'Erro ao carregar clientes.')
    })
  }, [token, enabled])

  const counts = getCounts(users)
  return { users, ...counts, loading, error }
}
