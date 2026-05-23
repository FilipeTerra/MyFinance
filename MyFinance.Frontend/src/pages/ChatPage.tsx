import React, { useState, useEffect, useRef } from 'react';
import './ChatPage.css';
import { sendMessage } from '../services/AiApi';
import { Header } from '../components/Layout/Header';

interface Message {
    id: string;
    sender: 'user' | 'agent';
    text: string;
    isTyping?: boolean;
    dataPayload?: any;
}

const TYPING_ID = 'typing-indicator';

export function ChatPage() {
    const [messages, setMessages] = useState<Message[]>([
        {
            id: '1',
            sender: 'agent',
            text: 'Olá! Sou FinAl, seu mentor financeiro pessoal. Como posso ajudar você hoje?'
        }
    ]);
    const [inputText, setInputText] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const messagesEndRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages]);

    const handleSend = async () => {
        if (!inputText.trim() || isLoading) return;

        const userMessage: Message = {
            id: Date.now().toString(),
            sender: 'user',
            text: inputText.trim(),
        };

        const messageText = inputText.trim();
        setMessages(prev => [...prev, userMessage]);
        setInputText('');
        setIsLoading(true);

        setMessages(prev => [
            ...prev,
            { id: TYPING_ID, sender: 'agent', text: '', isTyping: true },
        ]);

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

    const handleKeyPress = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            handleSend();
        }
    };

    return (
        <div className="chat-page">
            <Header />
            <div className="chat-content">
                <div className="chat-panel">
                    <div className="chat-header">
                        <h2>FinAl — Mentor Financeiro</h2>
                    </div>
                    <div className="messages-container">
                        {messages.map(message => (
                            <div
                                key={message.id}
                                className={`message ${message.sender === 'user' ? 'user-message' : 'agent-message'}`}
                            >
                                {message.isTyping ? (
                                    <div className="typing-indicator">
                                        <span></span><span></span><span></span>
                                    </div>
                                ) : (
                                    <div className="message-text">{message.text}</div>
                                )}
                            </div>
                        ))}
                        <div ref={messagesEndRef} />
                    </div>
                    <div className="input-container">
                        <input
                            type="text"
                            value={inputText}
                            onChange={(e) => setInputText(e.target.value)}
                            onKeyPress={handleKeyPress}
                            placeholder="Digite sua mensagem..."
                            disabled={isLoading}
                        />
                        <button
                            onClick={handleSend}
                            disabled={isLoading || !inputText.trim()}
                        >
                            {isLoading ? 'Aguardando FinAl...' : 'Enviar'}
                        </button>
                    </div>
                </div>
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
