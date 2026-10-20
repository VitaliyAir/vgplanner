import { createContext, useContext } from 'react'

export const ShowDoneContext = createContext<boolean>(true)

export function useShowDone(): boolean {
  return useContext(ShowDoneContext)
}
