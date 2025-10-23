import { Header } from '../components/Layout/Header';

// ... (c�digo do componente HomePage)

export function HomePage() {
    // ... (seus hooks useState, useEffect)

    return (
        <div className="homepage-container">
            <Header /> {/* <--- ADICIONAR O HEADER AQUI */}

            <main className="homepage-content">
                {/* ... (resto do conte�do da HomePage: Se��o de Contas, Se��o de Transa��es) */}
            </main>
        </div>
    );
}