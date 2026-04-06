import { useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { apiPost, API_PUBLIC_BOOKING_CANCELAR } from '../api/client'

const FALLBACK_ERROR = 'Não foi possível cancelar. O link pode ter expirado ou o agendamento já foi cancelado. Entre em contato com o estabelecimento se precisar de ajuda.'

export default function CancelarAgendamento() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''
  const [status, setStatus] = useState<'idle' | 'loading' | 'success' | 'error'>(
    token.trim() ? 'idle' : 'error'
  )
  const [message, setMessage] = useState(
    token.trim() ? '' : 'Link de cancelamento inválido. Está faltando o token.'
  )

  async function handleCancel() {
    setStatus('loading')
    const path = `${API_PUBLIC_BOOKING_CANCELAR}?token=${encodeURIComponent(token)}`
    const res = await apiPost<Record<string, never>, { message?: string }>(path, {})
    if (res.ok) {
      setStatus('success')
      setMessage(res.data?.message ?? 'Agendamento cancelado com sucesso.')
    } else {
      setStatus('error')
      const err = res.error && ('mensagem' in res.error ? res.error.mensagem : res.error.message)
      setMessage(err ?? FALLBACK_ERROR)
    }
  }

  return (
    <div className="min-h-screen bg-gray-50 flex flex-col items-center justify-center p-4">
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-8 max-w-md w-full text-center">

        {status === 'idle' && (
          <>
            <div className="w-12 h-12 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4">
              <svg className="w-6 h-6 text-red-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </div>
            <h1 className="text-xl font-semibold text-gray-900 mb-2">Cancelar agendamento</h1>
            <p className="text-gray-500 mb-6">Tem certeza que deseja cancelar seu agendamento?</p>
            <button
              onClick={handleCancel}
              className="w-full bg-red-600 hover:bg-red-700 text-white font-semibold py-3 px-6 rounded-xl transition"
            >
              Sim, cancelar agendamento
            </button>
          </>
        )}

        {status === 'loading' && (
          <>
            <div className="w-12 h-12 border-4 border-gray-200 border-t-red-600 rounded-full animate-spin mx-auto mb-4" />
            <h1 className="text-xl font-semibold text-gray-900">Cancelando...</h1>
            <p className="text-gray-500 mt-2">Aguarde um momento.</p>
          </>
        )}

        {status === 'success' && (
          <>
            <div className="w-12 h-12 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
              <svg className="w-6 h-6 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
            </div>
            <h1 className="text-xl font-semibold text-gray-900 mb-2">Cancelamento realizado</h1>
            <p className="text-gray-600 mb-4">{message}</p>
            <p className="text-sm text-gray-400">Você pode fechar esta página.</p>
          </>
        )}

        {status === 'error' && (
          <>
            <div className="w-12 h-12 bg-amber-100 rounded-full flex items-center justify-center mx-auto mb-4">
              <svg className="w-6 h-6 text-amber-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01M12 3a9 9 0 100 18A9 9 0 0012 3z" />
              </svg>
            </div>
            <h1 className="text-xl font-semibold text-gray-900 mb-2">Não foi possível cancelar</h1>
            <p className="text-gray-600 mb-4">{message}</p>
            <p className="text-sm text-gray-400">Você pode fechar esta página.</p>
          </>
        )}

      </div>
    </div>
  )
}
