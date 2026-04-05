import { useState } from 'react'
import { apiGet, apiPostWithAuth } from '../../api/client'

interface Props {
  token: string
  onDone: () => void
}

export default function SurveyModal({ token, onDone }: Props) {
  const [score, setScore] = useState<number | null>(null)
  const [comment, setComment] = useState('')
  const [saving, setSaving] = useState(false)
  const [done, setDone] = useState(false)

  async function handleSubmit() {
    if (score === null) return
    setSaving(true)
    await apiPostWithAuth('/api/survey', { score, comment: comment.trim() || null }, token)
    setSaving(false)
    setDone(true)
    setTimeout(onDone, 1800)
  }

  async function handleDismiss() {
    await apiPostWithAuth('/api/survey/dismiss', {}, token)
    onDone()
  }

  const scoreLabels: Record<number, string> = {
    0: 'Péssimo', 1: 'Muito ruim', 2: 'Ruim', 3: 'Insatisfatório', 4: 'Regular',
    5: 'Neutro', 6: 'Ok', 7: 'Bom', 8: 'Muito bom', 9: 'Ótimo', 10: 'Excelente'
  }

  function scoreColor(n: number) {
    if (n <= 3) return 'bg-red-500 text-white border-red-500'
    if (n <= 6) return 'bg-amber-400 text-white border-amber-400'
    return 'bg-green-500 text-white border-green-500'
  }

  function scoreIdleColor(n: number) {
    if (n <= 3) return 'border-red-300 text-red-500 hover:bg-red-50'
    if (n <= 6) return 'border-amber-300 text-amber-600 hover:bg-amber-50'
    return 'border-green-300 text-green-600 hover:bg-green-50'
  }

  return (
    <>
      <div className="fixed inset-0 bg-black/40 z-[60]" aria-hidden onClick={handleDismiss} />
      <div className="fixed inset-0 z-[70] flex items-end sm:items-center justify-center p-4">
        <div
          className="bg-white rounded-2xl shadow-xl w-full max-w-md p-6"
          role="dialog"
          aria-modal="true"
          aria-labelledby="survey-title"
        >
          {done ? (
            <div className="text-center py-4">
              <p className="text-3xl mb-2">🙏</p>
              <p className="text-lg font-semibold text-gray-900">Obrigado pelo feedback!</p>
              <p className="text-sm text-gray-500 mt-1">Sua opinião nos ajuda a melhorar.</p>
            </div>
          ) : (
            <>
              <h2 id="survey-title" className="text-base font-semibold text-gray-900 mb-1">
                Como você avalia o CS Sistemas?
              </h2>
              <p className="text-sm text-gray-500 mb-5">
                De 0 a 10, qual a chance de recomendar para um colega?
              </p>

              {/* Grade NPS */}
              <div className="flex gap-1.5 flex-wrap mb-2">
                {Array.from({ length: 11 }, (_, i) => (
                  <button
                    key={i}
                    type="button"
                    onClick={() => setScore(i)}
                    className={`w-9 h-9 rounded-lg border-2 text-sm font-semibold transition ${
                      score === i ? scoreColor(i) : `bg-white ${scoreIdleColor(i)}`
                    }`}
                  >
                    {i}
                  </button>
                ))}
              </div>
              <div className="flex justify-between text-xs text-gray-400 mb-5">
                <span>Não recomendaria</span>
                <span>Recomendaria muito</span>
              </div>

              {score !== null && (
                <p className="text-xs text-center text-gray-500 mb-4 -mt-3">
                  {scoreLabels[score]}
                </p>
              )}

              {/* Comentário */}
              <div className="mb-5">
                <label htmlFor="survey-comment" className="block text-sm font-medium text-gray-700 mb-1">
                  O que podemos melhorar? <span className="text-gray-400 font-normal">(opcional)</span>
                </label>
                <textarea
                  id="survey-comment"
                  rows={3}
                  value={comment}
                  onChange={(e) => setComment(e.target.value)}
                  placeholder="Conte o que sentiu falta ou o que achou difícil..."
                  className="w-full rounded-xl border border-gray-300 px-3 py-2 text-sm text-gray-900 focus:ring-2 focus:ring-primary focus:border-primary resize-none"
                />
              </div>

              <div className="flex gap-2">
                <button
                  type="button"
                  onClick={handleSubmit}
                  disabled={score === null || saving}
                  className="flex-1 min-h-[44px] rounded-xl bg-primary text-white font-medium text-sm hover:bg-primary/90 disabled:opacity-50"
                >
                  {saving ? 'Enviando...' : 'Enviar avaliação'}
                </button>
                <button
                  type="button"
                  onClick={handleDismiss}
                  className="min-h-[44px] px-4 rounded-xl border border-gray-300 text-gray-600 text-sm hover:bg-gray-50"
                >
                  Agora não
                </button>
              </div>
            </>
          )}
        </div>
      </div>
    </>
  )
}

/** Verifica com a API se o usuário deve ver o modal de pesquisa. */
export async function checkSurveyEligibility(token: string): Promise<boolean> {
  const res = await apiGet<{ eligible: boolean }>('/api/survey/eligibility', token)
  return res.ok && res.data.eligible
}
