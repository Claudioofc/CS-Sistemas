import { useEffect, useState } from 'react'
import { useAuth } from '../contexts/AuthContext'
import { formatDate, formatDateOnly, formatCurrency } from '../utils/format'
import { useSystemUsersSummary } from '../hooks/useSystemUsersSummary'
import { apiGet } from '../api/client'
import type { AdminPremiumSubscriptionItem } from '../types/api'

function KpiCard({ label, value, sub, accent }: { label: string; value: number | string; sub?: string; accent?: boolean }) {
  return (
    <div className="bg-white rounded-xl border border-gray-200 p-5 shadow-sm">
      <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider">{label}</p>
      <p className={`text-3xl font-bold mt-1 ${accent ? 'text-primary' : 'text-gray-900'}`}>{value}</p>
      {sub && <p className="text-xs text-gray-400 mt-1">{sub}</p>}
    </div>
  )
}

function StatusBadge({ label }: { label: string }) {
  const isPremium = label === 'Premium'
  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${isPremium ? 'bg-primary/10 text-primary' : 'bg-gray-100 text-gray-600'}`}>
      {label}
    </span>
  )
}

export default function AdminDashboard() {
  const { token } = useAuth()
  const { users, total, premiumCount, loading, error } = useSystemUsersSummary(token, !!token)
  const [subscriptions, setSubscriptions] = useState<AdminPremiumSubscriptionItem[]>([])
  const [subsLoading, setSubsLoading] = useState(false)

  useEffect(() => {
    if (!token) return
    setSubsLoading(true)
    apiGet<AdminPremiumSubscriptionItem[]>('/api/admin/subscriptions/premium?limit=100', token).then((res) => {
      setSubsLoading(false)
      if (res.ok && Array.isArray(res.data)) setSubscriptions(res.data)
    })
  }, [token])

  const now = new Date()
  const newThisMonth = users.filter((u) => {
    const d = new Date(u.createdAt)
    return d.getFullYear() === now.getFullYear() && d.getMonth() === now.getMonth()
  }).length

  const recentUsers = [...users]
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
    .slice(0, 10)

  const recentSubs = [...subscriptions]
    .sort((a, b) => new Date(b.startedAt).getTime() - new Date(a.startedAt).getTime())
    .slice(0, 5)

  const totalRevenue = subscriptions.reduce((sum, s) => sum + (s.price ?? 0), 0)

  if (loading) {
    return (
      <div className="flex items-center justify-center py-16">
        <p className="text-gray-400 text-sm">Carregando...</p>
      </div>
    )
  }

  if (error) {
    return (
      <div className="rounded-lg bg-red-50 border border-red-200 p-4 text-red-700 text-sm">{error}</div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Painel Administrativo</h1>
        <p className="text-sm text-gray-500 mt-0.5">Visão geral do sistema CS Sistemas</p>
      </div>

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <KpiCard label="Total de usuários" value={total} sub="Todos os cadastros" />
        <KpiCard label="Assinantes premium" value={premiumCount} sub="Plano pago ativo" accent />
        <KpiCard label="Novos este mês" value={newThisMonth} sub={now.toLocaleString('pt-BR', { month: 'long', year: 'numeric' })} />
        <KpiCard label="Receita total" value={formatCurrency(totalRevenue)} sub="Soma de todas as assinaturas" accent />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Últimas assinaturas */}
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          <div className="px-5 py-4 border-b border-gray-100">
            <h2 className="font-semibold text-gray-900">Últimas assinaturas</h2>
            <p className="text-xs text-gray-500 mt-0.5">5 mais recentes — veja todas em Assinaturas</p>
          </div>
          <div className="divide-y divide-gray-100">
            {subsLoading ? (
              <p className="px-5 py-6 text-sm text-gray-400">Carregando...</p>
            ) : recentSubs.length === 0 ? (
              <p className="px-5 py-6 text-sm text-gray-400">Nenhuma assinatura ainda.</p>
            ) : (
              recentSubs.map((s, i) => (
                <div key={`${s.userId}-${i}`} className="px-5 py-3 flex items-center justify-between gap-3">
                  <div className="min-w-0">
                    <p className="text-sm font-medium text-gray-900 truncate">{s.userName || '—'}</p>
                    <p className="text-xs text-gray-500 truncate">{s.userEmail}</p>
                  </div>
                  <div className="text-right flex-shrink-0">
                    <p className="text-sm font-semibold text-primary">{formatCurrency(s.price)}</p>
                    <p className="text-xs text-gray-400">{s.planName}</p>
                    <p className="text-xs text-gray-400">{formatDateOnly(s.startedAt)}</p>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>

        {/* Últimos cadastros */}
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          <div className="px-5 py-4 border-b border-gray-100">
            <h2 className="font-semibold text-gray-900">Últimos cadastros</h2>
            <p className="text-xs text-gray-500 mt-0.5">10 usuários mais recentes</p>
          </div>
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
                {recentUsers.map((u) => (
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
            {recentUsers.length === 0 && (
              <p className="px-5 py-10 text-center text-sm text-gray-400">Nenhum usuário cadastrado.</p>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
