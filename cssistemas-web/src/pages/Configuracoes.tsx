import { useState, useEffect, useRef } from 'react'
import { useAuth } from '../contexts/AuthContext'
import type { DocumentType } from '../contexts/AuthContext'
import InputWithIcon from '../components/ui/InputWithIcon'
import { formatDocument, filterDocumentInput, getDocumentPlaceholder, getDocumentMaxLength } from '../utils/document'
import { apiGet, apiPostWithAuth, apiPut, apiUploadProfilePhoto, getProfilePhotoUrl } from '../api/client'
import { NEGOCIO_SINGULAR } from '../constants'
import type { BusinessItem } from '../types/api'

const IconUser = () => (
  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden>
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
  </svg>
)

const IconDoc = () => (
  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden>
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
  </svg>
)

const IconClock = () => (
  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden>
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
  </svg>
)

/** BusinessType.Dentista = 0 (clínica odontológica). */
const BUSINESS_TYPE_DENTISTA = 0

const DAY_NAMES = ['Domingo', 'Segunda', 'Terça', 'Quarta', 'Quinta', 'Sexta', 'Sábado']

type HoursItem = { dayOfWeek: number; openAtMinutes: number | null; closeAtMinutes: number | null }

function minutesToTime(m: number): string {
  const h = Math.floor(m / 60)
  const min = m % 60
  return `${h.toString().padStart(2, '0')}:${min.toString().padStart(2, '0')}`
}

function timeToMinutes(s: string): number {
  const [h, min] = s.split(':').map(Number)
  return (h || 0) * 60 + (min || 0)
}

function getFieldError(campo: string, errors?: { campo: string; mensagem: string }[]): string | undefined {
  if (!errors?.length) return undefined
  const c = campo.toLowerCase()
  const err = errors.find((e) => e.campo?.toLowerCase() === c)
  return err?.mensagem
}

export default function Configuracoes() {
  const { user, updateProfile, token } = useAuth()
  const [name, setName] = useState('')
  const [profilePhotoFile, setProfilePhotoFile] = useState<File | null>(null)
  const [documentType, setDocumentType] = useState<DocumentType | ''>('')
  const [documentNumber, setDocumentNumber] = useState('')
  const [error, setError] = useState('')
  const [fieldErrors, setFieldErrors] = useState<{ campo: string; mensagem: string }[]>([])
  const [loading, setLoading] = useState(false)
  const [saved, setSaved] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [previewPhotoUrl, setPreviewPhotoUrl] = useState<string | null>(null)
  const [photoUploading, setPhotoUploading] = useState(false)
  const [photoError, setPhotoError] = useState('')

  // Seção: Sua clínica (nicho odontológico)
  const [businesses, setBusinesses] = useState<BusinessItem[]>([])
  const [clinicName, setClinicName] = useState('')
  const [clinicLoading, setClinicLoading] = useState(false)
  const [clinicError, setClinicError] = useState('')
  const [clinicSaved, setClinicSaved] = useState(false)

  // Link de agendamento (slug por negócio)
  const [slugByBusinessId, setSlugByBusinessId] = useState<Record<string, string>>({})
  const [slugSavingId, setSlugSavingId] = useState<string | null>(null)
  const [slugError, setSlugError] = useState('')
  const [slugSaved, setSlugSaved] = useState(false)

  // Horários de funcionamento
  const [selectedBusinessId, setSelectedBusinessId] = useState<string>('')
  const [hoursItems, setHoursItems] = useState<HoursItem[]>(() =>
    Array.from({ length: 7 }, (_, i) => ({ dayOfWeek: i, openAtMinutes: i >= 1 && i <= 5 ? 480 : null, closeAtMinutes: i >= 1 && i <= 5 ? 1080 : null }))
  )
  const [hoursLoading, setHoursLoading] = useState(false)
  const [hoursSaving, setHoursSaving] = useState(false)
  const [hoursError, setHoursError] = useState('')
  const [hoursSaved, setHoursSaved] = useState(false)

  useEffect(() => {
    if (!token) return
    apiGet<BusinessItem[]>('/api/business', token).then((res) => {
      if (res.ok) {
        setBusinesses(res.data)
        setSlugByBusinessId(prev => {
          const next = { ...prev }
          res.data.forEach(b => { next[b.id] = b.publicSlug ?? '' })
          return next
        })
      }
    })
  }, [token])

  useEffect(() => {
    if (!token || !selectedBusinessId) return
    setHoursLoading(true)
    setHoursError('')
    apiGet<{ dayOfWeek: number; openAtMinutes: number | null; closeAtMinutes: number | null }[]>(
      `/api/business/${selectedBusinessId}/hours`,
      token
    ).then((res) => {
      setHoursLoading(false)
      if (res.ok) setHoursItems(res.data)
      else setHoursItems((prev) => prev)
    })
  }, [token, selectedBusinessId])

  async function handleSaveHours(e: React.FormEvent) {
    e.preventDefault()
    if (!selectedBusinessId || !token) return
    setHoursError('')
    setHoursSaved(false)
    setHoursSaving(true)
    const items = hoursItems.map((h) => ({
      dayOfWeek: h.dayOfWeek,
      openAtMinutes: h.openAtMinutes ?? null,
      closeAtMinutes: h.closeAtMinutes ?? null,
    }))
    const result = await apiPut<{ items: typeof items }, HoursItem[]>(
      `/api/business/${selectedBusinessId}/hours`,
      { items },
      token
    )
    setHoursSaving(false)
    if (result.ok) {
      setHoursSaved(true)
      setHoursItems(result.data)
    } else {
      const err = result.error
      setHoursError((err && ('mensagem' in err ? err.mensagem : err.message)) ?? 'Erro ao salvar horários.')
    }
  }

  function setHoursDay(dayOfWeek: number, closed: boolean, openStr?: string, closeStr?: string) {
    setHoursItems((prev) =>
      prev.map((h) =>
        h.dayOfWeek === dayOfWeek
          ? {
              dayOfWeek,
              openAtMinutes: closed ? null : (openStr ? timeToMinutes(openStr) : h.openAtMinutes ?? 480),
              closeAtMinutes: closed ? null : (closeStr ? timeToMinutes(closeStr) : h.closeAtMinutes ?? 1080),
            }
          : h
      )
    )
  }

  async function handleAddClinic(e: React.FormEvent) {
    e.preventDefault()
    const name = clinicName.trim()
    if (!name) {
      setClinicError('Informe o nome da clínica.')
      return
    }
    setClinicError('')
    setClinicSaved(false)
    setClinicLoading(true)
    const result = await apiPostWithAuth<{ name: string; businessType: number; publicSlug: string | null }, BusinessItem>(
      '/api/business',
      { name, businessType: BUSINESS_TYPE_DENTISTA, publicSlug: null },
      token
    )
    setClinicLoading(false)
    if (result.ok) {
      setBusinesses((prev) => [...prev, result.data])
      setSlugByBusinessId(prev => ({ ...prev, [result.data.id]: result.data.publicSlug ?? '' }))
      setClinicName('')
      setClinicSaved(true)
      return
    }
    const err = result.error
    const errMsg = err && ('mensagem' in err ? err.mensagem : err.message)
    setClinicError(errMsg ?? 'Erro ao cadastrar clínica. Tente novamente.')
  }

  async function handleSaveSlug(businessId: string) {
    if (!token) return
    const business = businesses.find(b => b.id === businessId)
    if (!business) return
    const slugValue = (slugByBusinessId[businessId] ?? '').trim().toLowerCase().replace(/\s+/g, '-').replace(/[^a-z0-9_-]/g, '')
    setSlugError('')
    setSlugSaved(false)
    setSlugSavingId(businessId)
    const result = await apiPut<{ name: string; businessType: number; publicSlug: string | null; whatsAppPhone: string | null }, BusinessItem>(
      `/api/business/${businessId}`,
      { name: business.name, businessType: business.businessType, publicSlug: slugValue || null, whatsAppPhone: business.whatsAppPhone ?? null },
      token
    )
    setSlugSavingId(null)
    if (result.ok) {
      setBusinesses(prev => prev.map(b => b.id === businessId ? result.data : b))
      setSlugByBusinessId(prev => ({ ...prev, [businessId]: result.data.publicSlug ?? '' }))
      setSlugSaved(true)
    } else {
      const err = result.error && ('mensagem' in result.error ? result.error.mensagem : result.error.message)
      setSlugError(err ?? 'Erro ao salvar link.')
    }
  }

  function getBookingLink(slug: string | null): string {
    if (!slug?.trim()) return ''
    const base = typeof window !== 'undefined' ? window.location.origin : ''
    return `${base}/agendar/${slug.trim()}`
  }

  function copyBookingLink(slug: string | null) {
    const link = getBookingLink(slug)
    if (!link) return
    navigator.clipboard.writeText(link).then(() => setSlugSaved(true))
  }

  useEffect(() => {
    if (profilePhotoFile) {
      const url = URL.createObjectURL(profilePhotoFile)
      setPreviewPhotoUrl(url)
      return () => URL.revokeObjectURL(url)
    }
    setPreviewPhotoUrl(user?.profilePhotoUrl ? (getProfilePhotoUrl(user.profilePhotoUrl) ?? user.profilePhotoUrl) : null)
    return () => {}
  }, [profilePhotoFile, user?.profilePhotoUrl])

  useEffect(() => {
    if (user) {
      setName(user.name)
      const dt = user.documentType ?? ''
      setDocumentType(dt)
      const doc = user.documentNumber ?? ''
      setDocumentNumber(doc ? formatDocument(doc, (dt === 1 ? 1 : 0) as 0 | 1) : '')
    }
  }, [user])

  /** Upload de foto: um único lugar (DRY). Retorna a URL ou null em caso de erro. */
  async function uploadProfilePhotoFile(file: File): Promise<string | null> {
    const formData = new FormData()
    formData.append('file', file)
    const res = await apiUploadProfilePhoto(formData, token ?? null)
    if (!res.ok) return null
    return res.data.url
  }

  async function handleProfilePhotoChange(file: File | null) {
    setProfilePhotoFile(file ?? null)
    setPhotoError('')
    if (!file) return
    setPhotoUploading(true)
    const photoUrl = await uploadProfilePhotoFile(file)
    if (photoUrl == null) {
      setPhotoError('Erro ao enviar imagem.')
      setPhotoUploading(false)
      return
    }
    const result = await updateProfile({
      name: user?.name ?? '',
      profilePhotoUrl: photoUrl,
      documentType: user?.documentType ?? null,
      documentNumber: user?.documentNumber ?? null,
    })
    if (result.ok) {
      setProfilePhotoFile(null)
      if (fileInputRef.current) fileInputRef.current.value = ''
    } else {
      setPhotoError(result.message ?? 'Erro ao salvar foto.')
    }
    setPhotoUploading(false)
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setPhotoError('')
    setFieldErrors([])
    setSaved(false)
    setLoading(true)
    let photoUrl: string | null = user?.profilePhotoUrl ?? null
    if (profilePhotoFile) {
      photoUrl = await uploadProfilePhotoFile(profilePhotoFile)
      if (photoUrl == null) {
        setError('Erro ao enviar imagem.')
        setLoading(false)
        return
      }
      setProfilePhotoFile(null)
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
    const docNum = documentNumber.replace(/\D/g, '').trim()
    const result = await updateProfile({
      name,
      profilePhotoUrl: photoUrl,
      documentType: documentType === '' ? null : documentType,
      documentNumber: docNum || null,
    })
    setLoading(false)
    if (result.ok) {
      setSaved(true)
      const num = documentNumber.replace(/\D/g, '')
      setDocumentNumber(num ? formatDocument(num, (documentType === 1 ? 1 : 0) as 0 | 1) : '')
      if (!num) setDocumentType('')
      return
    }
    if (result.errors?.length) {
      setFieldErrors(result.errors)
      setError(result.message ?? 'Corrija os campos abaixo.')
    } else {
      setError(result.message ?? 'Erro ao salvar.')
    }
  }

  const nameErr = getFieldError('name', fieldErrors)
  const documentTypeErr = getFieldError('documentType', fieldErrors)
  const documentNumberErr = getFieldError('documentNumber', fieldErrors)

  return (
    <div className="p-4 sm:p-6 max-w-xl">
      <h1 className="text-xl font-semibold text-gray-900 mb-6">Configurações</h1>

      {/* Seção: Sua empresa */}
      <section className="mb-8">
        <h2 className="text-lg font-semibold text-gray-900 mb-1">Sua Empresa</h2>
        <p className="text-gray-600 text-sm mb-4">
          Cadastre sua empresa para usar o dashboard, agendamentos e serviços.
        </p>
        {businesses.length > 0 ? (
          <ul className="space-y-2 mb-4">
            {businesses.map((b) => (
              <li key={b.id} className="flex items-center gap-2 px-4 py-3 bg-gray-50 rounded-lg border border-gray-200">
                <span className="font-medium text-gray-900">{b.name}</span>
              </li>
            ))}
          </ul>
        ) : null}
        {businesses.length === 0 && (
          <>
            <form onSubmit={handleAddClinic} className="space-y-3">
              <div className="flex flex-col sm:flex-row gap-3">
                <div className="flex-1">
                  <label htmlFor="clinicName" className="sr-only">
                    Nome da {NEGOCIO_SINGULAR}
                  </label>
                  <input
                    id="clinicName"
                    type="text"
                    value={clinicName}
                    onChange={(e) => setClinicName(e.target.value)}
                    placeholder={`Nome da ${NEGOCIO_SINGULAR}`}
                    disabled={clinicLoading}
                    aria-label="Nome da clínica"
                    className="w-full px-4 py-3 min-h-[48px] text-base border border-gray-300 rounded-xl focus:ring-2 focus:ring-primary focus:border-primary outline-none transition touch-manipulation"
                  />
                </div>
                <button
                  type="submit"
                  disabled={clinicLoading || !clinicName.trim()}
                  className="min-h-[44px] px-4 py-2 rounded-xl bg-primary text-white font-medium hover:bg-primary/90 disabled:opacity-70 touch-manipulation whitespace-nowrap"
                >
                  {clinicLoading ? 'Salvando...' : `Cadastrar ${NEGOCIO_SINGULAR}`}
                </button>
              </div>
            </form>
            {clinicError && <p className="mt-2 text-sm text-red-600" role="alert">{clinicError}</p>}
            {clinicSaved && <p className="mt-2 text-sm text-green-600" role="status">{NEGOCIO_SINGULAR} cadastrada com sucesso.</p>}
          </>
        )}
      </section>

      {/* Link de agendamento — slug para /agendar/{slug} */}
      {businesses.length > 0 && (
        <section className="mb-8 border-t border-gray-200 pt-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-1">Link de agendamento</h2>
          {slugError && <p className="mb-2 text-sm text-red-600" role="alert">{slugError}</p>}
          {slugSaved && <p className="mb-2 text-sm text-green-600" role="status">Link salvo. Copie e envie para seus clientes.</p>}
          <ul className="space-y-4">
            {businesses.map((b) => {
              const slugValue = slugByBusinessId[b.id] ?? (b.publicSlug ?? '')
              const link = getBookingLink((slugValue.trim() || b.publicSlug) ?? null)
              return (
                <li key={b.id} className="p-4 rounded-xl border border-gray-200 bg-gray-50">
                  <p className="text-sm font-medium text-gray-900 mb-2">{b.name}</p>
                  <div className="flex flex-wrap gap-2 items-center">
                    <span className="text-sm text-gray-600 whitespace-nowrap">{typeof window !== 'undefined' ? window.location.origin : ''}/agendar/</span>
                    <input
                      type="text"
                      value={slugValue}
                      onChange={(e) => setSlugByBusinessId(prev => ({ ...prev, [b.id]: e.target.value.replace(/[^a-zA-Z0-9_-]/g, '').toLowerCase() }))}
                      placeholder="ex: minha-clinica"
                      className="flex-1 min-w-[120px] rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 focus:ring-2 focus:ring-primary focus:border-primary"
                      maxLength={100}
                    />
                    <button
                      type="button"
                      onClick={() => handleSaveSlug(b.id)}
                      disabled={slugSavingId === b.id}
                      className="min-h-[40px] px-4 py-2 rounded-xl bg-primary text-white text-sm font-medium hover:bg-primary/90 disabled:opacity-70"
                    >
                      {slugSavingId === b.id ? 'Salvando...' : 'Salvar'}
                    </button>
                    {link && (
                      <button
                        type="button"
                        onClick={() => copyBookingLink((slugValue.trim() || b.publicSlug) ?? null)}
                        className="min-h-[40px] px-4 py-2 rounded-xl border border-gray-300 text-gray-700 text-sm font-medium hover:bg-gray-50"
                      >
                        Copiar link
                      </button>
                    )}
                  </div>
                  {link && <p className="mt-2 text-xs text-gray-500 break-all">{link}</p>}
                </li>
              )
            })}
          </ul>
        </section>
      )}

      {/* Horários de funcionamento */}
      {businesses.length > 0 && (
        <section className="mb-8 border-t border-gray-200 pt-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-1 flex items-center gap-2">
            <IconClock />
            Horários de funcionamento
          </h2>
          <p className="text-gray-600 text-sm mb-4">
            Defina em quais dias e horários sua empresa atende. Dias fechados não exibirão horários no agendamento.
          </p>
          <div className="mb-4">
            <label htmlFor="hoursBusiness" className="block text-sm font-medium text-gray-700 mb-1">
              {NEGOCIO_SINGULAR}
            </label>
            <select
              id="hoursBusiness"
              value={selectedBusinessId}
              onChange={(e) => setSelectedBusinessId(e.target.value)}
              className="w-full rounded-xl border border-gray-300 px-4 py-2.5 text-gray-900 focus:ring-2 focus:ring-primary focus:border-primary"
            >
              <option value="">Selecione a {NEGOCIO_SINGULAR}</option>
              {businesses.map((b) => (
                <option key={b.id} value={b.id}>
                  {b.name}
                </option>
              ))}
            </select>
          </div>
          {selectedBusinessId && (
            <form onSubmit={handleSaveHours} className="space-y-3">
              {hoursLoading ? (
                <p className="text-sm text-gray-500">Carregando horários...</p>
              ) : (
                <ul className="space-y-2">
                  {hoursItems.map((h) => {
                    const closed = h.openAtMinutes == null && h.closeAtMinutes == null
                    return (
                      <li key={h.dayOfWeek} className="flex flex-wrap items-center gap-2 sm:gap-4 py-2 border-b border-gray-100 last:border-0">
                        <span className="w-24 text-sm font-medium text-gray-700">{DAY_NAMES[h.dayOfWeek]}</span>
                        <label className="flex items-center gap-1.5">
                          <input
                            type="checkbox"
                            checked={closed}
                            onChange={(e) => setHoursDay(h.dayOfWeek, e.target.checked)}
                            className="rounded border-gray-300 text-primary focus:ring-primary"
                          />
                          <span className="text-sm text-gray-600">Fechado</span>
                        </label>
                        {!closed && (
                          <>
                            <label className="sr-only">Abertura</label>
                            <input
                              type="time"
                              value={h.openAtMinutes != null ? minutesToTime(h.openAtMinutes) : '08:00'}
                              onChange={(e) => setHoursDay(h.dayOfWeek, false, e.target.value, undefined)}
                              className="rounded-lg border border-gray-300 px-2 py-1.5 text-sm"
                            />
                            <span className="text-gray-400">às</span>
                            <label className="sr-only">Fechamento</label>
                            <input
                              type="time"
                              value={h.closeAtMinutes != null ? minutesToTime(h.closeAtMinutes) : '18:00'}
                              onChange={(e) => setHoursDay(h.dayOfWeek, false, undefined, e.target.value)}
                              className="rounded-lg border border-gray-300 px-2 py-1.5 text-sm"
                            />
                          </>
                        )}
                      </li>
                    )
                  })}
                </ul>
              )}
              {hoursError && <p className="text-sm text-red-600" role="alert">{hoursError}</p>}
              {hoursSaved && <p className="text-sm text-green-600" role="status">Horários salvos com sucesso.</p>}
              <button
                type="submit"
                disabled={hoursLoading || hoursSaving}
                className="min-h-[44px] px-4 py-2 rounded-xl bg-primary text-white font-medium hover:bg-primary/90 disabled:opacity-70"
              >
                {hoursSaving ? 'Salvando...' : 'Salvar horários'}
              </button>
            </form>
          )}
        </section>
      )}

      <div className="border-t border-gray-200 pt-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-1">Perfil</h2>
        <p className="text-gray-600 text-sm mb-6">Atualize seu perfil. Nome e documento (CPF ou CNPJ) são obrigatórios; a validação é feita no servidor.</p>
      </div>

      <form className="space-y-5" onSubmit={handleSubmit}>
        <div>
          <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">Nome <span className="text-red-500">*</span></label>
          <InputWithIcon
            id="name"
            type="text"
            icon={<IconUser />}
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Seu nome"
            autoComplete="name"
            disabled={loading}
            aria-invalid={!!nameErr}
          />
          {nameErr && <p className="mt-1 text-sm text-red-600" role="alert">{nameErr}</p>}
        </div>

        <div>
          <label id="profilePhotoLabel" className="block text-sm font-medium text-gray-700 mb-2">Foto de perfil (opcional)</label>
          <input
            ref={fileInputRef}
            id="profilePhotoFile"
            type="file"
            accept="image/jpeg,image/jpg,image/png,image/webp,image/gif"
            onChange={(e) => handleProfilePhotoChange(e.target.files?.[0] ?? null)}
            disabled={loading || photoUploading}
            className="sr-only"
            aria-labelledby="profilePhotoLabel"
            aria-label="Selecionar foto de perfil"
          />
          <div className="flex flex-col items-start gap-2">
            <button
              type="button"
              onClick={() => fileInputRef.current?.click()}
              disabled={loading || photoUploading}
              className="relative flex items-center justify-center w-20 h-20 rounded-full border-2 border-gray-300 border-dashed bg-gray-50 hover:bg-gray-100 focus:ring-2 focus:ring-primary focus:ring-offset-2 transition disabled:opacity-70 disabled:pointer-events-none overflow-hidden"
              aria-label="Escolher foto de perfil"
            >
              {previewPhotoUrl ? (
                <img src={previewPhotoUrl} alt="" className="w-full h-full object-cover" />
              ) : (
                <span className="text-gray-400 [&>svg]:w-10 [&>svg]:h-10" aria-hidden><IconUser /></span>
              )}
              {photoUploading && (
                <span className="absolute inset-0 flex items-center justify-center bg-black/40 rounded-full text-white text-xs font-medium">
                  Salvando...
                </span>
              )}
            </button>
            <button
              type="button"
              onClick={() => fileInputRef.current?.click()}
              disabled={loading || photoUploading}
              className="text-sm text-primary hover:underline disabled:opacity-70"
            >
              {previewPhotoUrl ? 'Trocar foto' : 'Escolher foto'}
            </button>
          </div>
          {photoError && <p className="mt-1 text-sm text-red-600" role="alert">{photoError}</p>}
        </div>

        <div className="border-t border-gray-200 pt-5">
          <p className="text-sm font-medium text-gray-700 mb-2">Documento (CPF ou CNPJ) <span className="text-red-500">*</span></p>
          <div className="flex gap-4 mb-3">
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="radio"
                name="documentType"
                value={0}
                checked={documentType === 0}
                onChange={() => setDocumentType(0)}
                disabled={loading}
                className="rounded-full border-gray-300 text-primary focus:ring-primary"
                aria-required
              />
              <span className="text-sm text-gray-700">CPF</span>
            </label>
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="radio"
                name="documentType"
                value={1}
                checked={documentType === 1}
                onChange={() => setDocumentType(1)}
                disabled={loading}
                className="rounded-full border-gray-300 text-primary focus:ring-primary"
                aria-required
              />
              <span className="text-sm text-gray-700">CNPJ</span>
            </label>
          </div>
          {documentTypeErr && <p className="text-sm text-red-600 mb-2" role="alert">{documentTypeErr}</p>}
          <InputWithIcon
            type="text"
            inputMode="numeric"
            icon={<IconDoc />}
            value={documentNumber}
            onChange={(e) => setDocumentNumber(filterDocumentInput(e.target.value, (documentType === 1 ? 1 : 0) as 0 | 1))}
            placeholder={getDocumentPlaceholder(documentType)}
            autoComplete="off"
            disabled={loading}
            maxLength={getDocumentMaxLength(documentType)}
            aria-invalid={!!documentNumberErr}
            aria-required
          />
          {documentNumberErr && <p className="mt-1 text-sm text-red-600" role="alert">{documentNumberErr}</p>}
        </div>

        {error && <p className="text-sm text-red-600" role="alert">{error}</p>}
        {saved && <p className="text-sm text-green-600" role="status">Perfil salvo com sucesso.</p>}

        <button
          type="submit"
          disabled={loading}
          className="w-full min-h-[48px] py-3 rounded-xl bg-primary text-white font-semibold hover:bg-primary-hover transition active:scale-[0.98] touch-manipulation disabled:opacity-70"
        >
          {loading ? 'Salvando...' : 'Salvar'}
        </button>
      </form>
    </div>
  )
}
