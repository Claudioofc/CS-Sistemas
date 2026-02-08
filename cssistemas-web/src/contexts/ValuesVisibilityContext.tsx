import { createContext, useContext, useState, useCallback, useEffect, useMemo } from 'react'
import { STORAGE_VALUES_VISIBLE_KEY } from '../constants'
import { formatCurrency, MASKED_CURRENCY } from '../utils/format'

type ValuesVisibilityContextType = {
  valuesVisible: boolean
  toggleValuesVisibility: () => void
}

const ValuesVisibilityContext = createContext<ValuesVisibilityContextType | null>(null)

/** Sempre inicia com olho fechado (valores ocultos). O usuário pode clicar no olho para mostrar. */
export function ValuesVisibilityProvider({ children }: { children: React.ReactNode }) {
  const [valuesVisible, setValuesVisible] = useState(false)

  useEffect(() => {
    try {
      localStorage.setItem(STORAGE_VALUES_VISIBLE_KEY, String(valuesVisible))
    } catch {
      // ignore
    }
  }, [valuesVisible])

  const toggleValuesVisibility = useCallback(() => {
    setValuesVisible((prev) => !prev)
  }, [])

  return (
    <ValuesVisibilityContext.Provider value={{ valuesVisible, toggleValuesVisibility }}>
      {children}
    </ValuesVisibilityContext.Provider>
  )
}

export function useValuesVisibility(): ValuesVisibilityContextType {
  const ctx = useContext(ValuesVisibilityContext)
  if (!ctx) {
    return {
      valuesVisible: false,
      toggleValuesVisibility: () => {},
    }
  }
  return ctx
}

/** Formata valor em R$ ou máscara conforme preferência (DRY entre Dashboard e Ganhos). */
export function useDisplayCurrency(): (value: number) => string {
  const { valuesVisible } = useValuesVisibility()
  return useMemo(
    () => (value: number) => (valuesVisible ? formatCurrency(value) : MASKED_CURRENCY),
    [valuesVisible]
  )
}
