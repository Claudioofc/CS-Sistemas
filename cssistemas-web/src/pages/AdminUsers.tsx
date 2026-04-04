import { useState, useEffect } from 'react'
import { useAuth } from '../contexts/AuthContext'
import { apiGet } from '../api/client'
import { formatDate } from '../utils/format'
import type { AdminUserItem } from '../types/api'

function StatusBadge({ label }: { label: string }) {
  const isPremium = label === 'Premium'
  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${isPremium ? 'bg-primary/10 text-primary' : 'bg-gray-100 text-gray-600'}`}>
      {label}
    </span>
  )
}

export default function AdminUsers() {
  const { token } = useAuth()
  const [users, setUsers] = useState<AdminUserItem[]>([])
  const [filtered, setFiltered] = useState<AdminUserItem[]>([])
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!token) return
    setLoading(true)
    apiGet<AdminUserItem[]>('/api/admin/users', token).then((res) => {
      setLoading(false)
      if (res.ok) {
        const sorted = [...res.data].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
        setUsers(sorted)
        setFiltered(sorted)
      } else {
        setError('Erro ao carregar usuários.')
      }
    })
  }, [token])

  useEffect(() => {
    const term = search.trim().toLowerCase()
    if (!term) {
      setFiltered(users)
      return
    }
    setFiltered(users.filter((u) =>
      u.name.toLowerCase().includes(term) || u.email.toLowerCase().includes(term)
    ))
  }, [search, users])

  const premiumCount = filtered.filter((u) => u.subscriptionLabel === 'Premium').length

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Usuários</h1>
          <p className="text-sm text-gray-500 mt-0.5">
            {filtered.length} usuário{filtered.length !== 1 ? 's' : ''} encontrado{filtered.length !== 1 ? 's' : ''}
            {premiumCount > 0 && <span className="ml-2 text-primary font-medium">· {premiumCount} Premium</span>}
          </p>
        </div>
      </div>

      <div className="relative max-w-sm">
        <input
          type="text"
          placeholder="Buscar por nome ou e-mail..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary"
        />
        <svg className="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden>
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        {search && (
          <button onClick={() => setSearch('')} className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600" aria-label="Limpar busca">
            ×
          </button>
        )}
      </div>

      {loading && <p className="text-sm text-gray-400 py-8 text-center">Carregando...</p>}
      {error && <div className="rounded-lg bg-red-50 border border-red-200 p-4 text-red-700 text-sm">{error}</div>}

      {!loading && !error && (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-100">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-5 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Nome</th>
                  <th className="px-5 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Cadastro</th>
                  <th className="px-5 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Plano</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100 bg-white">
                {filtered.map((u) => (
                  <tr key={u.id} className="hover:bg-gray-50 transition-colors">
                    <td className="px-5 py-3">
                      <p className="text-sm font-medium text-gray-900">{u.name || '—'}</p>
                      <p className="text-xs text-gray-500">{u.email}</p>
                    </td>
                    <td className="px-5 py-3 text-sm text-gray-500 whitespace-nowrap">{formatDate(u.createdAt)}</td>
                    <td className="px-5 py-3"><StatusBadge label={u.subscriptionLabel ?? 'Gratuito'} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
            {filtered.length === 0 && (
              <p className="px-5 py-10 text-center text-sm text-gray-400">
                {search ? 'Nenhum usuário encontrado para esta busca.' : 'Nenhum usuário cadastrado.'}
              </p>
            )}
          </div>
        </div>
      )}
    </div>
  )
}
