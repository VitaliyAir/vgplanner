interface InfoIconProps {
  size?: number
}

export function InfoIcon({ size = 14 }: InfoIconProps) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={3}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <line x1="12" y1="10" x2="12" y2="18" />
      <circle cx="12" cy="6" r="1.4" fill="currentColor" stroke="none" />
    </svg>
  )
}
