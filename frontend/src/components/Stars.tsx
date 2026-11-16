export function Stars({ value }: { value: number | null }) {
  if (value == null) return <span className="muted">No reviews</span>;
  const full = Math.round(value);
  return (
    <span aria-label={`${value.toFixed(1)} out of 5 stars`}>
      {"★".repeat(full)}{"☆".repeat(5 - full)}
    </span>
  );
}
