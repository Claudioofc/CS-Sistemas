import { useState } from 'react'
import { Link } from 'react-router-dom'
import InputWithIcon from '../components/ui/InputWithIcon'
import AuthPageLayout, { AuthCardHeader } from '../components/layout/AuthPageLayout'
import { apiPost } from '../api/client'
import { ROUTES } from '../constants'

const IconEmail = () => (
  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden>
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
  </svg>
)

export default function EsqueciSenha() {
  const [email, setEmail] = useState('')
  const [status, setStatus] = useState<'idle' | 'loading' | 'success' | 'error'>('idle')
  const [message, setMessage] = useState('')

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setStatus('loading')
    setMessage('')
    const result = await apiPost<{ email: string }, { message: string }>('/api/auth/forgot-password', { email })
    if (result.ok) {
      setStatus('success')
      setMessage(result.data.message)
    } else {
      setStatus('error')
      const err = result.error as { mensagem?: string; message?: string }
      setMessage(err.mensagem ?? err.message ?? 'Ocorreu um erro. Tente novamente.')
    }
  }

  return (
    <AuthPageLayout>
      <AuthCardHeader title="Esqueceu sua senha?" />
      <p className="text-gray-600 text-sm mb-6">Informe o e-mail cadastrado e enviaremos um link para redefinir sua senha.</p>

      <form className="space-y-5" onSubmit={handleSubmit}>
            <InputWithIcon
              type="email"
              icon={<IconEmail />}
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="Digite seu email..."
              autoComplete="email"
              disabled={status === 'loading'}
            />
            {message && (
              <p className={`text-sm ${status === 'success' ? 'text-green-600' : 'text-red-600'}`} role="alert">
                {message}
              </p>
            )}
            <button
              type="submit"
              disabled={status === 'loading'}
              className="w-full min-h-[48px] py-3 rounded-xl bg-primary text-white font-semibold hover:bg-primary-hover transition active:scale-[0.98] touch-manipulation disabled:opacity-70"
            >
              {status === 'loading' ? 'Enviando...' : 'Enviar link'}
            </button>
          </form>

          <p className="mt-6 text-center text-gray-600 text-sm">
            <Link to={ROUTES.LOGIN} className="text-primary font-medium hover:underline py-2 min-h-[44px] inline-flex items-center justify-center touch-manipulation">
              Voltar ao login
            </Link>
          </p>
    </AuthPageLayout>
  )
}
