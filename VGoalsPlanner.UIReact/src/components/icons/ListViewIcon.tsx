interface ListViewIconProps {
  size?: number
}

export function ListViewIcon({ size = 20 }: ListViewIconProps) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={2}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <path d="M3 6l2 2 4-4.5" />
      <path d="M3 12l2 2 4-4.5" />
      <rect x="3" y="15.2" width="6" height="5.6" rx="1.2" />
      <path d="M13 6h8" />
      <path d="M13 12h8" />
      <path d="M13 18h8" />
    </svg>
  )
}
