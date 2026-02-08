import { useState, useEffect } from 'react'
import { useParams, Link } from 'react-router-dom'
import { apiGet, apiPost } from '../api/client'
import { APP_NAME } from '../constants'
import { formatDateAndTime, formatTimeOnly } from '../utils/format'

type PublicBusiness = { id: string; name: string; publicSlug: string | null }
type PublicService = { id: string; name: string; durationMinutes: number; price: number | null }
type SlotWithAvailability = { scheduledAtUtc: string; available: boolean }

export default function AgendarPublico() {
  const { slug } = useParams<{ slug: string }>()
  const [business, setBusiness] = useState<PublicBusiness | null>(null)
  const [services, setServices] = useState<PublicService[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const [selectedServiceId, setSelectedServiceId] = useState<string>('')
  const [selectedDate, setSelectedDate] = useState('')
  const [slots, setSlots] = useState<SlotWithAvailability[]>([])
  const [loadingSlots, setLoadingSlots] = useState(false)

  const [clientName, setClientName] = useState('')
  const [clientPhone, setClientPhone] = useState('')
  const [clientEmail, setClientEmail] = useState('')
  const [selectedSlotUtc, setSelectedSlotUtc] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState('')
  const [success, setSuccess] = useState(false)
  const [confirmedScheduledAt, setConfirmedScheduledAt] = useState<string | null>(null)

  useEffect(() => {
    if (!slug) return
    setLoading(true)
    setError('')
    apiGet<PublicBusiness>(`/api/public/booking/${slug}`, null).then((res) => {
      setLoading(false)
      if (res.ok) setBusiness(res.data)
      else setError('Link de agendamento não encontrado.')
    })
  }, [slug])

  useEffect(() => {
    if (!slug || !business) return
    apiGet<PublicService[]>(`/api/public/booking/${slug}/services`, null).then((res) => {
      if (res.ok) setServices(res.data)
    })
  }, [slug, business])

  useEffect(() => {
    if (!slug || !selectedServiceId || !selectedDate) {
      setSlots([])
      return
    }
    setLoadingSlots(true)
    setSlots([])
    setSelectedSlotUtc('')
    const dateParam = selectedDate
    apiGet<SlotWithAvailability[]>(`/api/public/booking/${slug}/slots?serviceId=${selectedServiceId}&date=${dateParam}`, null).then((res) => {
      setLoadingSlots(false)
      if (res.ok) setSlots(res.data)
    })
  }, [slug, selectedServiceId, selectedDate])

  const minDate = new Date().toISOString().slice(0, 10)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!slug || !selectedServiceId || !selectedSlotUtc || !clientName.trim()) return
    setSubmitting(true)
    setSubmitError('')
    const body = {
      serviceId: selectedServiceId,
      clientName: clientName.trim(),
      scheduledAt: selectedSlotUtc,
      clientPhone: clientPhone.trim() || null,
      clientEmail: clientEmail.trim() || null,
    }
    const res = await apiPost<typeof body, { id: string; clientName: string; scheduledAt: string }>(
      `/api/public/booking/${slug}/appointments`,
      body
    )
    setSubmitting(false)
    if (res.ok) {
      setConfirmedScheduledAt(res.data.scheduledAt)
      setSuccess(true)
      return
    }
    const err = res.error && ('mensagem' in res.error ? res.error.mensagem : res.error.message)
    setSubmitError(err ?? 'Não foi possível confirmar o agendamento.')
  }

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
        <p className="text-gray-600">Carregando...</p>
      </div>
    )
  }
  if (error || !business) {
    return (
      <div className="min-h-screen bg-gray-50 flex flex-col items-center justify-center p-4">
        <p className="text-red-600 mb-4">{error || 'Link inválido.'}</p>
        <Link to="/" className="text-primary hover:underline">Voltar ao início</Link>
      </div>
    )
  }

  if (success) {
    const dataHora = confirmedScheduledAt ? formatDateAndTime(confirmedScheduledAt) : null
    return (
      <div className="min-h-screen bg-gray-50 flex flex-col items-center justify-center p-4">
        <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 max-w-md w-full text-center">
          <h1 className="text-xl font-semibold text-gray-900 mb-2">Agendamento confirmado</h1>
          <p className="text-gray-600 mb-4">
            {dataHora ? (
              <>
                Seu horário foi reservado para o dia <strong>{dataHora}</strong>. Obrigado! Em breve {business.name} poderá entrar em contato se necessário.
              </>
            ) : (
              <>
                Obrigado! Em breve {business.name} entrará em contato para confirmar.
              </>
            )}
          </p>
          {clientEmail && (
            <p className="text-sm text-gray-500 mb-4">
              Enviamos um e-mail de confirmação com um link para cancelar o agendamento, se precisar.
            </p>
          )}
          <p className="text-sm text-gray-500">Você pode fechar esta página.</p>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gray-50 py-8 px-4">
      <div className="max-w-lg mx-auto">
        <div className="text-center mb-6">
          <Link to="/" className="text-lg font-semibold text-primary hover:underline">{APP_NAME}</Link>
        </div>
        <h1 className="text-2xl font-semibold text-gray-900 mb-1">Agendar em {business.name}</h1>
        <p className="text-gray-600 text-sm mb-6">Preencha os dados abaixo para agendar.</p>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label htmlFor="service" className="block text-sm font-medium text-gray-700 mb-1">Serviço *</label>
            <select
              id="service"
              value={selectedServiceId}
              onChange={(e) => setSelectedServiceId(e.target.value)}
              className="w-full px-4 py-3 rounded-xl border border-gray-300 bg-white text-gray-900 focus:ring-2 focus:ring-primary focus:border-primary"
              required
            >
              <option value="">Selecione o serviço</option>
              {services.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name} ({s.durationMinutes} min{s.price != null ? ` — R$ ${s.price.toFixed(2).replace('.', ',')}` : ''})
                </option>
              ))}
            </select>
          </div>

          <div>
            <label htmlFor="date" className="block text-sm font-medium text-gray-700 mb-1">Data *</label>
            <input
              id="date"
              type="date"
              value={selectedDate}
              min={minDate}
              onChange={(e) => setSelectedDate(e.target.value)}
              className="w-full px-4 py-3 rounded-xl border border-gray-300 bg-white text-gray-900 focus:ring-2 focus:ring-primary focus:border-primary"
              required
            />
          </div>

          {loadingSlots && selectedServiceId && selectedDate && (
            <p className="text-sm text-gray-500">Carregando horários...</p>
          )}
          {!loadingSlots && slots.length > 0 && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Horário *</label>
              <p className="text-xs text-gray-500 mb-2">Horários em vermelho já estão agendados.</p>
              <div className="flex flex-wrap gap-2">
                {slots.map((slot) => (
                  <button
                    key={slot.scheduledAtUtc}
                    type="button"
                    disabled={!slot.available}
                    onClick={() => slot.available && setSelectedSlotUtc(slot.scheduledAtUtc)}
                    className={`px-4 py-2 rounded-lg border text-sm font-medium transition ${
                      !slot.available
                        ? 'bg-red-50 text-red-600 border-red-200 cursor-not-allowed'
                        : selectedSlotUtc === slot.scheduledAtUtc
                          ? 'bg-primary text-white border-primary'
                          : 'bg-white text-gray-700 border-gray-300 hover:bg-gray-50'
                    }`}
                  >
                    {formatTimeOnly(slot.scheduledAtUtc)}
                  </button>
                ))}
              </div>
            </div>
          )}
          {!loadingSlots && selectedServiceId && selectedDate && slots.length === 0 && (
            <p className="text-sm text-amber-600">Nenhum horário disponível nesta data.</p>
          )}

          <div>
            <label htmlFor="clientName" className="block text-sm font-medium text-gray-700 mb-1">Nome *</label>
            <input
              id="clientName"
              type="text"
              value={clientName}
              onChange={(e) => setClientName(e.target.value)}
              placeholder="Seu nome"
              className="w-full px-4 py-3 rounded-xl border border-gray-300 bg-white text-gray-900 focus:ring-2 focus:ring-primary focus:border-primary"
              required
            />
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label htmlFor="clientPhone" className="block text-sm font-medium text-gray-700 mb-1">Telefone</label>
              <input
                id="clientPhone"
                type="tel"
                value={clientPhone}
                onChange={(e) => setClientPhone(e.target.value)}
                placeholder="(00) 00000-0000"
                className="w-full px-4 py-3 rounded-xl border border-gray-300 bg-white text-gray-900 focus:ring-2 focus:ring-primary focus:border-primary"
              />
            </div>
            <div>
              <label htmlFor="clientEmail" className="block text-sm font-medium text-gray-700 mb-1">E-mail</label>
              <input
                id="clientEmail"
                type="email"
                value={clientEmail}
                onChange={(e) => setClientEmail(e.target.value)}
                placeholder="email@exemplo.com"
                className="w-full px-4 py-3 rounded-xl border border-gray-300 bg-white text-gray-900 focus:ring-2 focus:ring-primary focus:border-primary"
              />
            </div>
          </div>

          {submitError && <p className="text-sm text-red-600" role="alert">{submitError}</p>}

          <button
            type="submit"
            disabled={submitting || !selectedSlotUtc}
            className="w-full py-3 rounded-xl bg-primary text-white font-semibold hover:bg-primary/90 disabled:opacity-70 disabled:cursor-not-allowed"
          >
            {submitting ? 'Confirmando...' : 'Confirmar agendamento'}
          </button>
        </form>

        <p className="mt-6 text-center text-sm text-gray-500">
          © {APP_NAME} — Agendamento online
        </p>
      </div>
    </div>
  )
}
