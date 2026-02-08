import { useState, useEffect } from 'react'
import { useAuth } from '../contexts/AuthContext'
import { apiGet, apiPatch, apiDelete } from '../api/client'
import { NEGOCIO_SINGULAR } from '../constants'
import type { BusinessItem } from '../types/api'
import { formatDateAndTime } from '../utils/format'
import Pagination from '../components/ui/Pagination'

type ServiceItem = { id: string; name: string }

type AppointmentItem = {
  id: string
  businessId: string
  serviceId: string
  clientName: string
  clientPhone: string | null
  clientEmail: string | null
  scheduledAt: string
  status: number
  notes: string | null
  createdAt: string
  updatedAt: string | null
}

const STATUS_LABELS: Record<number, string> = {
  0: 'Pendente',
  1: 'Confirmado',
  2: 'Cancelado',
  3: 'Concluído',
}

function getStatusBadgeClass(status: number): string {
  const base = 'inline-flex px-2 py-0.5 text-xs font-medium rounded-full '
  if (status === 0) return base + 'bg-amber-100 text-amber-800'
  if (status === 1) return base + 'bg-green-100 text-green-800'
  if (status === 2) return base + 'bg-red-100 text-red-800'
  return base + 'bg-gray-100 text-gray-800'
}

function StatusBadge({ status }: { status: number }) {
  return <span className={getStatusBadgeClass(status)}>{STATUS_LABELS[status] ?? status}</span>
}

function AppointmentActions({
  appointmentId,
  status,
  updatingId,
  onStatusChange,
  onRequestCancelByStatus,
  onRequestDelete,
  selectClassName = 'rounded-lg border border-gray-300 text-sm py-1.5 px-2 focus:ring-primary focus:border-primary',
}: {
  appointmentId: string
  status: number
  updatingId: string | null
  onStatusChange: (id: string, newStatus: number, cancellationReason?: string) => void
  onRequestCancelByStatus: (id: string) => void
  onRequestDelete: (id: string) => void
  selectClassName?: string
}) {
  return (
    <>
      <select
        value={status}
        onChange={(e) => {
          const newStatus = Number(e.target.value)
          if (newStatus === 2) onRequestCancelByStatus(appointmentId)
          else onStatusChange(appointmentId, newStatus)
        }}
        disabled={updatingId === appointmentId}
        className={selectClassName}
      >
        {[0, 1, 2, 3].map((s) => (
          <option key={s} value={s}>{STATUS_LABELS[s]}</option>
        ))}
      </select>
      <button type="button" onClick={() => onRequestDelete(appointmentId)} className="text-xs text-red-600 hover:underline" title="Excluir">Excluir</button>
    </>
  )
}

export default function Agendamentos() {
  const { token } = useAuth()
  const [businesses, setBusinesses] = useState<BusinessItem[]>([])
  const [selectedBusinessId, setSelectedBusinessId] = useState<string | null>(null)
  const [appointments, setAppointments] = useState<AppointmentItem[]>([])
  const [services, setServices] = useState<ServiceItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [dateFrom, setDateFrom] = useState(() => {
    const d = new Date()
    d.setHours(0, 0, 0, 0)
    return d.toISOString().slice(0, 10)
  })
  const [dateTo, setDateTo] = useState(() => {
    const d = new Date()
    d.setDate(d.getDate() + 30)
    return d.toISOString().slice(0, 10)
  })
  const [searchFilter, setSearchFilter] = useState('')
  const [page, setPage] = useState(1)
  const [totalCount, setTotalCount] = useState(0)
  const pageSize = 10
  const [updatingId, setUpdatingId] = useState<string | null>(null)
  const [cancelModal, setCancelModal] = useState<{ open: boolean; appointmentId: string; mode: 'status' | 'delete'; reason: string }>({ open: false, appointmentId: '', mode: 'status', reason: '' })

  useEffect(() => {
    if (!token) return
    apiGet<BusinessItem[]>('/api/business', token).then((res) => {
      if (res.ok) {
        setBusinesses(res.data)
        if (res.data.length > 0 && !selectedBusinessId) setSelectedBusinessId(res.data[0].id)
      }
    })
  }, [token])

  useEffect(() => {
    setPage(1)
  }, [searchFilter, dateFrom, dateTo, selectedBusinessId])

  useEffect(() => {
    if (!token || !selectedBusinessId) {
      setAppointments([])
      setServices([])
      setTotalCount(0)
      setLoading(false)
      return
    }
    setLoading(true)
    setError('')
    const from = new Date(dateFrom)
    from.setHours(0, 0, 0, 0)
    const to = new Date(dateTo)
    to.setHours(23, 59, 59, 999)
    const fromStr = from.toISOString()
    const toStr = to.toISOString()
    const searchParam = searchFilter.trim() ? `&search=${encodeURIComponent(searchFilter.trim())}` : ''
    const pageParam = `&page=${page}&pageSize=${pageSize}`
    Promise.all([
      apiGet<{ items: AppointmentItem[]; totalCount: number }>(
        `/api/appointments/by-business/${selectedBusinessId}?from=${encodeURIComponent(fromStr)}&to=${encodeURIComponent(toStr)}${searchParam}${pageParam}`,
        token
      ),
      apiGet<ServiceItem[]>(`/api/services/by-business/${selectedBusinessId}?includeInactive=true`, token),
    ]).then(([appRes, svcRes]) => {
      setLoading(false)
      if (appRes.ok) {
        setAppointments(appRes.data.items)
        setTotalCount(appRes.data.totalCount)
      } else {
        setError('Não foi possível carregar os agendamentos.')
      }
      if (svcRes.ok) setServices(svcRes.data)
    })
  }, [token, selectedBusinessId, dateFrom, dateTo, searchFilter, page])

  const serviceNameMap: Record<string, string> = {}
  services.forEach((s) => { serviceNameMap[s.id] = s.name })

  async function handleStatusChange(id: string, newStatus: number, cancellationReason?: string) {
    if (!token || !selectedBusinessId) return
    setUpdatingId(id)
    const body = cancellationReason !== undefined
      ? { status: newStatus, cancellationReason: cancellationReason.trim() || null }
      : { status: newStatus }
    const res = await apiPatch<{ status: number; cancellationReason?: string | null }, AppointmentItem>(
      `/api/appointments/${id}/status?businessId=${selectedBusinessId}`,
      body,
      token
    )
    setUpdatingId(null)
    if (res.ok) setAppointments((prev) => prev.map((a) => (a.id === id ? res.data : a)))
  }

  async function handleDelete(id: string) {
    if (!token || !selectedBusinessId) return
    const res = await apiDelete(`/api/appointments/${id}?businessId=${selectedBusinessId}`, token)
    if (res.ok) {
      setAppointments((prev) => prev.filter((a) => a.id !== id))
      setTotalCount((prev) => Math.max(0, prev - 1))
    }
  }

  function openCancelModal(appointmentId: string, mode: 'status' | 'delete') {
    setCancelModal({ open: true, appointmentId, mode, reason: '' })
  }

  function closeCancelModal() {
    setCancelModal((m) => ({ ...m, open: false }))
  }

  async function handleConfirmCancel() {
    const { appointmentId, mode, reason } = cancelModal
    if (!appointmentId) return
    closeCancelModal()
    if (mode === 'status') {
      await handleStatusChange(appointmentId, 2, reason)
    } else {
      await handleDelete(appointmentId)
    }
  }

  const selectedBusiness = businesses.find((b) => b.id === selectedBusinessId)

  return (
    <div className="p-4 sm:p-6 max-w-5xl">
      <h1 className="text-xl font-semibold text-gray-900 mb-2">Agendamentos</h1>
      <p className="text-gray-600 text-sm mb-6">
        Veja e gerencie os agendamentos da sua empresa. Altere o status ou remova conforme necessário.
      </p>

      {businesses.length === 0 ? (
        <div className="rounded-xl border border-amber-200 bg-amber-50 p-4 text-amber-800 text-sm">
          Nenhuma {NEGOCIO_SINGULAR} cadastrada. Cadastre sua {NEGOCIO_SINGULAR} em Configurações para ver agendamentos.
        </div>
      ) : (
        <>
          <div className="flex flex-wrap gap-4 mb-6">
            <div className="min-w-[200px]">
              <label htmlFor="businessSelect" className="block text-sm font-medium text-gray-700 mb-1">
                {NEGOCIO_SINGULAR.charAt(0).toUpperCase() + NEGOCIO_SINGULAR.slice(1)}
              </label>
              <select
                id="businessSelect"
                value={selectedBusinessId ?? ''}
                onChange={(e) => setSelectedBusinessId(e.target.value || null)}
                className="w-full rounded-xl border border-gray-300 px-4 py-2.5 text-gray-900 focus:ring-2 focus:ring-primary focus:border-primary"
              >
                {businesses.map((b) => (
                  <option key={b.id} value={b.id}>
                    {b.name}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label htmlFor="dateFrom" className="block text-sm font-medium text-gray-700 mb-1">De</label>
              <input
                id="dateFrom"
                type="date"
                value={dateFrom}
                onChange={(e) => setDateFrom(e.target.value)}
                className="rounded-xl border border-gray-300 px-4 py-2.5 text-gray-900 focus:ring-2 focus:ring-primary focus:border-primary"
              />
            </div>
            <div>
              <label htmlFor="dateTo" className="block text-sm font-medium text-gray-700 mb-1">Até</label>
              <input
                id="dateTo"
                type="date"
                value={dateTo}
                onChange={(e) => setDateTo(e.target.value)}
                className="rounded-xl border border-gray-300 px-4 py-2.5 text-gray-900 focus:ring-2 focus:ring-primary focus:border-primary"
              />
            </div>
            <div className="min-w-[200px] flex-1">
              <label htmlFor="searchFilter" className="block text-sm font-medium text-gray-700 mb-1">Cliente ou telefone</label>
              <input
                id="searchFilter"
                type="text"
                value={searchFilter}
                onChange={(e) => setSearchFilter(e.target.value)}
                placeholder="Filtrar por nome ou telefone"
                className="w-full rounded-xl border border-gray-300 px-4 py-2.5 text-gray-900 focus:ring-2 focus:ring-primary focus:border-primary"
              />
            </div>
          </div>

          {error && <p className="mb-4 text-sm text-red-600" role="alert">{error}</p>}

          {loading ? (
            <p className="text-gray-500 text-sm">Carregando agendamentos...</p>
          ) : appointments.length === 0 ? (
            <div className="rounded-xl border border-gray-200 bg-gray-50 p-6 text-center text-gray-600 text-sm">
              Nenhum agendamento no período para {selectedBusiness?.name ?? ''}.
            </div>
          ) : (
            <>
            {/* Mobile: cards empilhados */}
            <div className="md:hidden space-y-3">
              {appointments.map((a) => (
                <div key={a.id} className="rounded-xl border border-gray-200 bg-white p-4 shadow-sm">
                  <p className="text-sm font-semibold text-gray-900">
                    {formatDateAndTime(a.scheduledAt)}
                  </p>
                  <p className="mt-1 text-sm text-gray-800 font-medium">{a.clientName}</p>
                  {a.clientPhone && (
                    <p className="text-xs text-gray-500 break-all">{a.clientPhone}</p>
                  )}
                  <p className="mt-1 text-sm text-gray-600">{serviceNameMap[a.serviceId] ?? '—'}</p>
                  <div className="mt-3 flex flex-wrap items-center gap-2">
                    <StatusBadge status={a.status} />
                    <AppointmentActions
                      appointmentId={a.id}
                      status={a.status}
                      updatingId={updatingId}
                      onStatusChange={handleStatusChange}
                      onRequestCancelByStatus={(id) => openCancelModal(id, 'status')}
                      onRequestDelete={(id) => openCancelModal(id, 'delete')}
                    />
                  </div>
                </div>
              ))}
            </div>
            {/* Desktop: tabela */}
            <div className="hidden md:block overflow-x-auto rounded-xl border border-gray-200">
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Data / Hora</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Cliente</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Serviço</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
                    <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Ações</th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {appointments.map((a) => (
                    <tr key={a.id} className="hover:bg-gray-50">
                      <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-900">
                        {formatDateAndTime(a.scheduledAt)}
                      </td>
                      <td className="px-4 py-3 text-sm text-gray-900">
                        <span className="font-medium">{a.clientName}</span>
                        {a.clientPhone && <span className="block text-gray-500 text-xs">{a.clientPhone}</span>}
                      </td>
                      <td className="px-4 py-3 text-sm text-gray-700">{serviceNameMap[a.serviceId] ?? '—'}</td>
                      <td className="px-4 py-3 whitespace-nowrap">
                        <StatusBadge status={a.status} />
                      </td>
                      <td className="px-4 py-3 text-right">
                        <AppointmentActions
                          appointmentId={a.id}
                          status={a.status}
                          updatingId={updatingId}
                          onStatusChange={handleStatusChange}
                          onRequestCancelByStatus={(id) => openCancelModal(id, 'status')}
                          onRequestDelete={(id) => openCancelModal(id, 'delete')}
                          selectClassName="mr-2 rounded-lg border border-gray-300 text-sm py-1 px-2 focus:ring-primary focus:border-primary"
                        />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <Pagination
              page={page}
              totalCount={totalCount}
              pageSize={pageSize}
              onPageChange={setPage}
              className="mt-4"
            />
            </>
          )}
        </>
      )}

      {cancelModal.open && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50" aria-modal="true" role="dialog">
          <div className="bg-white rounded-xl shadow-lg max-w-md w-full p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-2">
              {cancelModal.mode === 'delete' ? 'Excluir agendamento?' : 'Cancelar agendamento?'}
            </h2>
            <p className="text-gray-600 text-sm mb-4">
              {cancelModal.mode === 'delete'
                ? 'O agendamento será removido da lista. O cliente não será notificado por e-mail.'
                : 'Tem certeza? O cliente será notificado por e-mail.'}
            </p>
            {cancelModal.mode === 'status' && (
              <>
                <label htmlFor="cancel-reason" className="block text-sm font-medium text-gray-700 mb-1">
                  Motivo do cancelamento (opcional)
                </label>
                <textarea
                  id="cancel-reason"
                  value={cancelModal.reason}
                  onChange={(e) => setCancelModal((m) => ({ ...m, reason: e.target.value }))}
                  placeholder="Ex.: horário indisponível, a pedido do cliente..."
                  rows={3}
                  className="w-full rounded-lg border border-gray-300 px-3 py-2 text-gray-900 text-sm focus:ring-2 focus:ring-primary focus:border-primary resize-none mb-4"
                />
              </>
            )}
            <div className="mt-6 flex gap-3 justify-end">
              <button
                type="button"
                onClick={closeCancelModal}
                className="px-4 py-2 rounded-lg border border-gray-300 text-gray-700 bg-white hover:bg-gray-50 text-sm font-medium"
              >
                Não
              </button>
              <button
                type="button"
                onClick={handleConfirmCancel}
                className="px-4 py-2 rounded-lg bg-red-600 text-white hover:bg-red-700 text-sm font-medium"
              >
                {cancelModal.mode === 'delete' ? 'Sim, excluir' : 'Sim, cancelar'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
