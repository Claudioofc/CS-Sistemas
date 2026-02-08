import { useState, useEffect } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'
import { apiGet, apiPostWithAuth } from '../api/client'
import { formatCurrency } from '../utils/format'

type Plan = {
  id: string
  name: string
  price: number
  billingIntervalMonths: number
  features: string | null
  isActive: boolean
  createdAt: string
}

function formatInterval(months: number): string {
  if (months === 1) return 'mensal'
  if (months === 6) return '6 meses'
  if (months === 12) return '1 ano'
  return `${months} meses`
}

type CardEnabled = { enabled: boolean }
type PixEnabled = { enabled: boolean }
type CheckoutResponse = { initPoint: string }
type PixOrderResponse = { orderId: string; qrCode: string; qrCodeBase64: string }

/** Mensagem para exibir no alert quando uma chamada de pagamento falha (inclui detail se existir). */
function getPaymentErrorMessage(
  res: { ok: boolean; error?: unknown },
  fallback: string
): string {
  const err = !res.ok && res.error && typeof res.error === 'object' ? res.error as { message?: string; detail?: string } : null
  const msg = err?.message ?? fallback
  const detail = err && typeof err.detail === 'string' ? err.detail : ''
  return detail ? `${msg}\n\nDetalhe:\n${detail}` : msg
}

/** Formata segundos em "M:SS" para o countdown do PIX. */
function formatTimerMinutes(seconds: number): string {
  const m = Math.floor(seconds / 60)
  const s = seconds % 60
  return `${m}:${String(s).padStart(2, '0')}`
}

/**
 * Página de planos: Assine agora com Cartão (Mercado Pago) ou PIX. Ao escolher PIX, o QR code já vem com o valor do plano.
 */
export default function Planos() {
  const { token, subscriptionStatus } = useAuth()
  const location = useLocation()
  const isPremiumPage = location.pathname === '/premium'
  /** Usuário sem acesso (trial acabou ou nunca assinou): destacar que precisa assinar. */
  const semAcesso = !subscriptionStatus?.hasAccess
  const [plans, setPlans] = useState<Plan[]>([])
  const [cardEnabled, setCardEnabled] = useState(false)
  const [pixEnabled, setPixEnabled] = useState(false)
  const [loading, setLoading] = useState(true)
  const [checkoutPlanId, setCheckoutPlanId] = useState<string | null>(null)
  const [modalPlan, setModalPlan] = useState<Plan | null>(null)
  const [pixPlan, setPixPlan] = useState<Plan | null>(null)
  const [pixOrder, setPixOrder] = useState<PixOrderResponse | null>(null)
  const [pixLoading, setPixLoading] = useState(false)
  const [pixTimerSeconds, setPixTimerSeconds] = useState(0)
  const [copied, setCopied] = useState(false)

  // Countdown do timer de 5 minutos enquanto o PIX estiver visível
  useEffect(() => {
    if (!pixOrder) return
    const id = setInterval(() => {
      setPixTimerSeconds((s) => (s <= 1 ? 0 : s - 1))
    }, 1000)
    return () => clearInterval(id)
  }, [pixOrder?.orderId])

  useEffect(() => {
    let cancelled = false
    apiGet<Plan[]>('/api/plans', null).then((res) => {
      if (!cancelled && res.ok) setPlans(res.data)
      setLoading(false)
    })
    return () => { cancelled = true }
  }, [])

  useEffect(() => {
    if (!token) return
    let cancelled = false
    apiGet<CardEnabled>('/api/payment/card', token).then((res) => {
      if (!cancelled && res.ok && res.data.enabled) setCardEnabled(true)
    })
    return () => { cancelled = true }
  }, [token])

  useEffect(() => {
    if (!token) return
    let cancelled = false
    apiGet<PixEnabled>('/api/payment/pix-enabled', token).then((res) => {
      if (!cancelled && res.ok && res.data.enabled) setPixEnabled(true)
    })
    return () => { cancelled = true }
  }, [token])

  const payWithCard = async (planId: string) => {
    if (!token) return
    setModalPlan(null)
    setCheckoutPlanId(planId)
    const res = await apiPostWithAuth<{ planId: string; returnBaseUrl?: string }, CheckoutResponse>(
      '/api/payment/mercado-pago/checkout',
      { planId, returnBaseUrl: window.location.origin },
      token
    )
    setCheckoutPlanId(null)
    if (res.ok && res.data.initPoint) {
      window.location.href = res.data.initPoint
    } else {
      alert(getPaymentErrorMessage(res, 'Não foi possível abrir o checkout. Tente novamente.'))
    }
  }

  const choosePix = async (plan: Plan) => {
    if (!token) return
    setModalPlan(null)
    setPixPlan(plan)
    setPixOrder(null)
    setPixLoading(true)
    const res = await apiPostWithAuth<{ planId: string }, PixOrderResponse>(
      '/api/payment/pix',
      { planId: plan.id },
      token
    )
    setPixLoading(false)
    if (res.ok && res.data?.orderId) {
      setPixOrder(res.data)
      setPixTimerSeconds(300) // 5 minutos
    } else {
      alert(getPaymentErrorMessage(res, 'Não foi possível gerar o PIX. Tente novamente.'))
      setPixPlan(null)
    }
  }

  const copyPixCode = () => {
    if (!pixOrder?.qrCode) return
    navigator.clipboard.writeText(pixOrder.qrCode).then(() => {
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    })
  }

  const clearPixState = () => {
    setPixPlan(null)
    setPixOrder(null)
    setPixTimerSeconds(0)
  }

  return (
    <div className="p-4 sm:p-6 max-w-4xl mx-auto">
      {semAcesso && (
        <div className="mb-6 rounded-xl bg-amber-50 border border-amber-200 p-4 text-amber-900" role="alert">
          <p className="font-medium">Seu período grátis acabou</p>
          <p className="text-sm mt-1 text-amber-800">
            Para continuar usando o sistema, assine um plano abaixo. Você pode pagar com cartão ou PIX.
          </p>
        </div>
      )}
      <div className="mb-6">
        <h1 className="text-xl font-semibold text-gray-900">
          {isPremiumPage ? 'Premium' : 'Escolha seu plano'}
        </h1>
        <p className="text-gray-600 text-sm mt-1">
          {semAcesso
            ? 'Assine um plano para continuar usando o sistema.'
            : isPremiumPage
              ? 'Assine um plano para desbloquear todos os recursos do sistema.'
              : 'Seu período de teste acabou. Assine um plano para continuar usando o sistema.'}
        </p>
      </div>

      {loading ? (
        <p className="text-gray-500 text-sm">Carregando planos...</p>
      ) : plans.length === 0 ? (
        <div className="bg-amber-50 border border-amber-200 rounded-xl p-6 text-center">
          <p className="text-amber-700 text-sm">Nenhum plano disponível no momento.</p>
          <Link to="/configuracoes" className="mt-4 inline-block text-primary font-medium hover:underline">
            Ir para Configurações
          </Link>
        </div>
      ) : (
        <div className="grid gap-4 sm:grid-cols-3">
          {plans.map((plan) => (
            <div
              key={plan.id}
              className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm flex flex-col"
            >
              <h2 className="font-semibold text-gray-900">{plan.name}</h2>
              <p className="mt-2 text-2xl font-bold text-primary">
                {formatCurrency(plan.price)}
              </p>
              <p className="text-gray-500 text-sm">
                {plan.billingIntervalMonths === 1
                  ? 'por mês'
                  : `a cada ${formatInterval(plan.billingIntervalMonths)}`}
              </p>
              {plan.features && (
                <p className="mt-3 text-gray-600 text-sm flex-1">{plan.features}</p>
              )}
              <button
                type="button"
                disabled={checkoutPlanId === plan.id}
                onClick={() => setModalPlan(plan)}
                className="mt-4 w-full min-h-[44px] py-2.5 rounded-xl bg-primary text-white font-medium hover:bg-primary/90 disabled:opacity-70 touch-manipulation"
              >
                Assine agora
              </button>
            </div>
          ))}
        </div>
      )}

      {/* Modal: Escolher Cartão ou PIX */}
      {modalPlan && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50" onClick={() => setModalPlan(null)}>
          <div className="bg-white rounded-xl shadow-xl max-w-sm w-full p-6" onClick={(e) => e.stopPropagation()}>
            <h3 className="text-lg font-semibold text-gray-900">Forma de pagamento</h3>
            <p className="mt-1 text-gray-600 text-sm">
              Plano <strong>{modalPlan.name}</strong> — {formatCurrency(modalPlan.price)}
            </p>
            <div className="mt-4 space-y-3">
              <button
                type="button"
                disabled={!cardEnabled || checkoutPlanId === modalPlan.id}
                onClick={() => payWithCard(modalPlan.id)}
                className="w-full min-h-[44px] py-2.5 rounded-xl bg-primary text-white font-medium hover:bg-primary/90 disabled:opacity-70 disabled:cursor-not-allowed touch-manipulation flex items-center justify-center gap-2"
              >
                {checkoutPlanId === modalPlan.id ? 'Abrindo...' : 'Cartão (crédito/débito)'}
              </button>
              <button
                type="button"
                disabled={!pixEnabled}
                onClick={() => choosePix(modalPlan)}
                className="w-full min-h-[44px] py-2.5 rounded-xl border-2 border-primary text-primary font-medium hover:bg-primary/10 disabled:opacity-50 disabled:cursor-not-allowed touch-manipulation"
              >
                PIX (valor já no QR)
              </button>
            </div>
            <p className="mt-3 text-xs text-gray-500">
              Cartão: Visa, Mastercard, Elo, Hipercard. PIX: escaneie o QR e o valor já vem preenchido.
            </p>
            <button
              type="button"
              onClick={() => setModalPlan(null)}
              className="mt-4 w-full py-2 text-sm text-gray-500 hover:text-gray-700"
            >
              Cancelar
            </button>
          </div>
        </div>
      )}

      {/* Tela PIX: QR code e copia-e-cola (valor já incluso) */}
      {pixPlan && (
        <div className="mt-8 p-6 bg-white border border-gray-200 rounded-xl shadow-sm max-w-md mx-auto">
          <h2 className="font-semibold text-gray-900 mb-1">Pagamento via PIX</h2>
          <p className="text-gray-700 font-medium mb-1">
            Valor: <span className="text-primary">{formatCurrency(pixPlan.price)}</span> — {pixPlan.name}
          </p>
          <p className="text-gray-600 text-sm mb-4">
            Escaneie o QR code no app do seu banco. O valor já vem preenchido.
          </p>
          {pixLoading ? (
            <p className="text-gray-500 text-sm py-8 text-center">Gerando PIX...</p>
          ) : pixOrder ? (
            <>
              <p className="text-sm text-gray-600 mb-4 flex items-center justify-center gap-2">
                {pixTimerSeconds > 0 ? (
                  <>
                    <span className="inline-flex h-2 w-2 rounded-full bg-green-500 animate-pulse" aria-hidden />
                    Expira em <strong>{formatTimerMinutes(pixTimerSeconds)}</strong>
                  </>
                ) : (
                  <span className="text-amber-600 font-medium">PIX expirado. Gere um novo acima.</span>
                )}
              </p>
              <div className="flex flex-col sm:flex-row items-center gap-6">
              <div className="flex-shrink-0 p-3 bg-white rounded-lg border border-gray-200">
                {pixOrder.qrCodeBase64 ? (
                  <img
                    src={`data:image/png;base64,${pixOrder.qrCodeBase64}`}
                    alt="QR Code PIX"
                    className="w-40 h-40 block"
                  />
                ) : (
                  <p className="text-xs text-gray-500 w-40 text-center">QR não disponível como imagem. Use o código abaixo.</p>
                )}
                <p className="text-xs text-gray-500 mt-2 text-center">Escaneie com o app do banco</p>
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">Ou copie o código PIX</p>
                <p className="text-sm font-mono text-gray-900 break-all bg-gray-50 p-2 rounded border max-h-24 overflow-y-auto">
                  {pixOrder.qrCode}
                </p>
                <button
                  type="button"
                  onClick={copyPixCode}
                  className="mt-3 w-full sm:w-auto min-h-[44px] px-4 py-2 rounded-xl bg-primary text-white text-sm font-medium hover:bg-primary/90"
                >
                  {copied ? 'Copiado!' : 'Copiar código'}
                </button>
              </div>
            </div>
            </>
          ) : null}
          <button
            type="button"
            onClick={clearPixState}
            className="mt-4 text-sm text-gray-500 hover:text-gray-700"
          >
            Escolher outro plano
          </button>
        </div>
      )}

      <p className="mt-6 text-center">
        <Link to="/configuracoes" className="text-sm text-gray-600 hover:underline">
          Configurações
        </Link>
      </p>
    </div>
  )
}
