import { Routes, Route, Navigate } from 'react-router-dom'
import { AuthProvider, useAuth } from './contexts/AuthContext'
import { ROUTES } from './constants'
import ProtectedRoute from './components/ProtectedRoute'
import AdminRoute from './components/AdminRoute'
import Layout from './components/layout/Layout'
import Login from './pages/Login'
import AdminDashboard from './pages/AdminDashboard'
import AdminUsers from './pages/AdminUsers'
import EsqueciSenha from './pages/EsqueciSenha'
import RedefinirSenha from './pages/RedefinirSenha'
import CriarConta from './pages/CriarConta'
import Dashboard from './pages/Dashboard'
import Configuracoes from './pages/Configuracoes'
import Planos from './pages/Planos'
import Servicos from './pages/Servicos'
import Clientes from './pages/Clientes'
import Ganhos from './pages/Ganhos'
import AgendarPublico from './pages/AgendarPublico'
import CancelarAgendamento from './pages/CancelarAgendamento'
import Agendamentos from './pages/Agendamentos'

/** Redireciona admin para /admin; usuário comum renderiza o filho normalmente. */
function NonAdminRoute({ children }: { children: React.ReactNode }) {
  const { user } = useAuth()
  if (user?.isAdmin) return <Navigate to={ROUTES.ADMIN} replace />
  return <>{children}</>
}

/** Dashboard redireciona admin direto para o painel admin. */
function DashboardOrAdmin() {
  const { user } = useAuth()
  if (user?.isAdmin) return <Navigate to={ROUTES.ADMIN} replace />
  return <Dashboard />
}

/** /clientes: admin vê lista de usuários do sistema; profissional vê clientes dos negócios. */
function ClientesOrAdminUsers() {
  const { user } = useAuth()
  if (user?.isAdmin) return <AdminUsers />
  return <Clientes />
}

export default function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="/" element={<Navigate to={ROUTES.DASHBOARD} replace />} />
        <Route path="/esqueci-senha" element={<EsqueciSenha />} />
        <Route path="/redefinir-senha" element={<RedefinirSenha />} />
        <Route path="/criar-conta" element={<CriarConta />} />
        <Route path="/agendar/:slug" element={<AgendarPublico />} />
        <Route path="/agendar/cancelar" element={<CancelarAgendamento />} />
        <Route
          path="/"
          element={
            <ProtectedRoute>
              <Layout />
            </ProtectedRoute>
          }
        >
          <Route path="dashboard" element={<DashboardOrAdmin />} />
          <Route path="agendamentos" element={<NonAdminRoute><Agendamentos /></NonAdminRoute>} />
          <Route path="servicos" element={<NonAdminRoute><Servicos /></NonAdminRoute>} />
          <Route path="clientes" element={<ClientesOrAdminUsers />} />
          <Route path="ganhos" element={<Ganhos />} />
          <Route path="configuracoes" element={<NonAdminRoute><Configuracoes /></NonAdminRoute>} />
          <Route path="planos" element={<NonAdminRoute><Planos /></NonAdminRoute>} />
          <Route path="premium" element={<NonAdminRoute><Planos /></NonAdminRoute>} />
          <Route path="admin" element={<AdminRoute><AdminDashboard /></AdminRoute>} />
        </Route>
        <Route path="*" element={<Navigate to={ROUTES.DASHBOARD} replace />} />
      </Routes>
    </AuthProvider>
  )
}
