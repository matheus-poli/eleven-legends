/** Country flag using flag-icons CSS library */

const countryToCode: Record<string, string> = {
  "Brasil": "br",
  "España": "es",
  "England": "gb-eng",
  "Italia": "it",
  "Africa": "za",   // South Africa as representative
  "Asia": "jp",     // Japan as representative
  "Americas": "ar", // Argentina as representative
};

interface FlagProps {
  country: string;
  size?: "sm" | "md" | "lg";
  className?: string;
}

export function Flag({ country, size = "md", className = "" }: FlagProps) {
  const code = countryToCode[country] ?? "un";
  const sizeClass = size === "sm" ? "text-sm" : size === "md" ? "text-lg" : "text-2xl";

  return (
    <span
      className={`fi fi-${code} ${sizeClass} rounded-sm ${className}`}
      title={country}
    />
  );
}
