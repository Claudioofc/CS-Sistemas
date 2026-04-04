/**
 * Gráfico de barras por mês — reutilizado em Dashboard (Relatório Financeiro) e Ganhos.
 */
import { motion } from 'framer-motion'

type BarItem = { label: string; value: number }

type MonthlyBarChartProps = {
  items: BarItem[]
  formatValue: (n: number) => string
  height?: number
  /** Valor mínimo do topo do eixo Y (evita barra gigante quando só há um valor pequeno). */
  minBarMax?: number
  /** Título do tooltip por barra (opcional). */
  getBarTitle?: (item: BarItem) => string
}

const DEFAULT_HEIGHT = 160
const DEFAULT_MIN_BAR_MAX = 1000

export default function MonthlyBarChart({
  items,
  formatValue,
  height = DEFAULT_HEIGHT,
  minBarMax = DEFAULT_MIN_BAR_MAX,
  getBarTitle,
}: MonthlyBarChartProps) {
  const maxValue = Math.max(...items.map((i) => i.value), 1)
  const chartDisplayMax = Math.max(maxValue, minBarMax)

  return (
    <div className="flex gap-2 sm:gap-3 min-w-0 w-full" style={{ height: height + 40 }}>
      <div className="flex flex-col justify-between py-1 text-xs text-gray-500 font-medium flex-shrink-0 w-8 sm:w-10">
        <span>{formatValue(chartDisplayMax)}</span>
        <span>0</span>
      </div>
      <div className="flex-1 flex items-end gap-1 sm:gap-3 min-w-0 overflow-hidden">
        {items.map((item, i) => {
          const barHeight = chartDisplayMax > 0 ? (item.value / chartDisplayMax) * height : 0
          const title = getBarTitle ? getBarTitle(item) : (item.value > 0 ? formatValue(item.value) : item.label)
          return (
            <div key={i} className="flex-1 min-w-0 flex flex-col items-center justify-end gap-0.5 sm:gap-1 h-full">
              <motion.div
                className="w-full rounded-t bg-primary/80 min-h-[4px] flex-shrink-0"
                initial={{ scaleY: 0, originY: 1 }}
                animate={{ scaleY: 1 }}
                transition={{ delay: i * 0.04, duration: 0.5, ease: "easeOut" }}
                style={{ height: Math.max(barHeight, 4), transformOrigin: 'bottom' }}
                title={title}
              />
              <span className="text-[10px] sm:text-xs text-gray-500 truncate w-full text-center flex-shrink-0">
                {item.label}
              </span>
            </div>
          )
        })}
      </div>
    </div>
  )
}
