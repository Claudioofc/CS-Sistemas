import { useState, useEffect } from 'react'
import { useAuth } from '../contexts/AuthContext'
import { apiGet, apiPostWithAuth, apiPut, apiDelete } from '../api/client'
import { NEGOCIO_SINGULAR } from '../constants'
import InputWithIcon from '../components/ui/InputWithIcon'
import type { BusinessItem } from '../types/api'
import { useSystemUsersSummary } from '../hooks/useSystemUsersSummary'
import SystemClientsSummaryCards from '../components/SystemClientsSummaryCards'

/** Item de negócio retornado pelo endpoint admin (inclui dono). */
type BusinessItemWithOwner = BusinessItem & { ownerName?: string }

/** Endpoints de negócios e clientes (DRY: um lugar para admin vs usuário). */
function getBusinessesEndpoint(isAdmin: boolean): string {
  return isAdmin ? '/api/admin/businesses' : '/api/business'
}
function getClientsByBusinessEndpoint(businessId: string, isAdmin: boolean): string {
  return isAdmin ? `/api/admin/clients/by-business/${businessId}` : `/api/clients/by-business/${businessId}`
}

type ClientItem = {
  id: string
  businessId: string
  name: string
  phone: string | null
  email: string | null
  notes: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string | null
}

const IconUser = () => (
  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden>
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
  </svg>
)
const IconPhone = () => (
  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden>
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" />
  </svg>
)
const IconMail = () => (
  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden>
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
  </svg>
)

export default function Clientes() {
  const { token, user } = useAuth()
  const [businesses, setBusinesses] = useState<BusinessItemWithOwner[]>([])
  const [selectedBusinessId, setSelectedBusinessId] = useState<string | null>(null)
  const [clients, setClients] = useState<ClientItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const isAdmin = user?.isAdmin === true
  const { total: totalSystemClients, premiumCount, gratuitosCount } = useSystemUsersSummary(token, isAdmin && !!token)

  const [formOpen, setFormOpen] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [formName, setFormName] = useState('')
  const [formPhone, setFormPhone] = useState('')
  const [formEmail, setFormEmail] = useState('')
  const [formNotes, setFormNotes] = useState('')
  const [formSaving, setFormSaving] = useState(false)
  const [formError, setFormError] = useState('')
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null)

  useEffect(() => {
    if (!token) return
    apiGet<BusinessItemWithOwner[]>(getBusinessesEndpoint(isAdmin), token).then((res) => {
      if (res.ok) {
        setBusinesses(res.data)
        if (res.data.length > 0 && !selectedBusinessId) setSelectedBusinessId(res.data[0].id)
      }
    })
  }, [token, isAdmin])

  useEffect(() => {
    if (!token || !selectedBusinessId) {
      setClients([])
      setLoading(false)
      return
    }
    setLoading(true)
    apiGet<ClientItem[]>(getClientsByBusinessEndpoint(selectedBusinessId, isAdmin), token).then((res) => {
      setLoading(false)
      if (res.ok) {
        setClients(res.data)
        setError('')
      } else setError('Não foi possível carregar os clientes.')
    })
  }, [token, selectedBusinessId, isAdmin])

  const openNewForm = () => {
    setEditingId(null)
    setFormName('')
    setFormPhone('')
    setFormEmail('')
    setFormNotes('')
    setFormError('')
    setFormOpen(true)
  }

  const openEditForm = (c: ClientItem) => {
    setEditingId(c.id)
    setFormName(c.name)
    setFormPhone(c.phone ?? '')
    setFormEmail(c.email ?? '')
    setFormNotes(c.notes ?? '')
    setFormError('')
    setFormOpen(true)
  }

  const closeForm = () => {
    setFormOpen(false)
    setEditingId(null)
    setFormName('')
    setFormPhone('')
    setFormEmail('')
    setFormNotes('')
    setFormError('')
  }

  async function handleSubmitClient(e: React.FormEvent) {
    e.preventDefault()
    if (!token || !selectedBusinessId) return
    const name = formName.trim()
    const phone = formPhone.trim() || null
    const email = formEmail.trim() || null
    const notes = formNotes.trim() || null

    setFormSaving(true)
    setFormError('')
    const body = { businessId: selectedBusinessId, name, phone, email, notes }

    if (editingId) {
      const res = await apiPut<typeof body, ClientItem>(
        `/api/clients/${editingId}?businessId=${selectedBusinessId}`,
        body,
        token
      )
      setFormSaving(false)
      if (res.ok) {
        setClients((prev) => prev.map((c) => (c.id === editingId ? res.data : c)))
        closeForm()
        return
      }
      const err = res.error && ('mensagem' in res.error ? res.error.mensagem : res.error.message)
      setFormError(err ?? 'Erro ao atualizar cliente.')
      return
    }

    const res = await apiPostWithAuth<typeof body, ClientItem>('/api/clients', body, token)
    setFormSaving(false)
    if (res.ok) {
      setClients((prev) => [...prev, res.data])
      closeForm()
      return
    }
    const err = res.error && ('mensagem' in res.error ? res.error.mensagem : res.error.message)
    setFormError(err ?? 'Erro ao cadastrar cliente.')
  }

  async function handleDelete(id: string) {
    if (!token || !selectedBusinessId) return
    const res = await apiDelete(`/api/clients/${id}?businessId=${selectedBusinessId}`, token)
    if (res.ok) {
      setClients((prev) => prev.filter((c) => c.id !== id))
      setDeleteConfirmId(null)
    }
  }

  const selectedBusiness = businesses.find((b) => b.id === selectedBusinessId)

  return (
    <div className="p-4 sm:p-6 max-w-4xl">
      <h1 className="text-xl font-semibold text-gray-900 mb-2">Clientes</h1>
      <p className="text-gray-600 text-sm mb-6">
        {isAdmin
          ? 'Clientes ativos por clínica (somente leitura para administradores).'
          : 'Cadastre os clientes da sua empresa (nome, telefone, e-mail, observações).'}
      </p>

      {isAdmin && (
        <SystemClientsSummaryCards
          total={totalSystemClients}
          premiumCount={premiumCount}
          gratuitosCount={gratuitosCount}
          totalSubtitle="Clientes ativos (premium + gratuitos)"
        />
      )}

      {businesses.length === 0 ? (
        <div className="rounded-xl border border-amber-200 bg-amber-50 p-4 text-amber-800 text-sm">
          {isAdmin
            ? 'Nenhum negócio cadastrado no sistema.'
            : `Nenhuma ${NEGOCIO_SINGULAR} cadastrada. Cadastre sua ${NEGOCIO_SINGULAR} em Configurações para gerenciar clientes.`}
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
                  {b.name}{isAdmin && b.ownerName ? ` (${b.ownerName})` : ''}
                </option>
              ))}
            </select>
          </div>

          {error && <p className="mb-4 text-sm text-red-600" role="alert">{error}</p>}

          {!isAdmin && !formOpen && clients.length === 0 ? (
            <button
              type="button"
              onClick={openNewForm}
              className="mb-6 min-h-[44px] px-4 py-2 rounded-xl bg-primary text-white font-medium hover:bg-primary/90"
            >
              Novo cliente
            </button>
          ) : !isAdmin && formOpen ? (
            <form onSubmit={handleSubmitClient} className="mb-6 p-4 rounded-xl border border-gray-200 bg-gray-50 space-y-4">
              <h2 className="text-lg font-medium text-gray-900">
                {editingId ? 'Editar cliente' : 'Novo cliente'}
              </h2>
              <div>
                <label htmlFor="formName" className="block text-sm font-medium text-gray-700 mb-1">Nome *</label>
                <InputWithIcon
                  id="formName"
                  type="text"
                  icon={<IconUser />}
                  value={formName}
                  onChange={(e) => setFormName(e.target.value)}
                  placeholder="Nome completo"
                  disabled={formSaving}
                />
              </div>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label htmlFor="formPhone" className="block text-sm font-medium text-gray-700 mb-1">Telefone</label>
                  <InputWithIcon
                    id="formPhone"
                    type="tel"
                    icon={<IconPhone />}
                    value={formPhone}
                    onChange={(e) => setFormPhone(e.target.value)}
                    placeholder="(00) 00000-0000"
                    disabled={formSaving}
                  />
                </div>
                <div>
                  <label htmlFor="formEmail" className="block text-sm font-medium text-gray-700 mb-1">E-mail</label>
                  <InputWithIcon
                    id="formEmail"
                    type="email"
                    icon={<IconMail />}
                    value={formEmail}
                    onChange={(e) => setFormEmail(e.target.value)}
                    placeholder="email@exemplo.com"
                    disabled={formSaving}
                  />
                </div>
              </div>
              <div>
                <label htmlFor="formNotes" className="block text-sm font-medium text-gray-700 mb-1">Observações</label>
                <textarea
                  id="formNotes"
                  value={formNotes}
                  onChange={(e) => setFormNotes(e.target.value)}
                  placeholder="Observações sobre o paciente"
                  disabled={formSaving}
                  rows={2}
                  className="w-full px-4 py-3 rounded-xl border border-gray-300 bg-white text-gray-900 focus:ring-2 focus:ring-primary focus:border-primary"
                />
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
          ) : null}

          {loading ? (
            <p className="text-gray-500 text-sm">Carregando clientes...</p>
          ) : clients.length === 0 ? (
            <p className="text-gray-500 text-sm">
              Nenhum cliente cadastrado para {selectedBusiness?.name ?? ''}. Clique em &quot;Novo cliente&quot; para adicionar.
            </p>
          ) : (
            <ul className="space-y-2">
              {clients.map((c) => (
                <li
                  key={c.id}
                  className="flex flex-wrap items-center justify-between gap-2 px-4 py-3 rounded-xl border border-gray-200 bg-gray-50"
                >
                  <div>
                    <span className="font-medium text-gray-900">{c.name}</span>
                    {(c.phone || c.email) && (
                      <span className="text-gray-500 text-sm ml-2">
                        {[c.phone, c.email].filter(Boolean).join(' · ')}
                      </span>
                    )}
                  </div>
                  {!isAdmin && (
                    <div className="flex gap-2">
                      <button
                        type="button"
                        onClick={() => openEditForm(c)}
                        className="text-sm text-primary hover:underline"
                      >
                        Editar
                      </button>
                      {deleteConfirmId === c.id ? (
                        <>
                          <span className="text-sm text-gray-500">Excluir?</span>
                          <button
                            type="button"
                            onClick={() => handleDelete(c.id)}
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
                          onClick={() => setDeleteConfirmId(c.id)}
                          className="text-sm text-red-600 hover:underline"
                        >
                          Excluir
                        </button>
                      )}
                    </div>
                  )}
                </li>
              ))}
            </ul>
          )}
        </>
      )}
    </div>
  )
}
