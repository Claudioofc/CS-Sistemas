import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'
import { ROUTES } from '../constants'

/** Redireciona para o dashboard se o usuário não for admin. */
export default function AdminRoute({ children }: { children: React.ReactNode }) {
  const { user, isLoading } = useAuth()
  const location = useLocation()

  if (isLoading) {
    return (
      <div className="min-h-screen min-h-[100dvh] flex items-center justify-center bg-gray-50">
        <p className="text-gray-500">Carregando...</p>
      </div>
    )
  }
  if (!user?.isAdmin) {
    return <Navigate to={ROUTES.DASHBOARD} replace state={{ from: location }} />
  }
  return <>{children}</>
}
