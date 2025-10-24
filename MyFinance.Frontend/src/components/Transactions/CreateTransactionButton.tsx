// src/components/Transactions/CreateTransactionButton.tsx
import React from 'react';
import './CreateTransactionButton.css';

interface CreateTransactionButtonProps {
    onClick: () => void;
}

export function CreateTransactionButton({ onClick }: CreateTransactionButtonProps) {
    return (
        <button className="create-transaction-button" onClick={onClick}>
            + Nova Transação
        </button>
    );
}   