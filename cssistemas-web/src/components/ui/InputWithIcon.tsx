import { type InputHTMLAttributes, type ReactNode } from 'react'

type InputWithIconProps = InputHTMLAttributes<HTMLInputElement> & {
  icon: ReactNode
}

/** Campo de input com ícone à esquerda — DRY, reutilizável em login, cadastro, etc. */
export default function InputWithIcon({ icon, className = '', ...props }: InputWithIconProps) {
  return (
    <div className="relative">
      <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" aria-hidden>
        {icon}
      </span>
      <input
        className={`w-full pl-10 pr-4 py-3 min-h-[48px] text-base border border-gray-300 rounded-xl focus:ring-2 focus:ring-primary focus:border-primary outline-none transition touch-manipulation ${className}`}
        {...props}
      />
    </div>
  )
}
