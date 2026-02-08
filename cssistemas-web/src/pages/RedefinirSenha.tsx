import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import InputWithIcon from '../components/ui/InputWithIcon'
import AuthPageLayout, { AuthCardHeader } from '../components/layout/AuthPageLayout'
import { apiPost } from '../api/client'
import { ROUTES } from '../constants'

const IconLock = () => (
  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden>
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
  </svg>
)

export default function RedefinirSenha() {
  const [searchParams] = useSearchParams()
  const tokenFromUrl = searchParams.get('token') ?? ''
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [status, setStatus] = useState<'idle' | 'loading' | 'success' | 'error'>('idle')
  const [message, setMessage] = useState('')

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setMessage('')
    const token = tokenFromUrl.trim()
    if (!token) {
      setStatus('error')
      setMessage('Link inválido. Solicite uma nova redefinição de senha.')
      return
    }
    const senha = newPassword.trim()
    if (senha.length < 6) {
      setStatus('error')
      setMessage('A nova senha deve ter no mínimo 6 caracteres.')
      return
    }
    if (senha !== confirmPassword.trim()) {
      setStatus('error')
      setMessage('As senhas não coincidem.')
      return
    }
    setStatus('loading')
    setMessage('')
    const payload = { token, newPassword: senha }
    try {
      const result = await apiPost<{ token: string; newPassword: string }, { message: string }>('/api/auth/reset-password', payload)
      if (result.ok) {
        setStatus('success')
        setMessage(result.data.message)
      } else {
        setStatus('error')
        const err = result.error as {
          mensagem?: string
          message?: string
          Mensagem?: string
          erros?: { campo?: string; mensagem?: string; Campo?: string; Mensagem?: string }[]
        }
        type ErroItem = { mensagem?: string; Mensagem?: string; campo?: string; Campo?: string }
        const erros = (err.erros ?? (result.error as { Erros?: ErroItem[] })?.Erros) as ErroItem[] | undefined
        const detalhes = Array.isArray(erros)
          ? erros.map((e) => e.mensagem ?? e.Mensagem ?? e.campo ?? e.Campo).filter(Boolean).join(' ')
          : ''
        const msg = detalhes || (err.mensagem ?? err.Mensagem ?? err.message ?? 'Ocorreu um erro. Tente novamente.')
        setMessage(msg)
      }
    } catch (e) {
      setStatus('error')
      setMessage('Não foi possível conectar à API. Verifique se a API está rodando (localhost:5264) e tente novamente.')
    }
  }

  if (!tokenFromUrl.trim()) {
    return (
      <div className="min-h-screen min-h-[100dvh] flex items-center justify-center bg-primary px-4">
        <div className="w-full max-w-md bg-white rounded-2xl shadow-xl p-6 sm:p-8 text-center">
          <p className="text-gray-800 mb-4">Link inválido ou expirado.</p>
          <Link to={ROUTES.ESQUECI_SENHA} className="text-primary font-medium hover:underline">
            Solicitar novo link
          </Link>
          <p className="mt-6">
            <Link to={ROUTES.LOGIN} className="text-gray-600 text-sm hover:underline">Voltar ao login</Link>
          </p>
        </div>
      </div>
    )
  }

  return (
    <AuthPageLayout>
      <AuthCardHeader title="Redefinir senha" />
      <p className="text-gray-600 text-sm mb-6">Digite e confirme sua nova senha (mínimo 6 caracteres).</p>

      <form className="space-y-5" onSubmit={handleSubmit}>
            <InputWithIcon
              type="password"
              icon={<IconLock />}
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              placeholder="Nova senha"
              autoComplete="new-password"
              disabled={status === 'loading' || status === 'success'}
            />
            <InputWithIcon
              type="password"
              icon={<IconLock />}
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              placeholder="Confirme a nova senha"
              autoComplete="new-password"
              disabled={status === 'loading' || status === 'success'}
            />
            {message && (
              <p className={`text-sm ${status === 'success' ? 'text-green-600' : 'text-red-600'}`} role="alert">
                {message}
              </p>
            )}
            <button
              type="submit"
              disabled={status === 'loading' || status === 'success'}
              className="w-full min-h-[48px] py-3 rounded-xl bg-primary text-white font-semibold hover:bg-primary-hover transition active:scale-[0.98] touch-manipulation disabled:opacity-70"
            >
              {status === 'loading' ? 'Salvando...' : status === 'success' ? 'Senha redefinida' : 'Redefinir senha'}
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
