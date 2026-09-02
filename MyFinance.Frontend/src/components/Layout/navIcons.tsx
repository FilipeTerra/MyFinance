/**
 * Ícones da navegação mobile.
 *
 * SVG inline em vez de emoji (que é o que as abas do Dashboard usam): emoji não
 * aceita a cor do estado ativo e renderiza em tamanhos bem diferentes entre
 * Android e iOS. Todos herdam `currentColor`, então o estado ativo é só uma
 * troca de `color` no CSS.
 */

const base = {
    width: 24,
    height: 24,
    viewBox: '0 0 24 24',
    fill: 'none',
    stroke: 'currentColor',
    strokeWidth: 1.75,
    strokeLinecap: 'round' as const,
    strokeLinejoin: 'round' as const,
};

export function IconeHome() {
    return (
        <svg {...base}>
            <path d="M3.5 10.7 12 3.8l8.5 6.9" />
            <path d="M5.8 9.4V19a1.2 1.2 0 0 0 1.2 1.2h3.2v-5.1h3.6v5.1h3.2a1.2 1.2 0 0 0 1.2-1.2V9.4" />
        </svg>
    );
}

export function IconeConsultor() {
    return (
        <svg {...base}>
            <path d="M20.2 12.1c0 3.9-3.7 7.1-8.2 7.1-1 0-2-.16-2.9-.45L4.3 20.2l1.4-3.6a6.8 6.8 0 0 1-1.9-4.5c0-3.9 3.7-7.1 8.2-7.1s8.2 3.2 8.2 7.1Z" />
        </svg>
    );
}

export function IconeDashboard() {
    return (
        <svg {...base}>
            <path d="M4 20h16" />
            <path d="M7.5 20v-6.4" />
            <path d="M12 20V5.8" />
            <path d="M16.5 20v-9.6" />
        </svg>
    );
}

export function IconeMais() {
    return (
        <svg {...base} strokeWidth={0} fill="currentColor">
            <circle cx="5.6" cy="12" r="1.65" />
            <circle cx="12" cy="12" r="1.65" />
            <circle cx="18.4" cy="12" r="1.65" />
        </svg>
    );
}

export function IconePerfil() {
    return (
        <svg {...base}>
            <circle cx="12" cy="8.2" r="3.6" />
            <path d="M4.9 20c.7-3.5 3.5-5.9 7.1-5.9s6.4 2.4 7.1 5.9" />
        </svg>
    );
}

export function IconeSair() {
    return (
        <svg {...base}>
            <path d="M14.4 16.6V19a1.5 1.5 0 0 1-1.5 1.5H6A1.5 1.5 0 0 1 4.5 19V5A1.5 1.5 0 0 1 6 3.5h6.9a1.5 1.5 0 0 1 1.5 1.5v2.4" />
            <path d="M10.6 12h8.9" />
            <path d="m16.6 9 3 3-3 3" />
        </svg>
    );
}
