import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'
import { apiGet } from '../api/client'
import type { BusinessItem } from '../types/api'
import type { DashboardSummary, EarningsDetailResponse } from '../types/dashboard'
import { formatDateAndTime, formatCurrency } from '../utils/format'
import { ROUTES, NEGOCIO_SINGULAR } from '../constants'
import { buildCsv, downloadCsv } from '../utils/exportCsv'
import ValueVisibilityToggle from '../components/ui/ValueVisibilityToggle'
import Pagination from '../components/ui/Pagination'
import { useDisplayCurrency } from '../contexts/ValuesVisibilityContext'

export default function Ganhos() {
  const { token } = useAuth()
  const displayCurrency = useDisplayCurrency()
  const [businesses, setBusinesses] = useState<BusinessItem[]>([])
  const [selectedBusinessId, setSelectedBusinessId] = useState<string | null>(null)
  const [summary, setSummary] = useState<Pick<DashboardSummary, 'ganhosDoMes'> | null>(null)
  const [earningsDetail, setEarningsDetail] = useState<EarningsDetailResponse['items']>([])
  const [loading, setLoading] = useState(true)
  const [page, setPage] = useState(1)
  const pageSize = 10
  const now = new Date()
  const [exportFrom, setExportFrom] = useState(() => new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10))
  const [exportTo, setExportTo] = useState(() => new Date(now.getFullYear(), now.getMonth() + 1, 0).toISOString().slice(0, 10))
  const [exporting, setExporting] = useState(false)

  useEffect(() => {
    if (!token) return
    apiGet<BusinessItem[]>('/api/business', token).then((res) => {
      if (res.ok && res.data.length > 0) {
        setBusinesses(res.data)
        if (!selectedBusinessId) setSelectedBusinessId(res.data[0].id)
      } else setLoading(false)
    })
  }, [token])

  useEffect(() => {
    if (!token || !selectedBusinessId) {
      setLoading(false)
      return
    }
    setLoading(true)
    Promise.all([
      apiGet<DashboardSummary>(`/api/dashboard/summary?businessId=${selectedBusinessId}`, token),
      apiGet<EarningsDetailResponse>(`/api/dashboard/earnings-detail?businessId=${selectedBusinessId}`, token),
    ]).then(([summaryRes, detailRes]) => {
      setLoading(false)
      if (summaryRes.ok) setSummary(summaryRes.data)
      else setSummary(null)
      if (detailRes.ok && Array.isArray(detailRes.data.items)) setEarningsDetail(detailRes.data.items)
      else setEarningsDetail([])
    })
  }, [token, selectedBusinessId])

  useEffect(() => {
    setPage(1)
  }, [selectedBusinessId])

  const hasClinic = !!selectedBusinessId
  const totalDetailCount = earningsDetail.length
  const pagedDetail = earningsDetail.slice((page - 1) * pageSize, page * pageSize)
  const showData = hasClinic && !loading

  async function handleExportExcel() {
    if (!token || !selectedBusinessId) return
    const from = exportFrom.trim()
    const to = exportTo.trim()
    if (!from || !to || from > to) return
    setExporting(true)
    try {
      const res = await apiGet<EarningsDetailResponse>(
        `/api/dashboard/earnings-detail?businessId=${selectedBusinessId}&from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`,
        token
      )
      if (!res.ok) return
      const items = Array.isArray(res.data?.items) ? res.data.items : []
      const rows: Record<string, string>[] = items.map((item) => ({
        'Data e hora': formatDateAndTime(item.scheduledAt),
        Cliente: item.clientName,
        Serviço: item.serviceName,
        Valor: formatCurrency(item.price),
      }))
      const csv = buildCsv(rows)
      downloadCsv(csv, `ganhos-${from}-${to}.csv`)
    } finally {
      setExporting(false)
    }
  }

  return (
    <div className="p-4 sm:p-6 max-w-4xl">
      <div className="flex flex-wrap items-start justify-between gap-4 mb-6">
        <div>
          <h1 className="text-xl font-semibold text-gray-900 mb-2">Ganhos</h1>
          <p className="text-gray-600 text-sm">
            Acompanhe os ganhos por mês. Consideram apenas agendamentos <strong>concluídos</strong> com serviço que tenha preço.
          </p>
        </div>
        <ValueVisibilityToggle className="flex-shrink-0" />
      </div>

      {!hasClinic ? (
        <div className="bg-amber-50 border border-amber-200 rounded-xl px-4 py-3 text-amber-800">
          <p className="text-sm">
            Nenhuma {NEGOCIO_SINGULAR} cadastrada.{' '}
            <Link to={ROUTES.CONFIGURACOES} className="text-primary font-medium hover:underline">
              Cadastre sua {NEGOCIO_SINGULAR} nas configurações
            </Link>
            {' '}para ver os ganhos.
          </p>
        </div>
      ) : (
        <>
          {businesses.length > 1 && (
            <div className="mb-6">
              <label htmlFor="ganhos-business" className="block text-sm font-medium text-gray-700 mb-1">Empresa</label>
              <select
                id="ganhos-business"
                value={selectedBusinessId ?? ''}
                onChange={(e) => setSelectedBusinessId(e.target.value || null)}
                className="w-full max-w-xs rounded-xl border border-gray-300 px-4 py-3 bg-white text-gray-900 focus:ring-2 focus:ring-primary focus:border-primary"
              >
                {businesses.map((b) => (
                  <option key={b.id} value={b.id}>{b.name}</option>
                ))}
              </select>
            </div>
          )}

          {loading ? (
            <p className="text-gray-500 text-sm">Carregando ganhos...</p>
          ) : (
            <>
              <div className="bg-white rounded-xl border border-gray-200 p-4 sm:p-6 shadow-sm mb-6 flex flex-wrap items-center justify-between gap-3">
                <div>
                  <h2 className="text-base font-semibold text-gray-900 mb-1">Ganhos do mês atual</h2>
                  <p className="text-2xl sm:text-3xl font-bold text-primary">
                    {showData && summary != null ? displayCurrency(summary.ganhosDoMes) : displayCurrency(0)}
                  </p>
                </div>
                <ValueVisibilityToggle className="flex-shrink-0" />
              </div>

              <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden mb-6">
                <div className="px-4 py-3 border-b border-gray-200">
                  <h2 className="text-base sm:text-lg font-semibold text-gray-900">Quais foram os ganhos</h2>
                  <p className="text-sm text-gray-500 mt-0.5">Atendimentos concluídos no mês atual que geraram receita</p>
                </div>
                <div>
                  {earningsDetail.length === 0 ? (
                    <p className="px-4 py-8 text-gray-500 text-sm text-center">Nenhum atendimento concluído com valor neste mês.</p>
                  ) : (
                    <>
                      <ul className="divide-y divide-gray-100">
                        {pagedDetail.map((item, i) => (
                          <li key={(page - 1) * pageSize + i} className="px-4 py-3 flex flex-wrap items-center justify-between gap-2 sm:gap-4">
                            <div className="min-w-0">
                              <p className="font-medium text-gray-900 truncate">{item.serviceName}</p>
                              <p className="text-sm text-gray-600 truncate">{item.clientName}</p>
                              <p className="text-xs text-gray-500 mt-0.5">
                                {formatDateAndTime(item.scheduledAt)}
                              </p>
                            </div>
                            <p className="font-semibold text-primary flex-shrink-0 tabular-nums">{displayCurrency(item.price)}</p>
                          </li>
                        ))}
                      </ul>
                      <Pagination
                        page={page}
                        totalCount={totalDetailCount}
                        pageSize={pageSize}
                        onPageChange={setPage}
                        className="mt-4 border-t border-gray-100 pt-4"
                      />
                    </>
                  )}
                </div>
              </div>


              <div className="mt-6 p-4 rounded-xl border border-gray-200 bg-gray-50">
                <h3 className="text-sm font-semibold text-gray-900 mb-2">Relatório de ganhos</h3>
                <p className="text-gray-600 text-sm mb-3">Escolha o intervalo e exporte os ganhos em planilha (Excel/CSV).</p>
                <div className="flex flex-wrap items-end gap-3">
                  <div>
                    <label htmlFor="export-from" className="block text-xs font-medium text-gray-600 mb-1">De</label>
                    <input
                      id="export-from"
                      type="date"
                      value={exportFrom}
                      onChange={(e) => setExportFrom(e.target.value)}
                      className="rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 focus:ring-2 focus:ring-primary focus:border-primary"
                    />
                  </div>
                  <div>
                    <label htmlFor="export-to" className="block text-xs font-medium text-gray-600 mb-1">Até</label>
                    <input
                      id="export-to"
                      type="date"
                      value={exportTo}
                      onChange={(e) => setExportTo(e.target.value)}
                      className="rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 focus:ring-2 focus:ring-primary focus:border-primary"
                    />
                  </div>
                  <button
                    type="button"
                    onClick={handleExportExcel}
                    disabled={exporting || !exportFrom || !exportTo || exportFrom > exportTo}
                    className="min-h-[40px] px-4 py-2 rounded-lg bg-primary text-white text-sm font-medium hover:bg-primary/90 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    {exporting ? 'Exportando...' : 'Exportar Excel'}
                  </button>
                </div>
              </div>

              <p className="mt-4 text-sm text-gray-500">
                Os valores vêm dos agendamentos marcados como <strong>Concluído</strong>. Para atualizar o status, use a página{' '}
                <Link to={ROUTES.AGENDAMENTOS} className="text-primary font-medium hover:underline">Agendamentos</Link>.
              </p>
            </>
          )}
        </>
      )}
    </div>
  )
}
