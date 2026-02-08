import { useState, useEffect } from 'react'
import { useSearchParams } from 'react-router-dom'
import { apiPost, API_PUBLIC_BOOKING_CANCELAR } from '../api/client'

const FALLBACK_ERROR = 'Não foi possível cancelar. O link pode ter expirado ou o agendamento já foi cancelado. Entre em contato com o estabelecimento se precisar de ajuda.'

export default function CancelarAgendamento() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''
  const [status, setStatus] = useState<'idle' | 'loading' | 'success' | 'error'>('idle')
  const [message, setMessage] = useState('')

  useEffect(() => {
    if (!token.trim()) {
      setStatus('error')
      setMessage('Link de cancelamento inválido. Está faltando o token.')
      return
    }
    let cancelled = false
    setStatus('loading')
    const path = `${API_PUBLIC_BOOKING_CANCELAR}?token=${encodeURIComponent(token)}`
    apiPost<Record<string, never>, { message?: string }>(path, {}).then((res) => {
      if (cancelled) return
      if (res.ok) {
        setStatus('success')
        setMessage(res.data?.message ?? 'Agendamento cancelado com sucesso.')
      } else {
        setStatus('error')
        const err = res.error && ('mensagem' in res.error ? res.error.mensagem : res.error.message)
        setMessage(err ?? FALLBACK_ERROR)
      }
    }).catch(() => {
      if (!cancelled) {
        setStatus('error')
        setMessage(FALLBACK_ERROR)
      }
    })
    return () => { cancelled = true }
  }, [token])

  return (
    <div className="min-h-screen bg-gray-50 flex flex-col items-center justify-center p-4">
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 max-w-md w-full text-center">
        <h1 className="text-xl font-semibold text-gray-900 mb-2">
          {status === 'loading' && 'Cancelando...'}
          {status === 'success' && 'Cancelamento realizado'}
          {status === 'error' && 'Não foi possível cancelar'}
          {status === 'idle' && token && 'Cancelando...'}
        </h1>
        {status === 'loading' && (
          <p className="text-gray-600">Aguarde um momento.</p>
        )}
        {status === 'success' && (
          <>
            <p className="text-gray-600 mb-4">{message}</p>
            <p className="text-sm text-gray-500">Você pode fechar esta página.</p>
          </>
        )}
        {status === 'error' && (
          <>
            <p className="text-gray-600 mb-4">{message}</p>
            <p className="text-sm text-gray-500">Você pode fechar esta página.</p>
          </>
        )}
        {!token.trim() && status === 'error' && (
          <p className="text-sm text-gray-500 mt-2">Você pode fechar esta página.</p>
        )}
      </div>
    </div>
  )
}
