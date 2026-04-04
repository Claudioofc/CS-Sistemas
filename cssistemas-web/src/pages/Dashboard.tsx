import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { motion } from 'framer-motion'
import { useAuth } from '../contexts/AuthContext'
import { apiGet } from '../api/client'
import type { BusinessItem } from '../types/api'
import type { DashboardSummary } from '../types/dashboard'
import { ROUTES, NEGOCIO_SINGULAR, FALLBACK_USER_NAME } from '../constants'
import { getFirstName } from '../utils/format'
import MonthlyBarChart from '../components/MonthlyBarChart'
import ValueVisibilityToggle from '../components/ui/ValueVisibilityToggle'
import { useDisplayCurrency } from '../contexts/ValuesVisibilityContext'
import { useCountUp } from '../hooks/useCountUp'

function SkeletonCard() {
  return (
    <div className="bg-white rounded-xl border border-gray-200 p-3 sm:p-4 shadow-sm min-h-[72px] flex flex-col justify-center gap-2 animate-pulse">
      <div className="h-3 w-2/3 rounded bg-gray-200" />
      <div className="h-7 w-1/2 rounded bg-gray-200" />
    </div>
  )
}

function SkeletonList() {
  return (
    <div className="space-y-2 p-4 animate-pulse">
      {[1, 2, 3].map(i => <div key={i} className="h-10 rounded-lg bg-gray-100" />)}
    </div>
  )
}

function KpiCard({ label, value, color = 'text-primary', index }: { label: string; value: string | number; color?: string; index: number }) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: index * 0.08, duration: 0.4 }}
      whileHover={{ y: -3, boxShadow: '0 8px 24px 0 rgba(0,0,0,0.10)' }}
      className="bg-white rounded-xl border border-gray-200 p-3 sm:p-4 shadow-sm min-h-[72px] sm:min-h-0 flex flex-col justify-center min-w-0 overflow-hidden cursor-default transition-shadow"
    >
      <p className="text-xs sm:text-sm font-medium text-gray-600">{label}</p>
      <p className={`text-xl sm:text-2xl font-bold mt-0.5 sm:mt-1 ${color}`}>{value}</p>
    </motion.div>
  )
}

export default function Dashboard() {
  const { token, user } = useAuth()
  const displayCurrency = useDisplayCurrency()
  const [businesses, setBusinesses] = useState<BusinessItem[]>([])
  const [summary, setSummary] = useState<DashboardSummary | null>(null)
  const [loading, setLoading] = useState(true)
  const [selectedBusinessId, setSelectedBusinessId] = useState<string | null>(null)

  const proximosCount = useCountUp(summary?.proximosAgendamentosCount ?? 0)
  const clientesHojeCount = useCountUp(summary?.clientesHojeCount ?? 0)
  const faltasCount = useCountUp(summary?.faltasCount ?? 0)

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
      <h1 className="text-lg font-bold text-gray-900 sm:hidden">
        Bem-vindo, {getFirstName(user?.name, FALLBACK_USER_NAME)}!
      </h1>

      {/* Cards — skeleton enquanto carrega */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-2 sm:gap-4 min-w-0">
        {loading ? (
          [0, 1, 2, 3].map(i => <SkeletonCard key={i} />)
        ) : (
          <>
            <KpiCard index={0} label="Próximos Agendamentos" value={showSummary ? proximosCount : 0} />
            <KpiCard index={1} label="Clientes Hoje" value={showSummary ? clientesHojeCount : 0} />
            <KpiCard index={2} label="Faltas" value={showSummary ? faltasCount : 0} color="text-red-600" />
            <motion.div
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.24, duration: 0.4 }}
              whileHover={{ y: -3, boxShadow: '0 8px 24px 0 rgba(0,0,0,0.10)' }}
              className="bg-white rounded-xl border border-gray-200 p-3 sm:p-4 shadow-sm min-h-[72px] sm:min-h-0 flex flex-col justify-center min-w-0 overflow-hidden cursor-default transition-shadow"
            >
              <p className="text-xs sm:text-sm font-medium text-gray-600">Ganhos do Mês</p>
              <div className="flex items-center gap-1 mt-0.5 sm:mt-1 flex-wrap">
                <p className="text-lg sm:text-2xl font-bold text-primary leading-tight break-words">
                  {showSummary && summary != null ? displayCurrency(summary.ganhosDoMes) : displayCurrency(0)}
                </p>
                <ValueVisibilityToggle className="flex-shrink-0" />
              </div>
            </motion.div>
          </>
        )}
      </div>

      {!loading && !hasClinic && (
        <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="bg-amber-50 border border-amber-200 rounded-xl px-4 py-3 text-amber-800">
          <p className="text-sm">
            Nenhuma {NEGOCIO_SINGULAR} cadastrada.{' '}
            <Link to={ROUTES.CONFIGURACOES} className="text-primary font-medium hover:underline">
              Cadastre sua {NEGOCIO_SINGULAR} nas configurações
            </Link>
            {' '}para ver o resumo do dashboard.
          </p>
        </motion.div>
      )}

      {!loading && businesses.length > 1 && (
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

      {!loading && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 sm:gap-6 min-w-0">
          {/* Agenda do Dia */}
          <motion.div
            initial={{ opacity: 0, y: 16 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.32, duration: 0.4 }}
            className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden min-w-0"
          >
            <div className="px-4 py-3 border-b border-gray-200 min-w-0">
              <h2 className="text-base sm:text-lg font-semibold text-gray-900 truncate">Agenda do Dia</h2>
            </div>
            {loading ? <SkeletonList /> : (
              <ul className="p-3 sm:p-4 space-y-2 overflow-hidden min-w-0">
                {!showSummary || (summary?.agendaDoDia?.length ?? 0) === 0 ? (
                  <li className="px-4 py-6 text-center text-gray-500 text-sm">Nenhum agendamento hoje.</li>
                ) : (
                  summary?.agendaDoDia?.map((item, i) => (
                    <motion.li
                      key={i}
                      initial={{ opacity: 0, x: -8 }}
                      animate={{ opacity: 1, x: 0 }}
                      transition={{ delay: 0.36 + i * 0.06 }}
                      className="px-4 py-3 rounded-lg bg-gray-100 text-gray-800 min-w-0 overflow-hidden"
                    >
                      <p className="text-sm sm:text-base break-words">
                        <span className="font-medium text-primary">{item.hora}</span>
                        {' '}<span className="text-gray-700">{item.servico}</span>
                        {' – '}<span className="text-gray-600">{item.cliente}</span>
                      </p>
                    </motion.li>
                  ))
                )}
              </ul>
            )}
          </motion.div>

          {/* Próximos Agendamentos */}
          <motion.div
            initial={{ opacity: 0, y: 16 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.4, duration: 0.4 }}
            className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden min-w-0"
          >
            <div className="px-4 py-3 border-b border-gray-200 flex items-center justify-between gap-2 min-w-0">
              <h2 className="text-base sm:text-lg font-semibold text-gray-900 min-w-0 truncate">Próximos Agendamentos</h2>
              <Link to={ROUTES.AGENDAMENTOS} className="text-sm text-primary hover:underline font-medium flex-shrink-0 whitespace-nowrap">Mais +</Link>
            </div>
            {loading ? <SkeletonList /> : (
              <ul className="divide-y divide-gray-100 overflow-hidden min-w-0">
                {!showSummary || (summary?.proximosAgendamentos?.length ?? 0) === 0 ? (
                  <li className="px-4 py-6 text-center text-gray-500 text-sm">Nenhum agendamento próximo.</li>
                ) : (
                  summary?.proximosAgendamentos?.map((item, i) => {
                    const isHoje = item.data.toLowerCase() === 'hoje'
                    const isAmanha = item.data.toLowerCase().includes('amanhã') || item.data.toLowerCase().includes('amanha')
                    const clockColor = isHoje ? 'text-red-500' : isAmanha ? 'text-amber-500' : 'text-green-600'
                    return (
                      <motion.li
                        key={i}
                        initial={{ opacity: 0, x: -8 }}
                        animate={{ opacity: 1, x: 0 }}
                        transition={{ delay: 0.44 + i * 0.06 }}
                        className="px-4 py-3 flex items-start sm:items-center gap-2 sm:gap-3 text-sm min-w-0 overflow-hidden"
                      >
                        <svg className={`w-5 h-5 flex-shrink-0 mt-0.5 sm:mt-0 ${clockColor}`} fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden>
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                        </svg>
                        <span className="text-gray-700 min-w-0 break-words flex-1">
                          {item.data} {item.hora} – {item.cliente} – {item.servico}
                        </span>
                      </motion.li>
                    )
                  })
                )}
              </ul>
            )}
          </motion.div>
        </div>
      )}

      {/* Relatório Financeiro */}
      {!loading && (
        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.48, duration: 0.4 }}
          className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden min-w-0"
        >
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
        </motion.div>
      )}
    </div>
  )
}
