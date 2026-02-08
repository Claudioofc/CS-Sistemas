const BRAZIL_TZ = 'America/Sao_Paulo'

/**
 * Formata data/hora ISO para exibição em pt-BR.
 */
export function formatDate(iso: string): string {
  try {
    const d = new Date(iso)
    return d.toLocaleDateString('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    })
  } catch {
    return iso
  }
}

/**
 * Formata apenas a data (dd/mm/aaaa) em pt-BR. timeZone opcional (padrão Brasil).
 */
export function formatDateOnly(iso: string, timeZone: string = BRAZIL_TZ): string {
  try {
    const d = new Date(iso)
    return d.toLocaleDateString('pt-BR', { timeZone, day: '2-digit', month: '2-digit', year: 'numeric' })
  } catch {
    return iso
  }
}

/**
 * Formata apenas o horário (HH:mm) em pt-BR. timeZone opcional (padrão Brasil).
 */
export function formatTimeOnly(iso: string, timeZone: string = BRAZIL_TZ): string {
  try {
    const d = new Date(iso)
    return d.toLocaleTimeString('pt-BR', { timeZone, hour: '2-digit', minute: '2-digit', hour12: false })
  } catch {
    return iso
  }
}

/**
 * Formata data e hora para exibição "dd/mm/aaaa às HH:mm" (DRY para listas de agendamentos/ganhos).
 */
export function formatDateAndTime(iso: string, timeZone: string = BRAZIL_TZ): string {
  return `${formatDateOnly(iso, timeZone)} às ${formatTimeOnly(iso, timeZone)}`
}

const currencyFormatter = new Intl.NumberFormat('pt-BR', {
  style: 'currency',
  currency: 'BRL',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})

/**
 * Formata valor monetário em R$ (pt-BR).
 */
export function formatCurrency(value: number): string {
  return currencyFormatter.format(value)
}

/** Texto exibido quando valores estão ocultos (estilo banco). */
export const MASKED_CURRENCY = 'R$ •••••••'

/**
 * Retorna o primeiro nome (ou fallback) a partir do nome completo.
 * Use FALLBACK_USER_NAME de constants.ts para manter DRY.
 */
export function getFirstName(name: string | null | undefined, fallback: string = 'Usuário'): string {
  const trimmed = name?.trim() || fallback
  const first = trimmed.split(/\s+/)[0]
  return first || fallback
}
