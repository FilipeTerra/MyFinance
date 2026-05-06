import React, { useState } from 'react';
import './ChatPage.css';
import { sendMessage } from '../services/AiApi';
import { Header } from '../components/Layout/Header';

interface Message {
    id: string;
    sender: 'user' | 'agent';
    text: string;
    dataPayload?: any;
}

export function ChatPage() {
    const [messages, setMessages] = useState<Message[]>([
        {
            id: '1',
            sender: 'agent',
            text: 'Olá! Sou seu assistente financeiro. Como posso ajudar você hoje?'
        }
    ]);
    const [inputText, setInputText] = useState('');
    const [isLoading, setIsLoading] = useState(false);

    const handleSend = async () => {
        if (!inputText.trim()) return;

        const userMessage: Message = {
            id: Date.now().toString(),
            sender: 'user',
            text: inputText.trim()
        };

        setMessages(prev => [...prev, userMessage]);
        const messageText = inputText.trim();
        setInputText('');
        setIsLoading(true);

        try {
            const response = await sendMessage(messageText);
            const agentMessage: Message = {
                id: (Date.now() + 1).toString(),
                sender: 'agent',
                text: response.message || 'Resposta recebida',
                dataPayload: response.data
            };
            setMessages(prev => [...prev, agentMessage]);
        } catch (error) {
            const errorMessage: Message = {
                id: (Date.now() + 1).toString(),
                sender: 'agent',
                text: 'Desculpe, tive um problema ao processar sua solicitação.'
            };
            setMessages(prev => [...prev, errorMessage]);
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
                        <h2>Consultor Financeiro IA</h2>
                    </div>
                    <div className="messages-container">
                        {messages.map(message => (
                            <div
                                key={message.id}
                                className={`message ${message.sender === 'user' ? 'user-message' : 'agent-message'}`}
                            >
                                <div className="message-text">{message.text}</div>
                            </div>
                        ))}
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
                            {isLoading ? 'Enviando...' : 'Enviar'}
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