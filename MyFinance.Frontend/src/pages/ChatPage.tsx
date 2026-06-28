import React, { useState, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import ReactMarkdown from 'react-markdown';
import './ChatPage.css';
import { sendMessage, SessionExpiredError } from '../services/AiApi';
import { Header } from '../components/Layout/Header';

interface Message {
    id: string;
    sender: 'user' | 'agent';
    text: string;
    isTyping?: boolean;
    dataPayload?: unknown;
}

const TYPING_ID = 'typing-indicator';

const QUICK_PROMPTS = [
    { emoji: '📊', label: 'Resumo financeiro dos últimos 30 dias' },
    { emoji: '💸', label: 'Onde eu gastei mais este mês?' },
    { emoji: '🎯', label: 'Gostaria de criar uma nova meta' },
    { emoji: '📉', label: 'Vale a pena financiar um carro?' },
];

export function ChatPage() {
    const navigate = useNavigate();
    const [messages, setMessages] = useState<Message[]>([
        {
            id: '1',
            sender: 'agent',
            text: 'Olá! Sou FinAl, seu mentor financeiro pessoal. Como posso ajudar você hoje?',
        },
    ]);
    const [inputText, setInputText] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const messagesEndRef = useRef<HTMLDivElement>(null);

    const scrollToBottom = () =>
        messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });

    const sendText = async (text: string) => {
        if (!text.trim() || isLoading) return;

        const messageText = text.trim();

        setMessages(prev => [
            ...prev,
            { id: Date.now().toString(), sender: 'user', text: messageText },
            { id: TYPING_ID, sender: 'agent', text: '', isTyping: true },
        ]);
        setIsLoading(true);

        // defer scroll so the new nodes are already in the DOM
        setTimeout(scrollToBottom, 50);

        try {
            const response = await sendMessage(messageText);
            setMessages(prev => [
                ...prev.filter(m => m.id !== TYPING_ID),
                {
                    id: (Date.now() + 1).toString(),
                    sender: 'agent',
                    text: response.message || 'Resposta recebida.',
                    dataPayload: response.data,
                },
            ]);
        } catch (error) {
            if (error instanceof SessionExpiredError) {
                localStorage.removeItem('authToken');
                navigate('/login', { replace: true });
                return;
            }
            setMessages(prev => [
                ...prev.filter(m => m.id !== TYPING_ID),
                {
                    id: (Date.now() + 1).toString(),
                    sender: 'agent',
                    text: error instanceof Error
                        ? error.message
                        : 'Desculpe, tive um problema ao processar sua solicitação.',
                },
            ]);
        } finally {
            setIsLoading(false);
            setTimeout(scrollToBottom, 50);
        }
    };

    const handleSend = () => {
        if (!inputText.trim() || isLoading) return;
        sendText(inputText);
        setInputText('');
    };

    const handleQuickPrompt = (text: string) => {
        sendText(text);
    };

    const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            handleSend();
        }
    };

    return (
        <div className="chat-page">
            <Header />

            <div className="chat-content">

                {/* ── 70 % — Área do Chat ─────────────────────────── */}
                <div className="chat-area">
                    <div className="chat-panel">

                        <div className="chat-header">
                            <div className="agent-avatar">F</div>
                            <div className="agent-info">
                                <span className="agent-name">FinAl</span>
                            </div>
                        </div>

                        <div className="messages-container">
                            {messages.map(msg => (
                                <div key={msg.id} className={`message-row ${msg.sender}`}>
                                    {msg.sender === 'agent' && (
                                        <div className="bubble-avatar">F</div>
                                    )}
                                    <div className={`bubble ${msg.sender === 'user' ? 'bubble-user' : 'bubble-agent'}`}>
                                        {msg.isTyping ? (
                                            <div className="typing-indicator">
                                                <span /><span /><span />
                                            </div>
                                        ) : msg.sender === 'agent' ? (
                                            <ReactMarkdown>{msg.text}</ReactMarkdown>
                                        ) : (
                                            msg.text
                                        )}
                                    </div>
                                </div>
                            ))}
                            <div ref={messagesEndRef} />
                        </div>

                        <div className="prompt-bar">
                            <input
                                type="text"
                                className="prompt-input"
                                value={inputText}
                                onChange={e => setInputText(e.target.value)}
                                onKeyDown={handleKeyDown}
                                placeholder="Pergunte algo sobre suas finanças…"
                                disabled={isLoading}
                            />
                            <button
                                className="send-button"
                                onClick={handleSend}
                                disabled={isLoading || !inputText.trim()}
                                aria-label="Enviar mensagem"
                            >
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                                    <line x1="22" y1="2" x2="11" y2="13" />
                                    <polygon points="22 2 15 22 11 13 2 9 22 2" />
                                </svg>
                            </button>
                        </div>

                    </div>
                </div>

                {/* ── 30 % — Barra Lateral de Ações Rápidas ──────── */}
                <aside className="quick-prompts-sidebar">
                    <div className="sidebar-header">
                        <span className="sidebar-sparkle">✦</span>
                        <div>
                            <p className="sidebar-title">Sugestões para o FinAl</p>
                            <p className="sidebar-subtitle">Clique para enviar ao chat</p>
                        </div>
                    </div>

                    <div className="quick-prompts-list">
                        {QUICK_PROMPTS.map(prompt => (
                            <button
                                key={prompt.label}
                                className="quick-prompt-btn"
                                onClick={() => handleQuickPrompt(prompt.label)}
                                disabled={isLoading}
                            >
                                <span className="prompt-emoji">{prompt.emoji}</span>
                                <span className="prompt-text">{prompt.label}</span>
                            </button>
                        ))}
                    </div>

                    <div className="sidebar-footer">
                        <span>Desenvolvido com</span>
                        <span className="footer-brand">FinAl IA</span>
                    </div>
                </aside>

            </div>
        </div>
    );
}
