import React, { useState, useEffect, useRef } from 'react';
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

    useEffect(() => {
        messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages]);

    const handleSend = async () => {
        if (!inputText.trim() || isLoading) return;

        const messageText = inputText.trim();

        setMessages(prev => [
            ...prev,
            { id: Date.now().toString(), sender: 'user', text: messageText },
            { id: TYPING_ID, sender: 'agent', text: '', isTyping: true },
        ]);
        setInputText('');
        setIsLoading(true);

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
        }
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

                {/* ── Chat Panel ───────────────────────────────────── */}
                <div className="chat-panel">

                    <div className="chat-header">
                        <div className="agent-avatar">F</div>
                        <div className="agent-info">
                            <span className="agent-name">FinAl</span>
                            <span className="agent-status">
                                <span className="status-dot" />
                                Online
                            </span>
                        </div>
                    </div>

                    <div className="messages-container">
                        {messages.map(message => (
                            <div
                                key={message.id}
                                className={`message-row ${message.sender}`}
                            >
                                {message.sender === 'agent' && (
                                    <div className="bubble-avatar">F</div>
                                )}

                                <div className={`bubble ${message.sender === 'user' ? 'bubble-user' : 'bubble-agent'}`}>
                                    {message.isTyping ? (
                                        <div className="typing-indicator">
                                            <span /><span /><span />
                                        </div>
                                    ) : message.sender === 'agent' ? (
                                        <ReactMarkdown>{message.text}</ReactMarkdown>
                                    ) : (
                                        message.text
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

                {/* ── Stage Panel ──────────────────────────────────── */}
                <div className="stage-panel">
                    <div className="stage-content">
                        <div className="stage-placeholder">
                            <div className="stage-icon">📊</div>
                            <h3>Visualização de Dados</h3>
                            <p>Os gráficos e tabelas aparecerão aqui conforme nossa conversa.</p>
                        </div>
                    </div>
                </div>

            </div>
        </div>
    );
}
