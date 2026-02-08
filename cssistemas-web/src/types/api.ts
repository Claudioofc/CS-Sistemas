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

export type BusinessItem = {
  id: string
  name: string
  businessType: number
  publicSlug?: string | null
  whatsAppPhone?: string | null
}
