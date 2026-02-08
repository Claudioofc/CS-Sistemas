/** Nome da aplicação (marca). */
export const APP_NAME = 'CS Sistemas'

/** Nome exibido quando o usuário não tem nome. */
export const FALLBACK_USER_NAME = 'Usuário'

/** Nicho do produto: clínica odontológica. Usado em textos e labels (DRY). */
export const NICHO_NAME = 'Clínica Odontológica'
/** Label singular do negócio no nicho (ex.: "clínica"). */
export const NEGOCIO_SINGULAR = 'clínica'
/** Label plural (ex.: "clínicas"). */
export const NEGOCIO_PLURAL = 'clínicas'

/** Chave localStorage para preferência de ocultar valores (estilo banco). */
export const STORAGE_VALUES_VISIBLE_KEY = 'cssistemas_valuesVisible'

/** Rotas da aplicação (evita strings repetidas). */
export const ROUTES = {
  DASHBOARD: '/dashboard',
  ADMIN: '/admin',
  LOGIN: '/login',
  CRIAR_CONTA: '/criar-conta',
  ESQUECI_SENHA: '/esqueci-senha',
  REDEFINIR_SENHA: '/redefinir-senha',
  CONFIGURACOES: '/configuracoes',
  PLANOS: '/planos',
  AGENDAMENTOS: '/agendamentos',
  SERVICOS: '/servicos',
  CLIENTES: '/clientes',
  GANHOS: '/ganhos',
  PREMIUM: '/premium',
  /** Agendamento público (sem login). Ex.: /agendar/minha-clinica */
  AGENDAR: '/agendar',
} as const
