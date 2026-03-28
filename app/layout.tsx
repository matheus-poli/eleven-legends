import type { Metadata } from "next";
import "./globals.css";
import "flag-icons/css/flag-icons.min.css";

export const metadata: Metadata = {
  title: "Eleven Legends",
  description: "Football Manager",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" data-theme="eleven">
      <body className="min-h-screen bg-base-200">{children}</body>
    </html>
  );
}
