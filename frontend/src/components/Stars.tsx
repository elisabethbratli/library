export function Stars({ value }: { value: number | null }) {
  if (value == null) return <span className="muted">No reviews</span>;
  const full = Math.round(value);
  return (
    <span title={value.toFixed(1)} aria-label={`${value.toFixed(1)} of 5`}>
      {"★".repeat(full)}{"☆".repeat(5 - full)}
    </span>
  );
}
