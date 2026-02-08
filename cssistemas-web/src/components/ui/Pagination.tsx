/**
 * Paginação numerada (DRY entre Agendamentos e Ganhos).
 * Só exibe quando há mais de uma página.
 */
type PaginationProps = {
  page: number
  totalCount: number
  pageSize: number
  onPageChange: (page: number) => void
  className?: string
}

export default function Pagination({
  page,
  totalCount,
  pageSize,
  onPageChange,
  className = '',
}: PaginationProps) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  if (totalPages <= 1) return null

  return (
    <div className={`w-full flex justify-center ${className}`.trim()}>
      <div className="inline-flex items-center justify-center gap-1">
        {Array.from({ length: totalPages }, (_, i) => i + 1).map((p) => (
          <button
            key={p}
            type="button"
            onClick={() => onPageChange(p)}
            className={`min-h-[36px] min-w-[36px] rounded-lg text-sm font-medium transition cursor-pointer ${
              p === page
                ? 'bg-primary text-white border border-primary'
                : 'border border-gray-300 text-gray-700 bg-white hover:bg-gray-50'
            }`}
          >
            {p}
          </button>
        ))}
      </div>
    </div>
  )
}
