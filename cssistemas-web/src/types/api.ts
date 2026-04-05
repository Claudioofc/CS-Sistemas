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

/** Resposta de assinatura premium para painel admin (registro de quem assinou, quando e valor). */
export type AdminPremiumSubscriptionItem = {
  userId: string
  userName: string
  userEmail: string
  startedAt: string
  endsAt: string
  planName: string
  price: number
}

export type BusinessItem = {
  id: string
  name: string
  businessType: number
  publicSlug?: string | null
  logoUrl?: string | null
}

export type EmployeeServicePriceItem = {
  serviceId: string
  price: number
}

export type EmployeeItem = {
  id: string
  name: string
  role?: string | null
  isActive: boolean
  servicePrices?: EmployeeServicePriceItem[]
}
