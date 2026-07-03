import { useEffect, useState } from 'react';
import { profileService } from '../services/Api';
import { Header } from '../components/Layout/Header';

export function ProfilePage() {
    const [name, setName] = useState('');
    const [email, setEmail] = useState('');
    const [monthlyIncome, setMonthlyIncome] = useState<string>('');
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [message, setMessage] = useState<{ text: string; isError: boolean } | null>(null);

    useEffect(() => {
        profileService.getProfile()
            .then(profile => {
                setName(profile.name);
                setEmail(profile.email);
                setMonthlyIncome(profile.monthlyIncome !== null ? String(profile.monthlyIncome) : '');
            })
            .finally(() => setLoading(false));
    }, []);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setSaving(true);
        setMessage(null);
        try {
            const income = monthlyIncome === '' ? null : parseFloat(monthlyIncome);
            await profileService.updateProfile({ monthlyIncome: income });
            setMessage({ text: 'Perfil atualizado com sucesso!', isError: false });
        } catch {
            setMessage({ text: 'Erro ao atualizar o perfil. Tente novamente.', isError: true });
        } finally {
            setSaving(false);
        }
    };

    return (
        <>
            <Header />
            <main className="profile-page">
                <h1>Meu Perfil</h1>

                {loading ? (
                    <p>Carregando...</p>
                ) : (
                    <form onSubmit={handleSubmit} className="profile-form">
                        <div className="form-group">
                            <label htmlFor="name">Nome</label>
                            <input id="name" type="text" value={name} disabled />
                        </div>

                        <div className="form-group">
                            <label htmlFor="email">E-mail</label>
                            <input id="email" type="email" value={email} disabled />
                        </div>

                        <div className="form-group">
                            <label htmlFor="monthlyIncome">Salário Mensal (R$)</label>
                            <input
                                id="monthlyIncome"
                                type="number"
                                min="0"
                                step="0.01"
                                value={monthlyIncome}
                                onChange={e => setMonthlyIncome(e.target.value)}
                                placeholder="Ex: 5000.00"
                            />
                        </div>

                        {message && (
                            <p className={message.isError ? 'form-error' : 'form-success'}>
                                {message.text}
                            </p>
                        )}

                        <button type="submit" disabled={saving}>
                            {saving ? 'Salvando...' : 'Salvar'}
                        </button>
                    </form>
                )}
            </main>
        </>
    );
}
