import { Header } from '../components/Layout/Header';

// ... (código do componente HomePage)

export function HomePage() {
    // ... (seus hooks useState, useEffect)

    return (
        <div className="homepage-container">
            <Header /> {/* <--- ADICIONAR O HEADER AQUI */}

            <main className="homepage-content">
                {/* ... (resto do conteúdo da HomePage: Seção de Contas, Seção de Transações) */}
            </main>
        </div>
    );
}