import { Routes, Route, Navigate } from 'react-router-dom'
import { AuthProvider } from './contexts/AuthContext'
import { ROUTES } from './constants'
import ProtectedRoute from './components/ProtectedRoute'
import AdminRoute from './components/AdminRoute'
import Layout from './components/layout/Layout'
import Login from './pages/Login'
import AdminDashboard from './pages/AdminDashboard'
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

/** Dashboard com cards, agenda e ganhos — igual para cliente e admin. Controle de clientes fica só em Admin. */
function DashboardOrAdmin() {
  return <Dashboard />
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
          <Route path="agendamentos" element={<Agendamentos />} />
          <Route path="servicos" element={<Servicos />} />
          <Route path="clientes" element={<Clientes />} />
          <Route path="ganhos" element={<Ganhos />} />
          <Route path="configuracoes" element={<Configuracoes />} />
          <Route path="planos" element={<Planos />} />
          <Route path="premium" element={<Planos />} />
          <Route path="admin" element={<AdminRoute><AdminDashboard /></AdminRoute>} />
        </Route>
        <Route path="*" element={<Navigate to={ROUTES.DASHBOARD} replace />} />
      </Routes>
    </AuthProvider>
  )
}
