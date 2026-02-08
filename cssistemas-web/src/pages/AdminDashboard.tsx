import { useAuth } from '../contexts/AuthContext'
import { formatDate } from '../utils/format'
import { useSystemUsersSummary } from '../hooks/useSystemUsersSummary'
import SystemClientsSummaryCards from '../components/SystemClientsSummaryCards'

export default function AdminDashboard() {
  const { token } = useAuth()
  const { users, total, premiumCount, gratuitosCount, loading, error } = useSystemUsersSummary(token, !!token)

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <p className="text-gray-500">Carregando clientes...</p>
      </div>
    )
  }
  if (error) {
    return (
      <div className="rounded-lg bg-red-50 border border-red-200 p-4 text-red-700">
        {error}
      </div>
    )
  }

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-1">Controle de clientes</h1>
      <p className="text-gray-600 text-sm mb-6">
        Todos os clientes cadastrados no sistema (premium e gratuito). Visível apenas para administradores.
      </p>

      <SystemClientsSummaryCards
        total={total}
        premiumCount={premiumCount}
        gratuitosCount={gratuitosCount}
      />

      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden shadow-sm">
        <div className="px-4 py-3 border-b border-gray-200 bg-gray-50">
          <h2 className="font-semibold text-gray-900">Lista de clientes</h2>
          <p className="text-sm text-gray-500">Todos os clientes (premium e free)</p>
        </div>
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Nome</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">E-mail</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Cadastro</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Plano</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Admin</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200 bg-white">
              {users.map((u) => (
                <tr key={u.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 text-sm text-gray-900">{u.name || '—'}</td>
                  <td className="px-4 py-3 text-sm text-gray-700">{u.email}</td>
                  <td className="px-4 py-3 text-sm text-gray-600">{formatDate(u.createdAt)}</td>
                  <td className="px-4 py-3">
                    <span className={(u.subscriptionLabel ?? 'Gratuito') === 'Premium' ? 'inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-primary/10 text-primary' : 'text-gray-600 text-sm'}>
                      {u.subscriptionLabel ?? 'Gratuito'}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    {u.isAdmin ? (
                      <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-amber-100 text-amber-800">Sim</span>
                    ) : (
                      <span className="text-gray-400 text-sm">—</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        {users.length === 0 && (
          <div className="px-4 py-8 text-center text-gray-500">Nenhum cliente cadastrado no sistema.</div>
        )}
      </div>
    </div>
  )
}
