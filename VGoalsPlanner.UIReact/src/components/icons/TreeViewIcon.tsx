interface TreeViewIconProps {
  size?: number
}

export function TreeViewIcon({ size = 20 }: TreeViewIconProps) {
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
      <rect x="9" y="3" width="6" height="5" rx="1.2" />
      <path d="M12 8v4" />
      <path d="M6 12h12" />
      <path d="M6 12v4" />
      <path d="M18 12v4" />
      <rect x="3" y="16" width="6" height="5" rx="1.2" />
      <rect x="15" y="16" width="6" height="5" rx="1.2" />
    </svg>
  )
}
