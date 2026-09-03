import { useEffect, useState } from 'react';
import { profileService } from '../services/Api';
import './ProfilePage.css';

/** Máscara monetária: converte dígitos digitados em "1.234,56" (mesmo padrão usado em toda a aplicação). */
function maskCurrency(raw: string): string {
    let digits = raw.replace(/\D/g, '');
    if (digits === '') return '';
    if (digits.length > 1) digits = digits.replace(/^0+/, '');
    while (digits.length < 3) digits = '0' + digits;
    const decimalIndex = digits.length - 2;
    const integerPart = digits.slice(0, decimalIndex);
    const decimalPart = digits.slice(decimalIndex);
    const formattedInteger = integerPart.replace(/\B(?=(\d{3})+(?!\d))/g, '.');
    return formattedInteger + ',' + decimalPart;
}

const parseCurrency = (masked: string): number =>
    parseFloat(masked.replace(/\./g, '').replace(',', '.'));

/** Formata um número já existente (vindo da API) para o mesmo padrão de máscara. */
const formatExistingValue = (value: number): string =>
    new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value);

const getInitials = (name: string) =>
    name.trim().split(/\s+/).slice(0, 2).map(p => p[0]?.toUpperCase() ?? '').join('') || '?';

export function ProfilePage() {
    const [name, setName] = useState('');
    const [email, setEmail] = useState('');
    const [monthlyIncome, setMonthlyIncome] = useState('');
    const [initialIncome, setInitialIncome] = useState('');
    const [isLoading, setIsLoading] = useState(true);
    const [isSaving, setIsSaving] = useState(false);
    const [feedback, setFeedback] = useState<{ text: string; isError: boolean } | null>(null);

    useEffect(() => {
        profileService.getProfile()
            .then(profile => {
                setName(profile.name);
                setEmail(profile.email);
                const masked = profile.monthlyIncome !== null ? formatExistingValue(profile.monthlyIncome) : '';
                setMonthlyIncome(masked);
                setInitialIncome(masked);
            })
            .catch(() => setFeedback({ text: 'Não foi possível carregar seu perfil. Tente novamente.', isError: true }))
            .finally(() => setIsLoading(false));
    }, []);

    const isDirty = monthlyIncome !== initialIncome;

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!isDirty) return;

        setIsSaving(true);
        setFeedback(null);
        try {
            const income = monthlyIncome === '' ? null : parseCurrency(monthlyIncome);
            const updated = await profileService.updateProfile({ monthlyIncome: income });
            const masked = updated.monthlyIncome !== null ? formatExistingValue(updated.monthlyIncome) : '';
            setMonthlyIncome(masked);
            setInitialIncome(masked);
            setFeedback({ text: 'Perfil atualizado com sucesso!', isError: false });
        } catch {
            setFeedback({ text: 'Erro ao atualizar o perfil. Tente novamente.', isError: true });
        } finally {
            setIsSaving(false);
        }
    };

    return (
        <div className="profile-container">

            <main className="profile-content" id="conteudo-principal">
                <div className="profile-page-header">
                    <h2 className="profile-title">Meu Perfil</h2>
                    <p className="profile-subtitle">Gerencie suas informações pessoais e financeiras</p>
                </div>

                {isLoading ? (
                    <div className="profile-skeleton-stack">
                        <div className="profile-skeleton" style={{ height: '108px' }} />
                        <div className="profile-skeleton" style={{ height: '180px' }} />
                    </div>
                ) : (
                    <>
                        {/* ── Identidade — somente leitura ────────────────────────── */}
                        <section className="profile-card">
                            <div className="profile-identity">
                                <div className="profile-avatar">{getInitials(name)}</div>
                                <div className="profile-identity-info">
                                    <span className="profile-name">{name}</span>
                                    <span className="profile-email">{email}</span>
                                </div>
                            </div>
                        </section>

                        {/* ── Renda mensal — editável ──────────────────────────────── */}
                        <form onSubmit={handleSubmit} className="profile-card">
                            <div className="profile-card-header">
                                <span className="profile-card-icon" aria-hidden="true">💰</span>
                                <div>
                                    <h3 className="profile-card-title">Renda mensal</h3>
                                    <p className="profile-card-desc">
                                        Usada pelo assistente Claudio para calcular seu orçamento
                                        ideal e sugerir metas personalizadas.
                                    </p>
                                </div>
                            </div>

                            <div className="profile-form-group">
                                <label htmlFor="monthlyIncome">Salário mensal (R$)</label>
                                <div className="profile-input-wrapper">
                                    <span className="profile-input-prefix">R$</span>
                                    <input
                                        id="monthlyIncome"
                                        type="text"
                                        inputMode="numeric"
                                        placeholder="0,00"
                                        value={monthlyIncome}
                                        onChange={e => { setMonthlyIncome(maskCurrency(e.target.value)); setFeedback(null); }}
                                        disabled={isSaving}
                                    />
                                    {monthlyIncome !== '' && (
                                        <button
                                            type="button"
                                            className="profile-input-clear"
                                            onClick={() => { setMonthlyIncome(''); setFeedback(null); }}
                                            disabled={isSaving}
                                            title="Limpar valor"
                                            aria-label="Limpar valor"
                                        >
                                            ×
                                        </button>
                                    )}
                                </div>
                            </div>

                            {feedback && (
                                <p className={feedback.isError ? 'profile-message profile-message--error' : 'profile-message profile-message--success'}>
                                    {feedback.text}
                                </p>
                            )}

                            <div className="profile-actions">
                                <button type="submit" className="profile-save-btn" disabled={isSaving || !isDirty}>
                                    {isSaving ? 'Salvando...' : 'Salvar alterações'}
                                </button>
                            </div>
                        </form>
                    </>
                )}
            </main>
        </div>
    );
}
