import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'
import { apiGet } from '../api/client'
import type { BusinessItem } from '../types/api'
import type { DashboardSummary } from '../types/dashboard'
import { ROUTES, NEGOCIO_SINGULAR, FALLBACK_USER_NAME } from '../constants'
import { getFirstName } from '../utils/format'
import MonthlyBarChart from '../components/MonthlyBarChart'
import ValueVisibilityToggle from '../components/ui/ValueVisibilityToggle'
import { useDisplayCurrency } from '../contexts/ValuesVisibilityContext'

export default function Dashboard() {
  const { token, user } = useAuth()
  const displayCurrency = useDisplayCurrency()
  const [businesses, setBusinesses] = useState<BusinessItem[]>([])
  const [summary, setSummary] = useState<DashboardSummary | null>(null)
  const [loading, setLoading] = useState(true)
  const [selectedBusinessId, setSelectedBusinessId] = useState<string | null>(null)

  useEffect(() => {
    if (!token) return
    ;(async () => {
      const res = await apiGet<BusinessItem[]>('/api/business', token)
      if (res.ok && res.data.length > 0) {
        setBusinesses(res.data)
        setSelectedBusinessId(res.data[0].id)
      } else {
        setLoading(false)
      }
    })()
  }, [token])

  useEffect(() => {
    if (!token || !selectedBusinessId) {
      setLoading(false)
      return
    }
    setLoading(true)
    apiGet<DashboardSummary>(`/api/dashboard/summary?businessId=${selectedBusinessId}`, token).then((res) => {
      if (res.ok) setSummary(res.data)
      setLoading(false)
    })
  }, [token, selectedBusinessId])

  const monthLabels = ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez']
  const currentMonthIndex = new Date().getMonth()
  const hasClinic = !!selectedBusinessId
  const showSummary = hasClinic && !loading
  const chartItems = monthLabels.map((label, i) => ({
    label,
    value: showSummary && i === currentMonthIndex ? Number(summary?.ganhosDoMes ?? 0) : 0,
  }))

  return (
    <div className="space-y-4 sm:space-y-6 min-w-0 w-full max-w-full">
      {/* Mobile: saudação abaixo do header, como na imagem */}
      <h1 className="text-lg font-bold text-gray-900 sm:hidden">
        Bem-vindo, {getFirstName(user?.name, FALLBACK_USER_NAME)}!
      </h1>

      {loading && !hasClinic ? (
        <div className="flex items-center justify-center py-12 text-gray-500">Carregando...</div>
      ) : (
        <>
          {!hasClinic && (
            <div className="bg-amber-50 border border-amber-200 rounded-xl px-4 py-3 text-amber-800">
              <p className="text-sm">
                Nenhuma {NEGOCIO_SINGULAR} cadastrada.{' '}
                <Link to={ROUTES.CONFIGURACOES} className="text-primary font-medium hover:underline">
                  Cadastre sua {NEGOCIO_SINGULAR} nas configurações
                </Link>
                {' '}para ver o resumo do dashboard.
              </p>
            </div>
          )}

          {businesses.length > 1 && (
            <div className="flex justify-end">
              <select
                value={selectedBusinessId ?? ''}
                onChange={(e) => setSelectedBusinessId(e.target.value || null)}
                className="rounded-lg border border-gray-300 px-3 py-2 text-sm focus:ring-2 focus:ring-primary focus:border-primary"
              >
                {businesses.map((b) => (
                  <option key={b.id} value={b.id}>{b.name}</option>
                ))}
              </select>
            </div>
          )}

          {/* Cards - mobile 2x2 como na imagem; desktop 4 em linha */}
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-2 sm:gap-4 min-w-0">
            <div className="bg-white rounded-xl border border-gray-200 p-3 sm:p-4 shadow-sm min-h-[72px] sm:min-h-0 flex flex-col justify-center min-w-0 overflow-hidden">
              <p className="text-xs sm:text-sm font-medium text-gray-600">Próximos Agendamentos</p>
              <p className="text-xl sm:text-2xl font-bold text-primary mt-0.5 sm:mt-1">{showSummary ? (summary?.proximosAgendamentosCount ?? 0) : 0}</p>
            </div>
            <div className="bg-white rounded-xl border border-gray-200 p-3 sm:p-4 shadow-sm min-h-[72px] sm:min-h-0 flex flex-col justify-center min-w-0 overflow-hidden">
              <p className="text-xs sm:text-sm font-medium text-gray-600">Clientes Hoje</p>
              <p className="text-xl sm:text-2xl font-bold text-primary mt-0.5 sm:mt-1">{showSummary ? (summary?.clientesHojeCount ?? 0) : 0}</p>
            </div>
            <div className="bg-white rounded-xl border border-gray-200 p-3 sm:p-4 shadow-sm min-h-[72px] sm:min-h-0 flex flex-col justify-center min-w-0 overflow-hidden">
              <p className="text-xs sm:text-sm font-medium text-gray-600">Faltas</p>
              <p className="text-xl sm:text-2xl font-bold text-red-600 mt-0.5 sm:mt-1">{showSummary ? (summary?.faltasCount ?? 0) : 0}</p>
            </div>
            <div className="bg-white rounded-xl border border-gray-200 p-3 sm:p-4 shadow-sm min-h-[72px] sm:min-h-0 flex flex-col justify-center min-w-0 overflow-hidden">
              <p className="text-xs sm:text-sm font-medium text-gray-600">Ganhos do Mês</p>
              <div className="flex items-center gap-1 mt-0.5 sm:mt-1 flex-wrap">
                <p className="text-lg sm:text-2xl font-bold text-primary leading-tight break-words">
                  {showSummary && summary != null ? displayCurrency(summary.ganhosDoMes) : displayCurrency(0)}
                </p>
                <ValueVisibilityToggle className="flex-shrink-0" />
              </div>
            </div>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 sm:gap-6 min-w-0">
            {/* Agenda do Dia - mobile: texto quebra sem overflow/scroll */}
            <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden min-w-0">
              <div className="px-4 py-3 border-b border-gray-200 min-w-0">
                <h2 className="text-base sm:text-lg font-semibold text-gray-900 truncate">Agenda do Dia</h2>
              </div>
              <ul className="p-3 sm:p-4 space-y-2 overflow-hidden min-w-0">
                {!showSummary || (summary?.agendaDoDia?.length ?? 0) === 0 ? (
                  <li className="px-4 py-6 text-center text-gray-500 text-sm">Nenhum agendamento hoje.</li>
                ) : (
                  summary?.agendaDoDia?.map((item, i) => (
                    <li key={i} className="px-4 py-3 rounded-lg bg-gray-100 text-gray-800 min-w-0 overflow-hidden">
                      <p className="text-sm sm:text-base break-words">
                        <span className="font-medium text-primary">{item.hora}</span>
                        {' '}
                        <span className="text-gray-700">{item.servico}</span>
                        {' – '}
                        <span className="text-gray-600">{item.cliente}</span>
                      </p>
                    </li>
                  ))
                )}
              </ul>
            </div>

            {/* Próximos Agendamentos - mobile: texto quebra sem overflow/scroll */}
            <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden min-w-0">
              <div className="px-4 py-3 border-b border-gray-200 flex items-center justify-between gap-2 min-w-0">
                <h2 className="text-base sm:text-lg font-semibold text-gray-900 min-w-0 truncate">Próximos Agendamentos</h2>
                <Link to={ROUTES.AGENDAMENTOS} className="text-sm text-primary hover:underline font-medium flex-shrink-0 whitespace-nowrap">Mais +</Link>
              </div>
              <ul className="divide-y divide-gray-100 overflow-hidden min-w-0">
                {!showSummary || (summary?.proximosAgendamentos?.length ?? 0) === 0 ? (
                  <li className="px-4 py-6 text-center text-gray-500 text-sm">Nenhum agendamento próximo.</li>
                ) : (
                  summary?.proximosAgendamentos?.map((item, i) => {
                    const isHoje = item.data.toLowerCase() === 'hoje'
                    const isAmanha = item.data.toLowerCase().includes('amanhã') || item.data.toLowerCase().includes('amanha')
                    const clockColor = isHoje ? 'text-red-500' : isAmanha ? 'text-amber-500' : 'text-green-600'
                    return (
                      <li key={i} className="px-4 py-3 flex items-start sm:items-center gap-2 sm:gap-3 text-sm min-w-0 overflow-hidden">
                        <svg className={`w-5 h-5 flex-shrink-0 mt-0.5 sm:mt-0 ${clockColor}`} fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden>
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                        </svg>
                        <span className="text-gray-700 min-w-0 break-words flex-1">
                          {item.data} {item.hora} – {item.cliente} – {item.servico}
                        </span>
                      </li>
                    )
                  })
                )}
              </ul>
            </div>
          </div>

          {/* Relatório Financeiro - mobile: gráfico encolhe para caber, sem scroll horizontal */}
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden min-w-0">
            <div className="px-4 py-3 border-b border-gray-200">
              <h2 className="text-base sm:text-lg font-semibold text-gray-900">Relatório Financeiro</h2>
              <p className="text-sm text-primary font-medium mt-0.5">Ganhos Mensais</p>
            </div>
            <div className="p-3 sm:p-6 overflow-hidden">
              <MonthlyBarChart
                items={chartItems}
                formatValue={displayCurrency}
                minBarMax={8000}
              />
              <p className="text-sm text-gray-500 mt-4 text-center flex items-center justify-center gap-1 flex-wrap">
                Mês atual: {showSummary && summary != null ? displayCurrency(summary.ganhosDoMes) : displayCurrency(0)}
                <ValueVisibilityToggle className="flex-shrink-0" />
              </p>
            </div>
          </div>
        </>
      )}
    </div>
  )
}
