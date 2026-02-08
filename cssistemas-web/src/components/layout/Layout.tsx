import { useState, useEffect, useCallback, useRef } from 'react'
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../../contexts/AuthContext'
import { ValuesVisibilityProvider } from '../../contexts/ValuesVisibilityContext'
import { apiGet, apiPatch, apiPostWithAuth, getProfilePhotoUrl } from '../../api/client'
import { APP_NAME, ROUTES, FALLBACK_USER_NAME } from '../../constants'
import { getFirstName, formatDateAndTime } from '../../utils/format'
import { playNotificationBeep, warmupNotificationSound } from '../../utils/sound'

type NotificationItem = {
  id: string
  type: string
  clientName: string
  scheduledAt: string
  readAt: string | null
  createdAt: string
}

const navItemsBase = [
  { path: ROUTES.DASHBOARD, label: 'Dashboard', icon: 'M4 5a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM4 15a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1H5a1 1 0 01-1-1v-4zM14 5a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1h-4a1 1 0 01-1-1V5zM14 15a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1h-4a1 1 0 01-1-1v-4z' },
  { path: ROUTES.AGENDAMENTOS, label: 'Agendamentos', icon: 'M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z' },
  { path: ROUTES.SERVICOS, label: 'Serviços', icon: 'M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10' },
  { path: ROUTES.CLIENTES, label: 'Clientes', icon: 'M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z' },
  { path: ROUTES.GANHOS, label: 'Ganhos', icon: 'M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z' },
  { path: ROUTES.CONFIGURACOES, label: 'Configurações', icon: 'M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z M15 12a3 3 0 11-6 0 3 3 0 016 0z' },
]
const premiumPath = { path: ROUTES.PREMIUM, label: 'Premium', icon: 'M5 3v4M3 5h4M6 17v4m-2-2h4m5-16l2.286 6.857L21 12l-5.714 2.143L13 21l-2.286-6.857L5 12l5.714-2.143L13 3z' }

function Icon({ d }: { d: string }) {
  return (
    <svg className="w-6 h-6 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden>
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d={d} />
    </svg>
  )
}

export default function Layout() {
  const { user, token, logout, subscriptionStatus } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const [userMenuOpen, setUserMenuOpen] = useState(false)
  const [notificationsOpen, setNotificationsOpen] = useState(false)
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const [photoError, setPhotoError] = useState(false)
  const [notifications, setNotifications] = useState<NotificationItem[]>([])
  const [notificationsLoading, setNotificationsLoading] = useState(false)
  const previousNotificationCountRef = useRef<number | null>(null)
  const [supportModalOpen, setSupportModalOpen] = useState(false)
  const [supportMessage, setSupportMessage] = useState('')
  const [supportSubmitting, setSupportSubmitting] = useState(false)
  const [supportError, setSupportError] = useState<string | null>(null)
  const [supportSuccess, setSupportSuccess] = useState(false)

  const fetchNotifications = useCallback(async () => {
    if (!token) return
    setNotificationsLoading(true)
    const res = await apiGet<NotificationItem[]>('/api/notifications', token)
    setNotificationsLoading(false)
    if (res.ok) {
      const count = res.data.length
      if (previousNotificationCountRef.current !== null && count > previousNotificationCountRef.current) {
        playNotificationBeep()
      }
      previousNotificationCountRef.current = count
      setNotifications(res.data)
    }
  }, [token])

  async function handleCloseNotification(id: string) {
    if (!token) return
    const res = await apiPatch<Record<string, never>, unknown>(`/api/notifications/${id}/read`, {}, token)
    if (res.ok) setNotifications((prev) => prev.filter((n) => n.id !== id))
  }

  async function handleSupportSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!token || !supportMessage.trim()) return
    setSupportError(null)
    setSupportSubmitting(true)
    const res = await apiPostWithAuth<{ message: string; pageUrl?: string }, { message?: string }>(
      '/api/support/contact',
      { message: supportMessage.trim(), pageUrl: typeof window !== 'undefined' ? window.location.href : undefined },
      token
    )
    setSupportSubmitting(false)
    if (res.ok) {
      setSupportSuccess(true)
      setSupportMessage('')
      setTimeout(() => { setSupportModalOpen(false); setSupportSuccess(false) }, 2000)
    } else {
      const err = res.error
      const msg = err && ('message' in err ? err.message : (err as { mensagem?: string }).mensagem)
      setSupportError(msg ?? 'Erro ao enviar. Tente novamente.')
    }
  }

  function openSupportModal() {
    setSupportModalOpen(true)
    setSupportMessage('')
    setSupportError(null)
    setSupportSuccess(false)
    closeSidebar()
  }

  useEffect(() => {
    if (token) fetchNotifications()
  }, [token, fetchNotifications])

  useEffect(() => {
    if (notificationsOpen && token) fetchNotifications()
  }, [notificationsOpen, token, fetchNotifications])

  useEffect(() => {
    if (!token) return
    const interval = setInterval(() => {
      if (typeof document !== 'undefined' && document.visibilityState === 'visible') {
        fetchNotifications()
      }
    }, 15000)
    return () => clearInterval(interval)
  }, [token, fetchNotifications])

  // Libera o áudio na primeira interação (clique ou tecla) na área logada — beep de notificação funciona após isso
  useEffect(() => {
    if (!token) return
    const once = () => warmupNotificationSound()
    document.addEventListener('click', once, { once: true })
    document.addEventListener('keydown', once, { once: true })
    return () => {
      document.removeEventListener('click', once)
      document.removeEventListener('keydown', once)
    }
  }, [token])

  useEffect(() => {
    if (!user?.isAdmin && subscriptionStatus && !subscriptionStatus.hasAccess && location.pathname !== ROUTES.PLANOS) {
      navigate(ROUTES.PLANOS, { replace: true })
    }
  }, [user?.isAdmin, subscriptionStatus, location.pathname, navigate])

  useEffect(() => {
    if (sidebarOpen) document.body.style.overflow = 'hidden'
    else document.body.style.overflow = ''
    return () => { document.body.style.overflow = '' }
  }, [sidebarOpen])

  useEffect(() => {
    setPhotoError(false)
  }, [user?.profilePhotoUrl])

  const closeSidebar = () => setSidebarOpen(false)
  const showPhoto = user?.profilePhotoUrl && !photoError

  return (
    <ValuesVisibilityProvider>
    <div className="min-h-screen min-h-[100dvh] flex bg-white">
      {/* Backdrop mobile */}
      {sidebarOpen && (
        <div
          className="fixed inset-0 bg-black/50 z-20 md:hidden"
          aria-hidden
          onClick={closeSidebar}
        />
      )}
      {/* Sidebar: drawer no mobile, fixo a partir de md */}
      <aside
        className={`fixed md:relative inset-y-0 left-0 z-30 w-64 bg-primary flex flex-col flex-shrink-0 transform transition-transform duration-200 ease-out md:translate-x-0 ${sidebarOpen ? 'translate-x-0' : '-translate-x-full'}`}
        style={{ paddingTop: 'env(safe-area-inset-top)', paddingLeft: 'env(safe-area-inset-left)' }}
      >
        <div className="p-4 flex items-center gap-2">
          <div className="w-9 h-9 rounded-full bg-white flex items-center justify-center flex-shrink-0 p-1.5">
            <img src="/livro-de-contato.svg" alt="" className="w-full h-full object-contain" aria-hidden />
          </div>
          <span className="text-white font-semibold text-lg">{APP_NAME}</span>
        </div>
        <nav className="flex-1 py-4 px-2 space-y-0.5">
          {navItemsBase.map((item) => {
            const isActive = location.pathname === item.path
            return (
              <Link
                key={item.path}
                to={item.path}
                onClick={closeSidebar}
                className={`flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition min-h-[44px] items-center ${
                  isActive ? 'bg-white/20 text-white' : 'text-white/80 hover:bg-white/10 hover:text-white active:bg-white/10'
                }`}
              >
                <Icon d={item.icon} />
                {item.label}
              </Link>
            )
          })}
          <div className="pt-4 mt-4 border-t border-white/20">
            <button
              type="button"
              onClick={openSupportModal}
              className="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium text-white/80 hover:bg-white/10 hover:text-white transition min-h-[44px] items-center w-full text-left cursor-pointer"
            >
              <Icon d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
              Fale conosco
            </button>
            {!user?.isAdmin && (
              <Link
                to={premiumPath.path}
                onClick={closeSidebar}
                className="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium text-white/80 hover:bg-white/10 hover:text-white transition min-h-[44px] items-center mt-0.5"
              >
                <Icon d={premiumPath.icon} />
                {premiumPath.label}
              </Link>
            )}
            {user?.isAdmin && (
              <Link
                to={ROUTES.ADMIN}
                onClick={closeSidebar}
                className={`flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition mt-0.5 min-h-[44px] items-center ${
                  location.pathname === ROUTES.ADMIN ? 'bg-white/20 text-white' : 'text-white/80 hover:bg-white/10 hover:text-white'
                }`}
              >
                <Icon d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
                Clientes cadastrados
              </Link>
            )}
          </div>
        </nav>
      </aside>

      {/* Main */}
      <div
        className="flex-1 flex flex-col min-w-0"
        style={{ paddingTop: 'env(safe-area-inset-top)', paddingRight: 'env(safe-area-inset-right)', paddingBottom: 'env(safe-area-inset-bottom)' }}
      >
        {/* Header: mobile = hamburger + logo + trial + ícones; desktop = + Bem-vindo no centro */}
        <header className="min-h-[44px] h-14 sm:h-16 bg-gray-100 border-b border-gray-200 flex items-center px-2 sm:px-6 flex-shrink-0 gap-1 sm:gap-2 flex-wrap sm:flex-nowrap">
          <button
            type="button"
            onClick={() => setSidebarOpen((o) => !o)}
            className="md:hidden p-2.5 -ml-0.5 rounded-lg text-gray-600 hover:bg-gray-200 hover:text-gray-800 active:bg-gray-300 min-h-[44px] min-w-[44px] flex items-center justify-center flex-shrink-0"
            aria-label="Abrir menu"
            aria-expanded={sidebarOpen}
          >
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden>
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
            </svg>
          </button>
          <div className="flex items-center gap-2 sm:gap-3 min-w-0 flex-1 flex-shrink">
            <div className="w-8 h-8 rounded-full bg-white border border-gray-200 flex items-center justify-center flex-shrink-0 p-1">
              <img src="/livro-de-contato.svg" alt="" className="w-full h-full object-contain" aria-hidden />
            </div>
            <span className="text-primary font-semibold text-sm sm:text-lg truncate min-w-0">{APP_NAME}</span>
            {!user?.isAdmin && subscriptionStatus?.hasAccess && subscriptionStatus?.isTrial && subscriptionStatus?.daysRemaining != null && (
              <span className="text-xs sm:text-sm text-amber-700 bg-amber-50 px-2 py-1 rounded-lg font-medium whitespace-nowrap flex-shrink-0">
                Free {subscriptionStatus.daysRemaining}d
              </span>
            )}
            {!user?.isAdmin && subscriptionStatus?.hasAccess && !subscriptionStatus?.isTrial && (
              <span className="text-xs sm:text-sm text-primary bg-primary/10 px-2 py-1 rounded-lg font-medium whitespace-nowrap flex-shrink-0">
                Premium
              </span>
            )}
          </div>
          <div className="flex-1 flex justify-center min-w-0 hidden sm:block">
            {location.pathname === ROUTES.DASHBOARD ? (
              <h1 className="text-xl font-bold text-gray-900 truncate">
                Bem-vindo, {getFirstName(user?.name, FALLBACK_USER_NAME)}!
              </h1>
            ) : null}
          </div>
          <div className="flex items-center gap-1 sm:gap-4 flex-shrink-0 justify-end">
            <div className="relative">
              <button
                type="button"
                onClick={() => { warmupNotificationSound(); setNotificationsOpen((o) => !o); setUserMenuOpen(false); }}
                className="p-2.5 rounded-lg text-gray-500 hover:bg-gray-100 hover:text-gray-700 active:bg-gray-200 min-h-[44px] min-w-[44px] flex items-center justify-center"
                aria-label="Notificações"
                aria-expanded={notificationsOpen}
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" /></svg>
                {notifications.length > 0 && (
                  <span className="absolute top-1.5 right-1.5 flex h-2.5 w-2.5" aria-hidden>
                    <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-red-400 opacity-75" />
                    <span className="relative inline-flex h-2.5 w-2.5 rounded-full bg-red-500" />
                  </span>
                )}
              </button>
              {notificationsOpen && (
                <>
                  <div className="fixed inset-0 z-10" aria-hidden onClick={() => setNotificationsOpen(false)} />
                  <div className="absolute right-0 mt-1 w-72 max-h-[min(24rem,70vh)] flex flex-col bg-white rounded-lg shadow-lg border border-gray-200 z-20 overflow-hidden">
                    <p className="text-sm font-medium text-gray-700 py-3 px-4 border-b border-gray-100">Notificações</p>
                    <div className="overflow-y-auto py-2">
                      {notificationsLoading ? (
                        <p className="text-sm text-gray-500 px-4 py-3">Carregando...</p>
                      ) : notifications.length === 0 ? (
                        <p className="text-sm text-gray-500 px-4 py-3">Nenhuma notificação no momento.</p>
                      ) : (
                        <ul className="divide-y divide-gray-100">
                          {notifications.map((n) => (
                            <li key={n.id} className="px-4 py-2.5 hover:bg-gray-50 flex items-start gap-2 group">
                              <div className="flex-1 min-w-0">
                                <p className="text-sm font-medium text-gray-800">
                                  {n.type === 'AppointmentCancelledByClient'
                                    ? 'Cliente cancelou'
                                    : n.type === 'NewUserRegistered'
                                      ? 'Novo cadastro'
                                      : n.clientName}
                                </p>
                                <p className="text-xs text-gray-500 mt-0.5">
                                  {n.type === 'AppointmentCancelledByClient'
                                    ? `${n.clientName} — ${formatDateAndTime(n.scheduledAt)}`
                                    : n.type === 'NewUserRegistered'
                                      ? n.clientName
                                      : formatDateAndTime(n.scheduledAt)}
                                </p>
                              </div>
                              <button
                                type="button"
                                onClick={() => handleCloseNotification(n.id)}
                                className="flex-shrink-0 p-1 rounded text-gray-400 hover:text-gray-600 hover:bg-gray-200"
                                aria-label="Fechar notificação"
                                title="Fechar"
                              >
                                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                                </svg>
                              </button>
                            </li>
                          ))}
                        </ul>
                      )}
                    </div>
                  </div>
                </>
              )}
            </div>
            <Link to={ROUTES.CONFIGURACOES} className="hidden sm:flex p-2.5 rounded-lg text-gray-500 hover:bg-gray-100 hover:text-gray-700 active:bg-gray-200 min-h-[44px] min-w-[44px] items-center justify-center" aria-label="Configurações" title="Configurações">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z M15 12a3 3 0 11-6 0 3 3 0 016 0z" /></svg>
            </Link>
            <div className="relative">
              <button
                type="button"
                onClick={() => setUserMenuOpen((o) => !o)}
                className="flex items-center gap-2 p-2 rounded-lg hover:bg-gray-100 focus:outline-none min-h-[44px] active:bg-gray-200"
                aria-expanded={userMenuOpen}
                aria-haspopup="true"
              >
                {showPhoto ? (
                  <img
                    key={`photo-${user.id}-${user.profilePhotoUrl ?? ''}`}
                    src={getProfilePhotoUrl(user.profilePhotoUrl, `${user.id}-${user.profilePhotoUrl ?? ''}`) ?? user.profilePhotoUrl ?? ''}
                    alt=""
                    className="w-9 h-9 rounded-full object-cover border-2 border-gray-200"
                    onError={() => setPhotoError(true)}
                  />
                ) : (
                  <div className="w-9 h-9 rounded-full bg-primary flex items-center justify-center text-white font-semibold text-sm">
                    {user?.name?.charAt(0)?.toUpperCase() ?? '?'}
                  </div>
                )}
                <span className="text-gray-700 font-medium hidden sm:inline max-w-[120px] truncate">{user?.name ?? FALLBACK_USER_NAME}</span>
                <svg className={`w-4 h-4 text-gray-500 transition ${userMenuOpen ? 'rotate-180' : ''}`} fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" /></svg>
              </button>
              {userMenuOpen && (
                <>
                  <div className="fixed inset-0 z-10" aria-hidden onClick={() => setUserMenuOpen(false)} />
                  <div className="absolute right-0 mt-1 w-48 py-1 bg-white rounded-lg shadow-lg border border-gray-200 z-20">
                    <Link to="/configuracoes" className="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100" onClick={() => setUserMenuOpen(false)}>Meu perfil</Link>
                    <button type="button" onClick={() => { setUserMenuOpen(false); logout(); }} className="block w-full text-left px-4 py-2 text-sm text-red-600 hover:bg-gray-100">Sair</button>
                  </div>
                </>
              )}
            </div>
          </div>
        </header>

        {/* Content */}
        <main className="flex-1 overflow-auto overflow-x-hidden p-4 sm:p-6 overscroll-behavior-contain bg-white">
          <Outlet />
        </main>
      </div>

      {/* Modal Fale conosco */}
      {supportModalOpen && (
        <>
          <div className="fixed inset-0 bg-black/50 z-40" aria-hidden onClick={() => !supportSubmitting && setSupportModalOpen(false)} />
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4 pointer-events-none">
            <div className="bg-white rounded-xl shadow-xl max-w-md w-full p-6 pointer-events-auto" role="dialog" aria-labelledby="support-modal-title" aria-modal="true">
              <h2 id="support-modal-title" className="text-lg font-semibold text-gray-900 mb-4">Fale conosco</h2>
              {supportSuccess ? (
                <p className="text-gray-600">Mensagem enviada. Entraremos em contato em breve.</p>
              ) : (
                <form onSubmit={handleSupportSubmit}>
                  <label htmlFor="support-message" className="block text-sm font-medium text-gray-700 mb-1">Mensagem</label>
                  <textarea
                    id="support-message"
                    value={supportMessage}
                    onChange={(e) => setSupportMessage(e.target.value)}
                    placeholder="Descreva o problema ou dúvida..."
                    required
                    rows={4}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary focus:border-primary text-gray-900 placeholder-gray-500"
                    disabled={supportSubmitting}
                  />
                  {supportError && (
                    <p className="mt-2 text-sm text-red-600" role="alert">{supportError}</p>
                  )}
                  <div className="mt-4 flex gap-2 justify-end">
                    <button
                      type="button"
                      onClick={() => setSupportModalOpen(false)}
                      className="px-4 py-2 text-sm font-medium text-gray-700 bg-gray-100 hover:bg-gray-200 rounded-lg"
                      disabled={supportSubmitting}
                    >
                      Cancelar
                    </button>
                    <button
                      type="submit"
                      disabled={supportSubmitting || !supportMessage.trim()}
                      className="px-4 py-2 text-sm font-medium text-white bg-primary hover:bg-primary/90 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      {supportSubmitting ? 'Enviando...' : 'Enviar'}
                    </button>
                  </div>
                </form>
              )}
            </div>
          </div>
        </>
      )}
    </div>
    </ValuesVisibilityProvider>
  )
}
