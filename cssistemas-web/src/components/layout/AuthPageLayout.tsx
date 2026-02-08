import { APP_NAME } from '../../constants'

/**
 * Layout compartilhado para telas de auth (Login, Criar conta, Esqueci senha, Redefinir senha).
 * DRY: mesmo fundo, ondas e card; responsivo e mobile (100dvh, safe-area, touch targets).
 */
export default function AuthPageLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen min-h-[100dvh] flex overflow-hidden bg-primary relative">
      <div className="absolute inset-0 opacity-30" aria-hidden>
        <svg className="absolute inset-0 w-full h-full" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1440 320" preserveAspectRatio="none">
          <path fill="rgba(255,255,255,0.15)" d="M0,96L48,112C96,128,192,160,288,160C384,160,480,128,576,122.7C672,117,768,139,864,138.7C960,139,1056,117,1152,106.7C1248,96,1344,96,1392,96L1440,96L1440,320L1392,320C1344,320,1248,320,1152,320C1056,320,960,320,864,320C768,320,672,320,576,320C480,320,384,320,288,320C192,320,96,320,48,320L0,320Z" />
          <path fill="rgba(255,255,255,0.1)" d="M0,192L60,186.7C120,181,240,171,360,181.3C480,192,600,224,720,213.3C840,203,960,149,1080,138.7C1200,128,1320,160,1380,176L1440,192L1440,320L1380,320C1320,320,1200,320,1080,320C960,320,840,320,720,320C600,320,480,320,360,320C240,320,120,320,60,320L0,320Z" />
        </svg>
      </div>
      <div className="absolute left-8 top-1/4 w-24 h-24 text-white/20 hidden sm:block" aria-hidden>
        <svg viewBox="0 0 24 24" fill="currentColor" className="w-full h-full">
          <path d="M19 4h-1V2h-2v2H8V2H6v2H5c-1.11 0-1.99.9-1.99 2L3 20c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 16H5V10h14v10zm0-12H5V6h14v2z" />
        </svg>
      </div>
      <div className="absolute left-16 top-2/5 w-20 h-20 text-white/20 hidden sm:block" aria-hidden>
        <svg viewBox="0 0 24 24" fill="currentColor" className="w-full h-full">
          <path d="M11.99 2C6.47 2 2 6.48 2 12s4.47 10 9.99 10C17.52 22 22 17.52 22 12S17.52 2 11.99 2zM12 20c-4.42 0-8-3.58-8-8s3.58-8 8-8 8 3.58 8 8-3.58 8-8 8zm.5-13H11v6l5.25 3.15.75-1.23-4.5-2.67z" />
        </svg>
      </div>
      <div className="absolute left-10 top-1/2 w-20 h-20 text-white/20 hidden sm:block" aria-hidden>
        <svg viewBox="0 0 24 24" fill="currentColor" className="w-full h-full">
          <path d="M20 2H4c-1.1 0-2 .9-2 2v18l4-4h14c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2zm0 14H5.17L4 17.17V4h16v12z" />
          <circle cx="8" cy="10" r="1.5" fill="currentColor" />
          <circle cx="12" cy="10" r="1.5" fill="currentColor" />
          <circle cx="16" cy="10" r="1.5" fill="currentColor" />
        </svg>
      </div>
      <div className="relative z-10 flex items-center justify-center min-h-screen min-h-[100dvh] w-full px-4 py-6 pb-[max(1.5rem,env(safe-area-inset-bottom))]">
        <div className="w-full max-w-md bg-white rounded-2xl shadow-xl p-6 sm:p-8 md:p-10">
          {children}
        </div>
      </div>
    </div>
  )
}

/** Cabeçalho do card (logo + título opcional). centerTitle centraliza logo + CS Sistemas e o título. */
export function AuthCardHeader({ title, centerTitle }: { title?: string; centerTitle?: boolean }) {
  return (
    <>
      <div className={`flex items-center gap-3 mb-2 ${centerTitle ? 'justify-center' : ''}`}>
        <img src="/livro-de-contato.svg" alt={APP_NAME} className="h-12 w-auto" />
        <span className="text-xl sm:text-2xl text-primary">
          <span className="font-bold">CS</span> <span className="font-semibold">Sistemas</span>
        </span>
      </div>
      {title && <p className={`text-gray-800 text-lg mb-8 ${centerTitle ? 'text-center' : ''}`}>{title}</p>}
    </>
  )
}
