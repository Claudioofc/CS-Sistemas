import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import InputWithIcon from '../components/ui/InputWithIcon'
import AuthPageLayout, { AuthCardHeader } from '../components/layout/AuthPageLayout'
import { useAuth } from '../contexts/AuthContext'
import { ROUTES } from '../constants'
import { filterDocumentInput, getDocumentRequiredLength, getDocumentPlaceholder, getDocumentMaxLength } from '../utils/document'

const IconUser = () => (
  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden>
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
  </svg>
)

const IconEmail = () => (
  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden>
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
  </svg>
)

const IconLock = () => (
  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden>
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
  </svg>
)

const IconDocument = () => (
  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden>
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 6H5a2 2 0 00-2 2v9a2 2 0 002 2h14a2 2 0 002-2V8a2 2 0 00-2-2h-5m-4 0V5a2 2 0 114 0v1m-4 0a2 2 0 104 0m-5 8a2 2 0 100-4 2 2 0 000 4zm0 0c1.306 0 2.417.835 2.83 2M9 14a3.001 3.001 0 00-2.83 2M15 11h3m-3 4h2" />
  </svg>
)

/** Retorna mensagens de erro por campo (backend: Name, Email, Password, DocumentType, DocumentNumber). */
function getFieldError(campo: string, errors?: { campo: string; mensagem: string }[]): string | undefined {
  if (!errors?.length) return undefined
  const c = campo.toLowerCase()
  const err = errors.find((e) => e.campo?.toLowerCase() === c)
  return err?.mensagem
}

export default function CriarConta() {
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [documentType, setDocumentType] = useState<0 | 1>(0)
  const [documentNumber, setDocumentNumber] = useState('')
  const [error, setError] = useState('')
  const [fieldErrors, setFieldErrors] = useState<{ campo: string; mensagem: string }[]>([])
  const [loading, setLoading] = useState(false)
  const { register } = useAuth()
  const navigate = useNavigate()

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setFieldErrors([])
    const docDigits = documentNumber.replace(/\D/g, '')
    const requiredLen = getDocumentRequiredLength(documentType)
    if (docDigits.length !== requiredLen) {
      setFieldErrors([{ campo: 'documentNumber', mensagem: documentType === 0 ? 'Informe um CPF válido (11 dígitos).' : 'Informe um CNPJ válido (14 dígitos).' }])
      setError('Preencha o CPF ou CNPJ corretamente.')
      return
    }
    setLoading(true)
    const result = await register(name, email, password, documentType, documentNumber)
    setLoading(false)
    if (result.ok) {
      navigate(ROUTES.DASHBOARD, { replace: true })
      return
    }
    if (result.errors?.length) {
      setFieldErrors(result.errors)
      setError(result.message ?? 'Corrija os campos abaixo.')
    } else {
      setError(result.message ?? 'Erro ao criar conta.')
    }
  }

  const nameErr = getFieldError('name', fieldErrors)
  const emailErr = getFieldError('email', fieldErrors)
  const passwordErr = getFieldError('password', fieldErrors)
  const documentErr = getFieldError('documentNumber', fieldErrors) ?? getFieldError('documentType', fieldErrors)

  return (
    <AuthPageLayout>
      <p className="mb-4">
        <Link
          to="/login"
          className="text-primary font-medium hover:underline inline-flex items-center gap-1.5 py-2 min-h-[44px] touch-manipulation"
        >
          <span aria-hidden>←</span> Voltar ao login
        </Link>
      </p>
      <AuthCardHeader title="Criar sua conta" />
      <form className="space-y-5" onSubmit={handleSubmit}>
        <div>
          <InputWithIcon
            type="text"
            icon={<IconUser />}
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Seu nome"
            autoComplete="name"
            disabled={loading}
            aria-invalid={!!nameErr}
            aria-describedby={nameErr ? 'name-error' : undefined}
          />
          {nameErr && (
            <p id="name-error" className="mt-1 text-sm text-red-600" role="alert">
              {nameErr}
            </p>
          )}
        </div>
        <div>
          <InputWithIcon
            type="email"
            icon={<IconEmail />}
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="Seu email"
            autoComplete="email"
            disabled={loading}
            aria-invalid={!!emailErr}
            aria-describedby={emailErr ? 'email-error' : undefined}
          />
          {emailErr && (
            <p id="email-error" className="mt-1 text-sm text-red-600" role="alert">
              {emailErr}
            </p>
          )}
        </div>
        <div>
          <InputWithIcon
            type="password"
            icon={<IconLock />}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="Senha (mín. 6 caracteres)"
            autoComplete="new-password"
            disabled={loading}
            aria-invalid={!!passwordErr}
            aria-describedby={passwordErr ? 'password-error' : undefined}
          />
          {passwordErr && (
            <p id="password-error" className="mt-1 text-sm text-red-600" role="alert">
              {passwordErr}
            </p>
          )}
        </div>
        <div>
          <p className="text-sm font-medium text-gray-700 mb-2">CPF ou CNPJ <span className="text-red-500">*</span></p>
          <div className="flex gap-4 mb-2">
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="radio"
                name="documentType"
                value={0}
                checked={documentType === 0}
                onChange={() => { setDocumentType(0); setDocumentNumber('') }}
                disabled={loading}
                className="rounded-full border-gray-300 text-primary focus:ring-primary"
              />
              <span className="text-sm text-gray-700">CPF</span>
            </label>
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="radio"
                name="documentType"
                value={1}
                checked={documentType === 1}
                onChange={() => { setDocumentType(1); setDocumentNumber('') }}
                disabled={loading}
                className="rounded-full border-gray-300 text-primary focus:ring-primary"
              />
              <span className="text-sm text-gray-700">CNPJ</span>
            </label>
          </div>
          <InputWithIcon
            type="text"
            inputMode="numeric"
            icon={<IconDocument />}
            value={documentNumber}
            onChange={(e) => setDocumentNumber(filterDocumentInput(e.target.value, documentType))}
            placeholder={getDocumentPlaceholder(documentType)}
            autoComplete="off"
            disabled={loading}
            maxLength={getDocumentMaxLength(documentType)}
            aria-invalid={!!documentErr}
            aria-required
          />
          {documentErr && (
            <p className="mt-1 text-sm text-red-600" role="alert">{documentErr}</p>
          )}
        </div>
        {error && (
          <p className="text-sm text-red-600" role="alert">
            {error}
          </p>
        )}
        <button
          type="submit"
          disabled={loading}
          className="w-full min-h-[48px] py-3 rounded-xl bg-primary text-white font-semibold hover:bg-primary-hover transition active:scale-[0.98] touch-manipulation disabled:opacity-70"
        >
          {loading ? 'Criando conta...' : 'Criar conta'}
        </button>
      </form>

      <p className="mt-6 text-center text-gray-600 text-sm">
        Já tem uma conta?{' '}
        <Link
          to="/login"
          className="text-primary font-medium hover:underline py-2 min-h-[44px] inline-flex items-center justify-center touch-manipulation"
        >
          Entrar
        </Link>
      </p>
    </AuthPageLayout>
  )
}
