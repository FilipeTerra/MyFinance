import './InsightCard.css';

interface LifestyleInsightCardProps {
    curiosity: string;
    information: string;
    suggestion: string;
    onDismiss: () => void;
}

export function LifestyleInsightCard({
    curiosity,
    information,
    suggestion,
    onDismiss,
}: LifestyleInsightCardProps) {
    return (
        <div className="insight-card" role="status">
            <span className="insight-card-icon" aria-hidden="true">📈</span>

            <div className="insight-card-body">
                <span className="insight-card-title">Inflação de estilo de vida</span>
                <p className="insight-card-curiosity">💡 {curiosity}</p>
                <p className="insight-card-text">{information}</p>
                <p className="insight-card-suggestion">{suggestion}</p>
            </div>

            <button className="insight-card-close" onClick={onDismiss} aria-label="Dispensar insight">
                ×
            </button>
        </div>
    );
}
