import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import InputWithIcon from '../components/ui/InputWithIcon'
import AuthPageLayout, { AuthCardHeader } from '../components/layout/AuthPageLayout'
import { useAuth } from '../contexts/AuthContext'
import { ROUTES, APP_NAME } from '../constants'

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

export default function Login() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [attemptsRemaining, setAttemptsRemaining] = useState<number | null>(null)
  const [locked, setLocked] = useState(false)
  const [loading, setLoading] = useState(false)
  const { login } = useAuth()
  const navigate = useNavigate()

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setAttemptsRemaining(null)
    setLocked(false)
    setLoading(true)
    const result = await login(email, password)
    setLoading(false)
    if (result.ok) navigate(ROUTES.DASHBOARD, { replace: true })
    else {
      setError(result.message ?? 'Erro ao entrar.')
      if (result.locked) setLocked(true)
      else if (result.attemptsRemaining != null) setAttemptsRemaining(result.attemptsRemaining)
    }
  }

  return (
    <AuthPageLayout>
      <AuthCardHeader title="Bem-vindo de volta!" centerTitle />

      <form className="space-y-5" onSubmit={handleSubmit}>
            <InputWithIcon
              type="email"
              icon={<IconEmail />}
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="Digite seu email..."
              autoComplete="email"
              disabled={loading}
            />
            <InputWithIcon
              type="password"
              icon={<IconLock />}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Digite sua senha..."
              autoComplete="current-password"
              disabled={loading}
            />
            {error && <p className="text-sm text-red-600" role="alert">{error}</p>}
            {attemptsRemaining != null && !locked && (
              <p className="text-sm text-amber-700 bg-amber-50 rounded-lg px-3 py-2">
                Tentativas restantes: <strong>{attemptsRemaining}</strong>
              </p>
            )}
            {locked && (
              <div className="text-sm text-amber-800 bg-amber-50 rounded-lg px-3 py-2 space-y-2">
                <p>Conta bloqueada. Use o link abaixo para redefinir sua senha por e-mail.</p>
                <Link to={ROUTES.ESQUECI_SENHA} className="text-primary font-medium hover:underline inline-block">
                  Redefinir senha por e-mail
                </Link>
              </div>
            )}
            <div className="flex justify-center">
              <Link to={ROUTES.ESQUECI_SENHA} className="text-sm text-primary hover:underline py-2 min-h-[44px] inline-flex items-center justify-center touch-manipulation">
                Esqueceu sua senha?
              </Link>
            </div>
            <button
              type="submit"
              disabled={loading}
              className="w-full min-h-[48px] py-3 rounded-xl bg-primary text-white font-semibold hover:bg-primary-hover transition active:scale-[0.98] touch-manipulation disabled:opacity-70"
            >
              {loading ? 'Entrando...' : 'Entrar'}
            </button>
          </form>

          <p className="mt-6 text-center text-gray-600 text-sm">
            Não tem uma conta?{' '}
            <Link to={ROUTES.CRIAR_CONTA} className="text-primary font-medium hover:underline py-2 min-h-[44px] inline-flex items-center justify-center touch-manipulation">
              Criar conta
            </Link>
          </p>
          <p className="mt-6 text-center text-gray-500 text-xs sm:text-sm">
            © {APP_NAME} 2026 — Todos os direitos reservados
          </p>
    </AuthPageLayout>
  )
}
