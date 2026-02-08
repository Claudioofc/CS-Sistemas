/**
 * Tipos compartilhados para Dashboard e Ganhos (DRY).
 */

export type AgendaItemDto = { hora: string; servico: string; cliente: string }
export type ProximoAgendamentoDto = { data: string; hora: string; cliente: string; servico: string }

export type DashboardSummary = {
  proximosAgendamentosCount: number
  clientesHojeCount: number
  faltasCount: number
  ganhosDoMes: number
  agendaDoDia: AgendaItemDto[]
  proximosAgendamentos: ProximoAgendamentoDto[]
}

export type EarningsMonth = {
  year: number
  month: number
  monthLabel: string
  total: number
}

export type EarningsByMonthResponse = {
  months: EarningsMonth[]
}

export type EarningsDetailItem = {
  scheduledAt: string
  clientName: string
  serviceName: string
  price: number
}

export type EarningsDetailResponse = {
  items: EarningsDetailItem[]
}
