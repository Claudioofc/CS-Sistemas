/** 0 = CPF, 1 = CNPJ (alinhado ao backend). */
export type DocumentType = 0 | 1

/** Formata apenas dígitos para exibição: CPF xxx.xxx.xxx-xx ou CNPJ xx.xxx.xxx/xxxx-xx. */
export function formatDocument(digits: string, documentType: DocumentType): string {
  const d = digits.replace(/\D/g, '')
  if (documentType === 0) {
    if (d.length <= 3) return d
    if (d.length <= 6) return `${d.slice(0, 3)}.${d.slice(3)}`
    if (d.length <= 9) return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6)}`
    return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6, 9)}-${d.slice(9, 11)}`
  }
  if (d.length <= 2) return d
  if (d.length <= 5) return `${d.slice(0, 2)}.${d.slice(2)}`
  if (d.length <= 8) return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5)}`
  if (d.length <= 12) return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5, 8)}/${d.slice(8)}`
  return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5, 8)}/${d.slice(8, 12)}-${d.slice(12, 14)}`
}

/** Aceita apenas caracteres válidos para CPF/CNPJ e formata (limite 11 ou 14 dígitos). */
export function filterDocumentInput(value: string, documentType: DocumentType): string {
  const isCpf = documentType === 0
  const allowed = isCpf ? /[\d.-]/g : /[\d./-]/g
  const filtered = (value.match(allowed) ?? []).join('')
  const digits = filtered.replace(/\D/g, '')
  const maxDigits = isCpf ? 11 : 14
  const truncated = digits.slice(0, maxDigits)
  if (truncated.length <= 3) return truncated
  if (isCpf) {
    if (truncated.length <= 6) return `${truncated.slice(0, 3)}.${truncated.slice(3)}`
    if (truncated.length <= 9) return `${truncated.slice(0, 3)}.${truncated.slice(3, 6)}.${truncated.slice(6)}`
    return `${truncated.slice(0, 3)}.${truncated.slice(3, 6)}.${truncated.slice(6, 9)}-${truncated.slice(9)}`
  }
  if (truncated.length <= 5) return `${truncated.slice(0, 2)}.${truncated.slice(2)}`
  if (truncated.length <= 8) return `${truncated.slice(0, 2)}.${truncated.slice(2, 5)}.${truncated.slice(5)}`
  if (truncated.length <= 12) return `${truncated.slice(0, 2)}.${truncated.slice(2, 5)}.${truncated.slice(5, 8)}/${truncated.slice(8)}`
  return `${truncated.slice(0, 2)}.${truncated.slice(2, 5)}.${truncated.slice(5, 8)}/${truncated.slice(8, 12)}-${truncated.slice(12)}`
}

export function getDocumentRequiredLength(documentType: DocumentType): number {
  return documentType === 0 ? 11 : 14
}

export function getDocumentPlaceholder(documentType: DocumentType | ''): string {
  if (documentType === 0) return '000.000.000-00'
  if (documentType === 1) return '00.000.000/0001-00'
  return 'Selecione CPF ou CNPJ'
}

export function getDocumentMaxLength(documentType: DocumentType | ''): number {
  return documentType === 0 ? 14 : 18
}
