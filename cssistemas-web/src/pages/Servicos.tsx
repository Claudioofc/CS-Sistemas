import { useState, useEffect } from 'react'
import { useAuth } from '../contexts/AuthContext'
import { apiGet, apiPostWithAuth, apiPut, apiDelete } from '../api/client'
import { NEGOCIO_SINGULAR } from '../constants'
import InputWithIcon from '../components/ui/InputWithIcon'
import type { BusinessItem } from '../types/api'

type ServiceItem = {
  id: string
  businessId: string
  name: string
  durationMinutes: number
  price: number | null
  isActive: boolean
  createdAt: string
  updatedAt: string | null
}

const IconCurrency = () => (
  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden>
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
  </svg>
)

function formatPrice(value: number | null): string {
  if (value == null) return '—'
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value)
}

function formatDuration(min: number): string {
  if (min < 60) return `${min} min`
  const h = Math.floor(min / 60)
  const m = min % 60
  return m ? `${h}h ${m}min` : `${h}h`
}

export default function Servicos() {
  const { token } = useAuth()
  const [businesses, setBusinesses] = useState<BusinessItem[]>([])
  const [selectedBusinessId, setSelectedBusinessId] = useState<string | null>(null)
  const [services, setServices] = useState<ServiceItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  // Form: novo / edição
  const [formOpen, setFormOpen] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [formName, setFormName] = useState('')
  const [formDurationHours, setFormDurationHours] = useState(0)
  const [formDurationMinutes, setFormDurationMinutes] = useState(30)
  const [formPrice, setFormPrice] = useState('')
  const [formSaving, setFormSaving] = useState(false)
  const [formError, setFormError] = useState('')
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null)

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
    if (!token || !selectedBusinessId) {
      setServices([])
      setLoading(false)
      return
    }
    setLoading(true)
    apiGet<ServiceItem[]>(`/api/services/by-business/${selectedBusinessId}`, token).then((res) => {
      setLoading(false)
      if (res.ok) {
        setServices(res.data)
        setError('')
      } else setError('Não foi possível carregar os serviços.')
    })
  }, [token, selectedBusinessId])

  const openNewForm = () => {
    setEditingId(null)
    setFormName('')
    setFormDurationHours(0)
    setFormDurationMinutes(30)
    setFormPrice('')
    setFormError('')
    setFormOpen(true)
  }

  const openEditForm = (s: ServiceItem) => {
    setEditingId(s.id)
    setFormName(s.name)
    setFormDurationHours(Math.floor(s.durationMinutes / 60))
    setFormDurationMinutes(s.durationMinutes % 60)
    setFormPrice(s.price != null ? String(s.price) : '')
    setFormError('')
    setFormOpen(true)
  }

  const closeForm = () => {
    setFormOpen(false)
    setEditingId(null)
    setFormName('')
    setFormDurationHours(0)
    setFormDurationMinutes(30)
    setFormPrice('')
    setFormError('')
  }

  async function handleSubmitService(e: React.FormEvent) {
    e.preventDefault()
    if (!token || !selectedBusinessId) return
    const name = formName.trim()
    const durationMinutes = formDurationHours * 60 + formDurationMinutes
    if (durationMinutes < 1) {
      setFormError('Duração deve ser de pelo menos 1 minuto.')
      return
    }
    const priceStr = formPrice.replace(',', '.').trim()
    const price = priceStr === '' ? null : parseFloat(priceStr)

    setFormSaving(true)
    setFormError('')
    const body = { businessId: selectedBusinessId, name, durationMinutes, price }

    if (editingId) {
      const res = await apiPut<typeof body, ServiceItem>(
        `/api/services/${editingId}?businessId=${selectedBusinessId}`,
        body,
        token
      )
      setFormSaving(false)
      if (res.ok) {
        setServices((prev) => prev.map((s) => (s.id === editingId ? res.data : s)))
        closeForm()
        return
      }
      const err = res.error && ('mensagem' in res.error ? res.error.mensagem : res.error.message)
      setFormError(err ?? 'Erro ao atualizar serviço.')
      return
    }

    const res = await apiPostWithAuth<typeof body, ServiceItem>('/api/services', body, token)
    setFormSaving(false)
    if (res.ok) {
      setServices((prev) => [...prev, res.data])
      closeForm()
      return
    }
    const err = res.error && ('mensagem' in res.error ? res.error.mensagem : res.error.message)
    setFormError(err ?? 'Erro ao cadastrar serviço.')
  }

  async function handleDelete(id: string) {
    if (!token || !selectedBusinessId) return
    const res = await apiDelete(`/api/services/${id}?businessId=${selectedBusinessId}`, token)
    if (res.ok) {
      setServices((prev) => prev.filter((s) => s.id !== id))
      setDeleteConfirmId(null)
    }
  }

  const selectedBusiness = businesses.find((b) => b.id === selectedBusinessId)

  return (
    <div className="p-4 sm:p-6 max-w-4xl">
      <h1 className="text-xl font-semibold text-gray-900 mb-2">Serviços</h1>
      <p className="text-gray-600 text-sm mb-6">
        Cadastre os serviços oferecidos pela sua empresa.
      </p>

      {businesses.length === 0 ? (
        <div className="rounded-xl border border-amber-200 bg-amber-50 p-4 text-amber-800 text-sm">
          Nenhuma {NEGOCIO_SINGULAR} cadastrada. Cadastre sua {NEGOCIO_SINGULAR} em Configurações para gerenciar serviços.
        </div>
      ) : (
        <>
          <div className="mb-6">
            <label htmlFor="businessSelect" className="block text-sm font-medium text-gray-700 mb-1">
              Empresa
            </label>
            <select
              id="businessSelect"
              value={selectedBusinessId ?? ''}
              onChange={(e) => setSelectedBusinessId(e.target.value || null)}
              className="w-full rounded-xl border border-gray-300 px-4 py-3 bg-white text-gray-900 focus:ring-2 focus:ring-primary focus:border-primary"
            >
                {businesses.map((b) => (
                  <option key={b.id} value={b.id}>
                    {b.name}
                  </option>
                ))}
            </select>
          </div>

          {error && <p className="mb-4 text-sm text-red-600" role="alert">{error}</p>}

          {!formOpen ? (
            <button
              type="button"
              onClick={openNewForm}
              className="mb-6 min-h-[44px] px-4 py-2 rounded-xl bg-primary text-white font-medium hover:bg-primary/90"
            >
              Novo serviço
            </button>
          ) : (
            <form onSubmit={handleSubmitService} className="mb-6 p-4 rounded-xl border border-gray-200 bg-gray-50 space-y-4">
              <h2 className="text-lg font-medium text-gray-900">
                {editingId ? 'Editar serviço' : 'Novo serviço'}
              </h2>
              <div>
                <label htmlFor="formName" className="block text-sm font-medium text-gray-700 mb-1">Nome *</label>
                <input
                  id="formName"
                  type="text"
                  value={formName}
                  onChange={(e) => setFormName(e.target.value)}
                  placeholder="Serviços prestados pela sua empresa"
                  disabled={formSaving}
                  className="w-full px-4 py-3 min-h-[48px] text-base border border-gray-300 rounded-xl focus:ring-2 focus:ring-primary focus:border-primary outline-none transition touch-manipulation"
                />
              </div>
              <div>
                <span className="block text-sm font-medium text-gray-700 mb-2">Duração *</span>
                <div className="flex flex-wrap items-center gap-3">
                  <div className="flex items-center gap-2">
                    <label htmlFor="formDurationHours" className="text-sm text-gray-600 sr-only">Horas</label>
                    <input
                      id="formDurationHours"
                      type="number"
                      min={0}
                      max={23}
                      value={formDurationHours}
                      onChange={(e) => setFormDurationHours(Math.max(0, Math.min(23, parseInt(e.target.value, 10) || 0)))}
                      disabled={formSaving}
                      className="w-16 px-3 py-3 text-center text-base border border-gray-300 rounded-xl focus:ring-2 focus:ring-primary focus:border-primary outline-none transition [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
                    />
                    <span className="text-gray-600 text-sm">h</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <label htmlFor="formDurationMinutes" className="text-sm text-gray-600 sr-only">Minutos</label>
                    <input
                      id="formDurationMinutes"
                      type="number"
                      min={0}
                      max={59}
                      value={formDurationMinutes}
                      onChange={(e) => setFormDurationMinutes(Math.max(0, Math.min(59, parseInt(e.target.value, 10) || 0)))}
                      disabled={formSaving}
                      className="w-16 px-3 py-3 text-center text-base border border-gray-300 rounded-xl focus:ring-2 focus:ring-primary focus:border-primary outline-none transition [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
                    />
                    <span className="text-gray-600 text-sm">min</span>
                  </div>
                </div>
                <p className="mt-1 text-xs text-gray-500">
                  Total: {formDurationHours * 60 + formDurationMinutes} min
                </p>
              </div>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label htmlFor="formPrice" className="block text-sm font-medium text-gray-700 mb-1">Preço (opcional)</label>
                  <InputWithIcon
                    id="formPrice"
                    type="text"
                    inputMode="decimal"
                    icon={<IconCurrency />}
                    value={formPrice}
                    onChange={(e) => setFormPrice(e.target.value.replace(/[^\d,.]/g, ''))}
                    placeholder="0,00"
                    disabled={formSaving}
                  />
                </div>
              </div>
              {formError && <p className="text-sm text-red-600" role="alert">{formError}</p>}
              <div className="flex gap-2">
                <button
                  type="submit"
                  disabled={formSaving}
                  className="min-h-[44px] px-4 py-2 rounded-xl bg-primary text-white font-medium hover:bg-primary/90 disabled:opacity-70"
                >
                  {formSaving ? 'Salvando...' : editingId ? 'Salvar' : 'Cadastrar'}
                </button>
                <button
                  type="button"
                  onClick={closeForm}
                  className="min-h-[44px] px-4 py-2 rounded-xl border border-gray-300 text-gray-700 hover:bg-gray-50"
                >
                  Cancelar
                </button>
              </div>
            </form>
          )}

          {loading ? (
            <p className="text-gray-500 text-sm">Carregando serviços...</p>
          ) : services.length === 0 ? (
            <p className="text-gray-500 text-sm">
              Nenhum serviço cadastrado para {selectedBusiness?.name ?? ''}. Clique em &quot;Novo serviço&quot; para adicionar.
            </p>
          ) : (
            <ul className="space-y-2">
              {services.map((s) => (
                <li
                  key={s.id}
                  className="flex flex-wrap items-center justify-between gap-2 px-4 py-3 rounded-xl border border-gray-200 bg-gray-50"
                >
                  <div>
                    <span className="font-medium text-gray-900">{s.name}</span>
                    <span className="text-gray-500 text-sm ml-2">
                      {formatDuration(s.durationMinutes)} · {formatPrice(s.price)}
                    </span>
                  </div>
                  <div className="flex gap-2">
                    <button
                      type="button"
                      onClick={() => openEditForm(s)}
                      className="text-sm text-primary hover:underline"
                    >
                      Editar
                    </button>
                    {deleteConfirmId === s.id ? (
                      <>
                        <span className="text-sm text-gray-500">Excluir?</span>
                        <button
                          type="button"
                          onClick={() => handleDelete(s.id)}
                          className="text-sm text-red-600 hover:underline"
                        >
                          Sim
                        </button>
                        <button
                          type="button"
                          onClick={() => setDeleteConfirmId(null)}
                          className="text-sm text-gray-600 hover:underline"
                        >
                          Não
                        </button>
                      </>
                    ) : (
                      <button
                        type="button"
                        onClick={() => setDeleteConfirmId(s.id)}
                        className="text-sm text-red-600 hover:underline"
                      >
                        Excluir
                      </button>
                    )}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </>
      )}
    </div>
  )
}
