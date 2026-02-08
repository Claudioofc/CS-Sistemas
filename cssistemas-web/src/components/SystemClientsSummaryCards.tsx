/**
 * Cards de resumo: total, premium e gratuitos (DRY entre AdminDashboard e Clientes).
 */
type SystemClientsSummaryCardsProps = {
  total: number
  premiumCount: number
  gratuitosCount: number
  /** Legenda opcional sob o total (ex.: "Clientes ativos (premium + gratuitos)"). */
  totalSubtitle?: string
}

export default function SystemClientsSummaryCards({
  total,
  premiumCount,
  gratuitosCount,
  totalSubtitle,
}: SystemClientsSummaryCardsProps) {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6">
      <div className="bg-white rounded-xl border border-gray-200 p-4 shadow-sm">
        <p className="text-sm font-medium text-gray-500 uppercase tracking-wider">Total de clientes</p>
        <p className="text-2xl font-bold text-gray-900 mt-1">{total}</p>
        {totalSubtitle && <p className="text-xs text-gray-500 mt-0.5">{totalSubtitle}</p>}
      </div>
      <div className="bg-white rounded-xl border border-gray-200 p-4 shadow-sm">
        <p className="text-sm font-medium text-gray-500 uppercase tracking-wider">Clientes premium</p>
        <p className="text-2xl font-bold text-primary mt-1">{premiumCount}</p>
      </div>
      <div className="bg-white rounded-xl border border-gray-200 p-4 shadow-sm">
        <p className="text-sm font-medium text-gray-500 uppercase tracking-wider">Clientes gratuitos</p>
        <p className="text-2xl font-bold text-gray-900 mt-1">{gratuitosCount}</p>
      </div>
    </div>
  )
}
