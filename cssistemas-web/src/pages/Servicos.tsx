import { useState, useEffect } from 'react'
import { useAuth } from '../contexts/AuthContext'
import { apiGet, apiPostWithAuth, apiPut, apiDelete } from '../api/client'
import { NEGOCIO_SINGULAR } from '../constants'
import InputWithIcon from '../components/ui/InputWithIcon'
import type { BusinessItem, EmployeeItem } from '../types/api'

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
  const [employees, setEmployees] = useState<EmployeeItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  // Form: novo / edição
  const [formOpen, setFormOpen] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [formName, setFormName] = useState('')
  const [formDurationHours, setFormDurationHours] = useState(0)
  const [formDurationMinutes, setFormDurationMinutes] = useState(30)
  const [formPrice, setFormPrice] = useState('')
  // Preços por funcionário: employeeId → valor digitado (vazio = sem preço personalizado)
  const [empPrices, setEmpPrices] = useState<Record<string, string>>({})
  const [formSaving, setFormSaving] = useState(false)
  const [formError, setFormError] = useState('')
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null)

  // Form: novo funcionário
  const [empFormOpen, setEmpFormOpen] = useState(false)
  const [newEmpName, setNewEmpName] = useState('')
  const [newEmpRole, setNewEmpRole] = useState('')
  const [empSaving, setEmpSaving] = useState(false)
  const [empError, setEmpError] = useState('')

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
      setEmployees([])
      setLoading(false)
      return
    }
    setLoading(true)
    Promise.all([
      apiGet<ServiceItem[]>(`/api/services/by-business/${selectedBusinessId}`, token),
      apiGet<EmployeeItem[]>(`/api/business/${selectedBusinessId}/employees`, token),
    ]).then(([svcRes, empRes]) => {
      setLoading(false)
      if (svcRes.ok) { setServices(svcRes.data); setError('') }
      else setError('Não foi possível carregar os serviços.')
      if (empRes.ok) setEmployees(empRes.data.filter(e => e.isActive))
    })
  }, [token, selectedBusinessId])

  const openNewForm = () => {
    setEditingId(null)
    setFormName('')
    setFormDurationHours(0)
    setFormDurationMinutes(30)
    setFormPrice('')
    setEmpPrices({})
    setFormError('')
    setFormOpen(true)
  }

  const openEditForm = (s: ServiceItem) => {
    setEditingId(s.id)
    setFormName(s.name)
    setFormDurationHours(Math.floor(s.durationMinutes / 60))
    setFormDurationMinutes(s.durationMinutes % 60)
    setFormPrice(s.price != null ? String(s.price) : '')
    // Preenche preços dos funcionários a partir dos dados já carregados
    const prices: Record<string, string> = {}
    employees.forEach(emp => {
      const sp = emp.servicePrices?.find(p => p.serviceId === s.id)
      if (sp != null) prices[emp.id] = sp.price.toFixed(2).replace('.', ',')
    })
    setEmpPrices(prices)
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
    setEmpPrices({})
    setFormError('')
  }

  async function handleSubmitService(e: React.FormEvent) {
    e.preventDefault()
    if (!token || !selectedBusinessId) return
    const name = formName.trim()
    const durationMinutes = formDurationHours * 60 + formDurationMinutes
    if (durationMinutes < 1) { setFormError('Duração deve ser de pelo menos 1 minuto.'); return }
    const priceStr = formPrice.replace(',', '.').trim()
    const price = priceStr === '' ? null : parseFloat(priceStr)

    setFormSaving(true)
    setFormError('')
    const body = { businessId: selectedBusinessId, name, durationMinutes, price }

    let savedId = editingId

    if (editingId) {
      const res = await apiPut<typeof body, ServiceItem>(
        `/api/services/${editingId}?businessId=${selectedBusinessId}`, body, token
      )
      if (!res.ok) {
        setFormSaving(false)
        setFormError((res.error && ('mensagem' in res.error ? res.error.mensagem : res.error.message)) ?? 'Erro ao atualizar serviço.')
        return
      }
      setServices(prev => prev.map(s => s.id === editingId ? res.data : s))
    } else {
      const res = await apiPostWithAuth<typeof body, ServiceItem>('/api/services', body, token)
      if (!res.ok) {
        setFormSaving(false)
        setFormError((res.error && ('mensagem' in res.error ? res.error.mensagem : res.error.message)) ?? 'Erro ao cadastrar serviço.')
        return
      }
      setServices(prev => [...prev, res.data])
      savedId = res.data.id
    }

    // Salva preços por funcionário
    if (savedId && employees.length > 0) {
      const employeePrices = Object.entries(empPrices)
        .map(([employeeId, val]) => ({ employeeId, price: parseFloat(val.replace(',', '.')) }))
        .filter(p => !isNaN(p.price) && p.price >= 0)
      await apiPut<typeof employeePrices, unknown>(
        `/api/services/${savedId}/employee-prices?businessId=${selectedBusinessId}`,
        employeePrices,
        token
      )
      // Atualiza employees localmente com os novos preços
      setEmployees(prev => prev.map(emp => {
        const newPrice = employeePrices.find(p => p.employeeId === emp.id)
        const filtered = (emp.servicePrices ?? []).filter(sp => sp.serviceId !== savedId)
        const updated = newPrice ? [...filtered, { serviceId: savedId!, price: newPrice.price }] : filtered
        return { ...emp, servicePrices: updated }
      }))
    }

    setFormSaving(false)
    closeForm()
  }

  const openEmpForm = () => {
    setNewEmpName('')
    setNewEmpRole('')
    setEmpError('')
    setEmpFormOpen(true)
  }

  const closeEmpForm = () => {
    setEmpFormOpen(false)
    setNewEmpName('')
    setNewEmpRole('')
    setEmpError('')
  }

  async function handleAddEmployee(e: React.FormEvent) {
    e.preventDefault()
    if (!token || !selectedBusinessId || !newEmpName.trim()) return
    setEmpSaving(true)
    setEmpError('')
    const res = await apiPostWithAuth<{ name: string; role?: string }, EmployeeItem>(
      `/api/business/${selectedBusinessId}/employees`,
      { name: newEmpName.trim(), role: newEmpRole.trim() || undefined },
      token
    )
    setEmpSaving(false)
    if (res.ok) {
      setEmployees(prev => [...prev, res.data])
      closeEmpForm()
    } else {
      const err = res.error && ('mensagem' in res.error ? res.error.mensagem : res.error.message)
      setEmpError(err ?? 'Erro ao adicionar funcionário.')
    }
  }

  async function handleDelete(id: string) {
    if (!token || !selectedBusinessId) return
    const res = await apiDelete(`/api/services/${id}?businessId=${selectedBusinessId}`, token)
    if (res.ok) { setServices(prev => prev.filter(s => s.id !== id)); setDeleteConfirmId(null) }
  }

  const selectedBusiness = businesses.find(b => b.id === selectedBusinessId)

  return (
    <div className="p-4 sm:p-6 max-w-4xl">
      <h1 className="text-xl font-semibold text-gray-900 mb-2">Serviços</h1>
      <p className="text-gray-600 text-sm mb-6">
        Cadastre os serviços e defina o preço que cada funcionário cobra.
      </p>

      {businesses.length === 0 ? (
        <div className="rounded-xl border border-amber-200 bg-amber-50 p-4 text-amber-800 text-sm">
          Nenhuma {NEGOCIO_SINGULAR} cadastrada. Cadastre sua {NEGOCIO_SINGULAR} em Configurações para gerenciar serviços.
        </div>
      ) : (
        <>
          <div className="mb-6">
            <label htmlFor="businessSelect" className="block text-sm font-medium text-gray-700 mb-1">Empresa</label>
            <select
              id="businessSelect"
              value={selectedBusinessId ?? ''}
              onChange={(e) => { setSelectedBusinessId(e.target.value || null); closeForm() }}
              className="w-full rounded-xl border border-gray-300 px-4 py-3 bg-white text-gray-900 focus:ring-2 focus:ring-primary focus:border-primary"
            >
              {businesses.map(b => <option key={b.id} value={b.id}>{b.name}</option>)}
            </select>
          </div>

          {error && <p className="mb-4 text-sm text-red-600" role="alert">{error}</p>}

          {!formOpen && !empFormOpen && (
            <div className="mb-6 flex flex-wrap gap-3">
              <button type="button" onClick={openNewForm} className="min-h-[44px] px-4 py-2 rounded-xl bg-primary text-white font-medium hover:bg-primary/90">
                Novo serviço
              </button>
              {employees.length > 0 && (
                <button type="button" onClick={openEmpForm} className="min-h-[44px] px-4 py-2 rounded-xl bg-primary text-white font-medium hover:bg-primary/90">
                  Novo funcionário
                </button>
              )}
            </div>
          )}

          {empFormOpen && (
            <form onSubmit={handleAddEmployee} className="mb-6 p-4 rounded-xl border border-gray-200 bg-gray-50 space-y-4">
              <h2 className="text-lg font-medium text-gray-900">Novo funcionário</h2>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <label htmlFor="empName" className="block text-sm font-medium text-gray-700 mb-1">Nome *</label>
                  <input
                    id="empName"
                    type="text"
                    value={newEmpName}
                    onChange={(e) => setNewEmpName(e.target.value)}
                    placeholder="Nome do funcionário"
                    disabled={empSaving}
                    className="w-full px-4 py-3 min-h-[48px] text-base border border-gray-300 rounded-xl focus:ring-2 focus:ring-primary focus:border-primary outline-none transition"
                    required
                  />
                </div>
                <div>
                  <label htmlFor="empRole" className="block text-sm font-medium text-gray-700 mb-1">Cargo / especialidade</label>
                  <input
                    id="empRole"
                    type="text"
                    value={newEmpRole}
                    onChange={(e) => setNewEmpRole(e.target.value)}
                    placeholder="Ex.: Barbeiro, Manicure..."
                    disabled={empSaving}
                    className="w-full px-4 py-3 min-h-[48px] text-base border border-gray-300 rounded-xl focus:ring-2 focus:ring-primary focus:border-primary outline-none transition"
                  />
                </div>
              </div>
              {empError && <p className="text-sm text-red-600" role="alert">{empError}</p>}
              <div className="flex gap-2">
                <button type="submit" disabled={empSaving || !newEmpName.trim()} className="min-h-[44px] px-5 py-2 rounded-xl bg-primary text-white font-medium hover:bg-primary/90 disabled:opacity-70">
                  {empSaving ? 'Salvando...' : 'Cadastrar'}
                </button>
                <button type="button" onClick={closeEmpForm} className="min-h-[44px] px-4 py-2 rounded-xl border border-gray-300 text-gray-700 hover:bg-gray-50">
                  Cancelar
                </button>
              </div>
            </form>
          )}

          {formOpen && (
            <form onSubmit={handleSubmitService} className="mb-6 p-4 rounded-xl border border-gray-200 bg-gray-50 space-y-4">
              <h2 className="text-lg font-medium text-gray-900">{editingId ? 'Editar serviço' : 'Novo serviço'}</h2>

              {/* Nome */}
              <div>
                <label htmlFor="formName" className="block text-sm font-medium text-gray-700 mb-1">Nome *</label>
                <input
                  id="formName"
                  type="text"
                  value={formName}
                  onChange={(e) => setFormName(e.target.value)}
                  placeholder="Ex.: Corte de cabelo, Manicure..."
                  disabled={formSaving}
                  className="w-full px-4 py-3 min-h-[48px] text-base border border-gray-300 rounded-xl focus:ring-2 focus:ring-primary focus:border-primary outline-none transition"
                />
              </div>

              {/* Duração */}
              <div>
                <span className="block text-sm font-medium text-gray-700 mb-2">Duração *</span>
                <div className="flex flex-wrap items-center gap-3">
                  <div className="flex items-center gap-2">
                    <input
                      type="number" min={0} max={23} value={formDurationHours}
                      onChange={(e) => setFormDurationHours(Math.max(0, Math.min(23, parseInt(e.target.value, 10) || 0)))}
                      disabled={formSaving}
                      className="w-16 px-3 py-3 text-center text-base border border-gray-300 rounded-xl focus:ring-2 focus:ring-primary focus:border-primary outline-none [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
                    />
                    <span className="text-gray-600 text-sm">h</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <input
                      type="number" min={0} max={59} value={formDurationMinutes}
                      onChange={(e) => setFormDurationMinutes(Math.max(0, Math.min(59, parseInt(e.target.value, 10) || 0)))}
                      disabled={formSaving}
                      className="w-16 px-3 py-3 text-center text-base border border-gray-300 rounded-xl focus:ring-2 focus:ring-primary focus:border-primary outline-none [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
                    />
                    <span className="text-gray-600 text-sm">min</span>
                  </div>
                </div>
                <p className="mt-1 text-xs text-gray-500">Total: {formDurationHours * 60 + formDurationMinutes} min</p>
              </div>

              {/* Preço padrão */}
              <div className="max-w-xs">
                <label htmlFor="formPrice" className="block text-sm font-medium text-gray-700 mb-1">Preço padrão <span className="font-normal text-gray-400">(opcional)</span></label>
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

              {/* Preços por funcionário */}
              {employees.length > 0 && (
                <div>
                  <p className="text-sm font-medium text-gray-700 mb-1">
                    Preço por funcionário <span className="font-normal text-gray-400">(deixe vazio para usar o preço padrão)</span>
                  </p>
                  <div className="space-y-2">
                    {employees.map(emp => (
                      <div key={emp.id} className="flex items-center gap-3 bg-white border border-gray-200 rounded-xl px-4 py-2.5">
                        <div className="flex-1 min-w-0">
                          <span className="text-sm font-medium text-gray-800">{emp.name}</span>
                          {emp.role && <span className="ml-2 text-xs text-gray-400">{emp.role}</span>}
                        </div>
                        <div className="flex items-center gap-1.5 shrink-0">
                          <span className="text-sm text-gray-500">R$</span>
                          <input
                            type="text"
                            inputMode="decimal"
                            value={empPrices[emp.id] ?? ''}
                            onChange={e => setEmpPrices(prev => ({ ...prev, [emp.id]: e.target.value.replace(/[^\d,.]/g, '') }))}
                            placeholder="—"
                            disabled={formSaving}
                            className="w-24 rounded-lg border border-gray-300 px-2 py-1.5 text-sm text-right focus:ring-1 focus:ring-primary focus:border-primary"
                          />
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {formError && <p className="text-sm text-red-600" role="alert">{formError}</p>}

              <div className="flex gap-2">
                <button type="submit" disabled={formSaving} className="min-h-[44px] px-5 py-2 rounded-xl bg-primary text-white font-medium hover:bg-primary/90 disabled:opacity-70">
                  {formSaving ? 'Salvando...' : editingId ? 'Salvar' : 'Cadastrar'}
                </button>
                <button type="button" onClick={closeForm} className="min-h-[44px] px-4 py-2 rounded-xl border border-gray-300 text-gray-700 hover:bg-gray-50">
                  Cancelar
                </button>
              </div>
            </form>
          )}

          {loading ? (
            <p className="text-gray-500 text-sm">Carregando serviços...</p>
          ) : services.length === 0 ? (
            <p className="text-gray-500 text-sm">
              Nenhum serviço cadastrado para {selectedBusiness?.name ?? ''}. Clique em "Novo serviço" para adicionar.
            </p>
          ) : (
            <ul className="space-y-2">
              {services.map(s => {
                // Funcionários com preço configurado para este serviço
                const empWithPrice = employees
                  .map(emp => ({ emp, sp: emp.servicePrices?.find(p => p.serviceId === s.id) }))
                  .filter(x => x.sp != null) as { emp: EmployeeItem; sp: { serviceId: string; price: number } }[]

                return (
                  <li key={s.id} className="rounded-xl border border-gray-200 bg-gray-50">
                    <div className="flex flex-wrap items-center justify-between gap-2 px-4 py-3">
                      <div className="flex-1 min-w-0">
                        <span className="font-medium text-gray-900">{s.name}</span>
                        <span className="text-gray-500 text-sm ml-2">
                          {formatDuration(s.durationMinutes)} · {formatPrice(s.price)}
                        </span>
                        {empWithPrice.length > 0 && (
                          <div className="mt-1 flex flex-wrap gap-1.5">
                            {empWithPrice.map(({ emp, sp }) => (
                              <span key={emp.id} className="inline-flex items-center gap-1 text-xs bg-primary/8 text-primary px-2 py-0.5 rounded-full border border-primary/20">
                                {emp.name} · {formatPrice(sp.price)}
                              </span>
                            ))}
                          </div>
                        )}
                      </div>
                      <div className="flex gap-2 shrink-0">
                        <button type="button" onClick={() => openEditForm(s)} className="text-sm text-primary hover:underline">
                          Editar
                        </button>
                        {deleteConfirmId === s.id ? (
                          <>
                            <span className="text-sm text-gray-500">Excluir?</span>
                            <button type="button" onClick={() => handleDelete(s.id)} className="text-sm text-red-600 hover:underline">Sim</button>
                            <button type="button" onClick={() => setDeleteConfirmId(null)} className="text-sm text-gray-600 hover:underline">Não</button>
                          </>
                        ) : (
                          <button type="button" onClick={() => setDeleteConfirmId(s.id)} className="text-sm text-red-600 hover:underline">
                            Excluir
                          </button>
                        )}
                      </div>
                    </div>
                  </li>
                )
              })}
            </ul>
          )}
        </>
      )}
    </div>
  )
}
