const AudioContextClass = typeof window !== 'undefined'
  ? (window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext)
  : null

let sharedContext: AudioContext | null = null

function getContext(): AudioContext | null {
  if (!AudioContextClass) return null
  if (sharedContext) return sharedContext
  try {
    sharedContext = new AudioContext()
    return sharedContext
  } catch {
    return null
  }
}

/** Toca um tom (única implementação — DRY). gain/duração em segundos; rampTo opcional para fade-out. */
function playTone(
  ctx: AudioContext,
  frequency: number,
  gainValue: number,
  durationSeconds: number,
  rampTo?: number
): void {
  try {
    const osc = ctx.createOscillator()
    const gain = ctx.createGain()
    osc.connect(gain)
    gain.connect(ctx.destination)
    osc.frequency.value = frequency
    osc.type = 'sine'
    const t = ctx.currentTime
    gain.gain.setValueAtTime(gainValue, t)
    if (rampTo !== undefined) {
      gain.gain.exponentialRampToValueAtTime(Math.max(rampTo, 0.0001), t + durationSeconds)
    }
    osc.start(t)
    osc.stop(t + durationSeconds)
  } catch {
    // ignora falha ao tocar
  }
}

function playBeep(ctx: AudioContext): void {
  playTone(ctx, 800, 0.15, 0.15, 0.01)
}

/** Som imperceptível para desbloquear o áudio no navegador (após gesto do usuário). */
function playSilentUnlock(ctx: AudioContext): void {
  playTone(ctx, 800, 0.001, 0.01)
}

/**
 * Chame na primeira interação do usuário (ex.: clique no sino) para liberar o áudio.
 * Toca um som imperceptível para desbloquear; depois o beep de notificação funcionará pelo timer.
 */
export function warmupNotificationSound(): void {
  const ctx = getContext()
  if (!ctx) return
  const doUnlock = () => {
    if (ctx.state === 'suspended') {
      ctx.resume().then(() => playSilentUnlock(ctx)).catch(() => {})
    } else {
      playSilentUnlock(ctx)
    }
  }
  doUnlock()
}

/**
 * Toca um bipe curto para aviso de notificação (Web Audio API, sem arquivo externo).
 */
export function playNotificationBeep(): void {
  try {
    const ctx = getContext()
    if (!ctx) return
    const play = () => playBeep(ctx)
    if (ctx.state === 'suspended') {
      ctx.resume().then(play).catch(() => {})
    } else {
      play()
    }
  } catch {
    // Ignora se o navegador bloquear áudio (ex.: sem interação do usuário)
  }
}
