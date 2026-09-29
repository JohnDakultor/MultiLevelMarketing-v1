import { AlertTriangle, ArrowDownRight, ArrowUpRight } from "lucide-react";
import type { ReactNode } from "react";
import { Badge, type FeedbackTone } from "./Feedback";

export function StatusBadge({
  label,
  tone = "neutral",
}: {
  label: string;
  tone?: FeedbackTone;
}) {
  return <Badge tone={tone}>{label}</Badge>;
}

export function StatsCard({
  label,
  value,
  description,
  trend,
  tone = "neutral",
}: {
  label: string;
  value: ReactNode;
  description?: string;
  trend?: { direction: "up" | "down"; label: string };
  tone?: "neutral" | "success" | "warning" | "danger";
}) {
  const TrendIcon = trend?.direction === "down" ? ArrowDownRight : ArrowUpRight;
  return (
    <article className={`ds-stat-card ds-stat-card--${tone}`}>
      <div className="ds-stat-card__header">
        <span>{label}</span>
        {tone === "warning" || tone === "danger" ? (
          <AlertTriangle aria-hidden />
        ) : null}
      </div>
      <strong className="ds-stat-card__value">{value}</strong>
      {(description || trend) && (
        <div className="ds-stat-card__footer">
          {trend && (
            <span className={`ds-trend ds-trend--${trend.direction}`}>
              <TrendIcon aria-hidden /> {trend.label}
            </span>
          )}
          {description && <span>{description}</span>}
        </div>
      )}
    </article>
  );
}

export function SectionHeader({
  title,
  description,
  action,
}: {
  title: string;
  description?: string;
  action?: ReactNode;
}) {
  return (
    <header className="ds-section-header">
      <div>
        <h2>{title}</h2>
        {description && <p>{description}</p>}
      </div>
      {action}
    </header>
  );
}
