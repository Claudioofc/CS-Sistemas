/**
 * Tipos compartilhados das respostas da API (DRY).
 */

/** Resposta de usuário do sistema para painel admin (clientes premium e gratuitos). */
export type AdminUserItem = {
  id: string
  email: string
  name: string
  createdAt: string
  isAdmin: boolean
  subscriptionLabel?: string
}

/** Resposta de assinatura premium para painel admin (registro de quem assinou e quando). */
export type AdminPremiumSubscriptionItem = {
  userId: string
  userName: string
  userEmail: string
  startedAt: string
  endsAt: string
}

export type BusinessItem = {
  id: string
  name: string
  businessType: number
  publicSlug?: string | null
  whatsAppPhone?: string | null
}
