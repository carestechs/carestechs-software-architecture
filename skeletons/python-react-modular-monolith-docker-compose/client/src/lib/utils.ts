import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

// Conditional class merging goes through cn() — never string concatenation
// (adrs/react/tailwind-shadcn.md)
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}
