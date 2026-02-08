/**
 * Exportação para CSV (abre no Excel). DRY para qualquer lista de colunas.
 */

const CSV_SEP = ';'
const CSV_QUOTE = '"'

function escapeCsvCell(value: string): string {
  const s = String(value ?? '')
  if (s.includes(CSV_SEP) || s.includes(CSV_QUOTE) || s.includes('\n') || s.includes('\r')) {
    return CSV_QUOTE + s.replace(/"/g, '""') + CSV_QUOTE
  }
  return s
}

/**
 * Gera CSV a partir de linhas com chaves iguais (objetos com mesmas keys).
 * Usa ; como separador (Excel pt-BR reconhece).
 */
export function buildCsv(rows: Record<string, string>[]): string {
  if (rows.length === 0) return ''
  const headers = Object.keys(rows[0])
  const headerLine = headers.map((h) => escapeCsvCell(h)).join(CSV_SEP)
  const dataLines = rows.map((row) => headers.map((h) => escapeCsvCell(row[h] ?? '')).join(CSV_SEP))
  return [headerLine, ...dataLines].join('\r\n')
}

/**
 * Faz download de um arquivo CSV (nome sugerido sem extensão ou com .csv).
 */
export function downloadCsv(content: string, filenameBase: string): void {
  const name = filenameBase.endsWith('.csv') ? filenameBase : `${filenameBase}.csv`
  const blob = new Blob(['\uFEFF' + content], { type: 'text/csv;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = name
  a.click()
  URL.revokeObjectURL(url)
}
