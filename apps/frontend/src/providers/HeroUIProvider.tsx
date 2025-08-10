import { HeroUIProvider } from "@heroui/react";

export function HeroUIProviders({ children }: { children: React.ReactNode }) {
  return <HeroUIProvider>{children}</HeroUIProvider>;
}
