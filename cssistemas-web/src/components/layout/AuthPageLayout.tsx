import { motion } from 'framer-motion'
import { APP_NAME } from '../../constants'

const benefits = [
  {
    icon: (
      <svg viewBox="0 0 24 24" fill="currentColor" className="w-5 h-5">
        <path d="M19 4h-1V2h-2v2H8V2H6v2H5c-1.11 0-1.99.9-1.99 2L3 20c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 16H5V10h14v10zm0-12H5V6h14v2z" />
      </svg>
    ),
    text: 'Agendamentos organizados em um só lugar',
  },
  {
    icon: (
      <svg viewBox="0 0 24 24" fill="currentColor" className="w-5 h-5">
        <path d="M11.99 2C6.47 2 2 6.48 2 12s4.47 10 9.99 10C17.52 22 22 17.52 22 12S17.52 2 11.99 2zM12 20c-4.42 0-8-3.58-8-8s3.58-8 8-8 8 3.58 8 8-3.58 8-8 8zm.5-13H11v6l5.25 3.15.75-1.23-4.5-2.67z" />
      </svg>
    ),
    text: 'Lembretes automáticos para seus clientes',
  },
  {
    icon: (
      <svg viewBox="0 0 24 24" fill="currentColor" className="w-5 h-5">
        <path d="M20 4H4c-1.11 0-1.99.89-1.99 2L2 18c0 1.11.89 2 2 2h16c1.11 0 2-.89 2-2V6c0-1.11-.89-2-2-2zm0 14H4v-6h16v6zm0-10H4V6h16v2z" />
      </svg>
    ),
    text: 'Pagamentos via PIX e cartão integrados',
  },
]

/**
 * Layout compartilhado para telas de auth (Login, Criar conta, Esqueci senha, Redefinir senha).
 * Split screen: painel esquerdo com branding (desktop), formulário à direita.
 */
export default function AuthPageLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen min-h-[100dvh] flex overflow-hidden">
      <style>{`
        @keyframes floatOrb {
          0%, 100% { transform: translateY(0) scale(1); }
          50%       { transform: translateY(-20px) scale(1.05); }
        }
        @keyframes floatIcon {
          0%, 100% { transform: translateY(0) rotate(0deg); }
          50%       { transform: translateY(-10px) rotate(5deg); }
        }
        @keyframes waveMove {
          from { transform: translateX(0); }
          to   { transform: translateX(-3%); }
        }
        @keyframes fadeSlideUp {
          from { opacity: 0; transform: translateY(16px); }
          to   { opacity: 1; transform: translateY(0); }
        }
        @keyframes cardShake {
          0%, 100% { transform: translateX(0); }
          20%       { transform: translateX(-8px); }
          40%       { transform: translateX(8px); }
          60%       { transform: translateX(-5px); }
          80%       { transform: translateX(5px); }
        }
        .shake { animation: cardShake 0.45s ease; }
      `}</style>

      {/* ── Painel esquerdo (apenas desktop) ── */}
      <div className="hidden lg:flex lg:w-[44%] relative bg-primary flex-col justify-between overflow-hidden px-10 py-10">
        {/* Ondas */}
        <svg className="absolute bottom-0 left-0 w-full" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1440 320" preserveAspectRatio="none" style={{ animation: 'waveMove 8s ease-in-out infinite alternate' }} aria-hidden>
          <path fill="rgba(255,255,255,0.10)" d="M0,96L48,112C96,128,192,160,288,160C384,160,480,128,576,122.7C672,117,768,139,864,138.7C960,139,1056,117,1152,106.7C1248,96,1344,96,1392,96L1440,96L1440,320L1392,320L1344,320L1248,320L1152,320L1056,320L960,320L864,320L768,320L672,320L576,320L480,320L384,320L288,320L192,320L96,320L48,320L0,320Z" />
          <path fill="rgba(255,255,255,0.06)" d="M0,192L60,186.7C120,181,240,171,360,181.3C480,192,600,224,720,213.3C840,203,960,149,1080,138.7C1200,128,1320,160,1380,176L1440,192L1440,320L1380,320L1320,320L1200,320L1080,320L960,320L840,320L720,320L600,320L480,320L360,320L240,320L120,320L60,320L0,320Z" />
        </svg>

        {/* Orbs */}
        <div className="absolute top-16 right-10 w-40 h-40 rounded-full bg-white/10 blur-3xl" style={{ animation: 'floatOrb 7s ease-in-out infinite' }} aria-hidden />
        <div className="absolute bottom-32 left-6 w-32 h-32 rounded-full bg-white/10 blur-2xl" style={{ animation: 'floatOrb 9s ease-in-out infinite reverse' }} aria-hidden />

        {/* Ícones flutuantes */}
        <div className="absolute top-1/3 right-8 w-12 h-12 text-white/20" style={{ animation: 'floatIcon 6s ease-in-out infinite' }} aria-hidden>
          <svg viewBox="0 0 24 24" fill="currentColor" className="w-full h-full">
            <path d="M19 4h-1V2h-2v2H8V2H6v2H5c-1.11 0-1.99.9-1.99 2L3 20c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 16H5V10h14v10zm0-12H5V6h14v2z" />
          </svg>
        </div>
        <div className="absolute bottom-1/3 right-14 w-9 h-9 text-white/20" style={{ animation: 'floatIcon 8s ease-in-out infinite 1.5s' }} aria-hidden>
          <svg viewBox="0 0 24 24" fill="currentColor" className="w-full h-full">
            <path d="M11.99 2C6.47 2 2 6.48 2 12s4.47 10 9.99 10C17.52 22 22 17.52 22 12S17.52 2 11.99 2zM12 20c-4.42 0-8-3.58-8-8s3.58-8 8-8 8 3.58 8 8-3.58 8-8 8zm.5-13H11v6l5.25 3.15.75-1.23-4.5-2.67z" />
          </svg>
        </div>

        {/* Conteúdo */}
        <div className="relative z-10">
          <div className="flex items-center gap-3 mb-10">
            <img src="/livro-de-contato.svg" alt={APP_NAME} className="h-10 w-auto brightness-0 invert" />
            <span className="text-2xl text-white">
              <span className="font-bold">CS</span> <span className="font-semibold">Sistemas</span>
            </span>
          </div>

          <h1 className="text-3xl font-bold text-white leading-tight mb-3">
            Gerencie sua agenda<br />com facilidade
          </h1>
          <p className="text-white/70 text-base mb-10">
            Tudo que você precisa para organizar clientes, serviços e pagamentos.
          </p>

          <div className="space-y-4">
            {benefits.map((b, i) => (
              <div
                key={i}
                className="flex items-center gap-3 opacity-0"
                style={{ animation: `fadeSlideUp 0.4s ease-out forwards`, animationDelay: `${0.3 + i * 0.15}s` }}
              >
                <div className="w-9 h-9 rounded-xl bg-white/15 flex items-center justify-center shrink-0 text-white">
                  {b.icon}
                </div>
                <p className="text-white/85 text-sm">{b.text}</p>
              </div>
            ))}
          </div>
        </div>

        <p className="relative z-10 text-white/40 text-xs">© {APP_NAME} 2026</p>
      </div>

      {/* ── Painel direito (formulário) ── */}
      <div className="flex-1 flex items-center justify-center bg-gray-50 px-4 py-8 sm:px-8 overflow-y-auto">
        <motion.div
          initial={{ opacity: 0, y: 30, scale: 0.97 }}
          animate={{ opacity: 1, y: 0, scale: 1 }}
          transition={{ duration: 0.4, ease: 'easeOut' }}
          className="w-full max-w-md bg-white rounded-2xl shadow-xl p-6 sm:p-8"
        >
          {/* Logo visível apenas no mobile (painel esquerdo fica oculto) */}
          <div className="flex items-center gap-2 mb-6 lg:hidden">
            <img src="/livro-de-contato.svg" alt={APP_NAME} className="h-8 w-auto" />
            <span className="text-lg text-primary">
              <span className="font-bold">CS</span> <span className="font-semibold">Sistemas</span>
            </span>
          </div>

          {children}
        </motion.div>
      </div>
    </div>
  )
}

/** Cabeçalho do card (título). */
export function AuthCardHeader({ title, centerTitle }: { title?: string; centerTitle?: boolean }) {
  return (
    <>
      {title && (
        <p className={`text-gray-800 text-xl font-semibold mb-6 ${centerTitle ? 'text-center' : ''}`}>
          {title}
        </p>
      )}
    </>
  )
}
