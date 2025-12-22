import React, { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import './AccountModal.css';
import { AccountType } from '../../types/AccountType';
import type { AccountResponseDto } from '../../types/AccountResponseDto';
import { accountService, AxiosError, type ApiErrorResponse } from '../../services/Api';
import type { UpdateAccountRequestDto } from '../../types/UpdateAccountRequestDto';

interface AccountModalProps {
    isOpen: boolean;
    onClose: () => void;
    onSuccess: (account: AccountResponseDto, isEdit: boolean) => void;
    accountToEdit?: AccountResponseDto | null;
}

// Schema de validação
const accountSchema = z.object({
    name: z.string().min(3, 'O nome deve ter pelo menos 3 caracteres.'),
    // Preprocess converte a string do value="1" para number 1 antes de validar o Enum
    type: z.preprocess((val) => Number(val), z.nativeEnum(AccountType)),
    // Saldo inicial é string para facilitar máscara, validamos apenas se for criação
    initialBalance: z.string().optional(), 
});

// Inferimos o tipo a partir do Schema
type AccountFormData = z.infer<typeof accountSchema>;

export function AccountModal({ isOpen, onClose, onSuccess, accountToEdit }: AccountModalProps) {
    const [isLoading, setIsLoading] = React.useState(false);
    const [error, setError] = React.useState<string | null>(null);

    // --- CORREÇÃO: Removemos <AccountFormData> para evitar conflito de tipagem com o preprocess ---
    const {
        register,
        handleSubmit,
        reset,
        setValue,
        formState: { errors }
    } = useForm({
        resolver: zodResolver(accountSchema),
        defaultValues: {
            name: '',
            type: AccountType.ContaCorrente,
            initialBalance: '0,00'
        }
    });

    const isEditing = !!accountToEdit;

    // Reseta o form quando o modal abre
    useEffect(() => {
        if (isOpen) {
            setError(null);
            if (accountToEdit) {
                reset({
                    name: accountToEdit.name,
                    type: accountToEdit.type,
                    initialBalance: '0,00' // Não usado na edição
                });
            } else {
                reset({
                    name: '',
                    type: AccountType.ContaCorrente,
                    initialBalance: '0,00'
                });
            }
        }
    }, [isOpen, accountToEdit, reset]);

    // Máscara de moeda para o Saldo Inicial
    const handleAmountChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        let value = e.target.value.replace(/\D/g, '');
        if (value === '') value = '0';
        
        const numericValue = parseInt(value, 10) / 100;
        const formatted = numericValue.toLocaleString('pt-BR', { minimumFractionDigits: 2 });
        
        setValue('initialBalance', formatted);
    };

    const onSubmit = async (data: AccountFormData) => {
        setIsLoading(true);
        setError(null);

        try {
            let savedAccount: AccountResponseDto;

            if (isEditing && accountToEdit) {
                // --- EDIÇÃO ---
                const updateDto: UpdateAccountRequestDto = {
                    name: data.name,
                    type: data.type
                };
                const response = await accountService.update(accountToEdit.id, updateDto);
                savedAccount = response.data;
            } else {
                // --- CRIAÇÃO ---
                // Converte "1.000,50" para 1000.50
                const balanceStr = data.initialBalance || '0,00';
                const balanceNumber = parseFloat(balanceStr.replace(/\./g, '').replace(',', '.'));

                const createDto = {
                    name: data.name,
                    type: data.type,
                    initialBalance: balanceNumber
                };
                const response = await accountService.create(createDto);
                savedAccount = response.data;
            }

            onSuccess(savedAccount, isEditing);
            onClose();

        } catch (err) {
            const axiosError = err as AxiosError<ApiErrorResponse>;
            setError(axiosError.response?.data?.message || 'Erro ao salvar conta.');
        } finally {
            setIsLoading(false);
        }
    };

    if (!isOpen) return null;

    return (
        <div className="modal-overlay">
            <div className="modal-content">
                <h2>{isEditing ? 'Editar Conta' : 'Nova Conta'}</h2>
                
                {error && <div className="modal-error-message">{error}</div>}

                <form onSubmit={handleSubmit(onSubmit)}>
                    <div className="form-group">
                        <label>Nome da Conta</label>
                        <input 
                            {...register('name')} 
                            placeholder="Ex: Nubank" 
                            className={errors.name ? 'input-error' : ''}
                        />
                        {errors.name && <span className="field-error-message">{errors.name.message as string}</span>}
                    </div>

                    <div className="form-group">
                        <label>Tipo</label>
                        <select {...register('type')}>
                            <option value={AccountType.ContaCorrente}>Conta Corrente</option>
                            <option value={AccountType.Poupanca}>Poupança</option>
                            <option value={AccountType.Carteira}>Carteira (Dinheiro)</option>
                            <option value={AccountType.CartaoCredito}>Cartão de Crédito</option>
                            <option value={AccountType.Investimento}>Investimentos</option>
                            <option value={AccountType.Outro}>Outros</option>
                        </select>
                        {errors.type && <span className="field-error-message">{errors.type.message as string}</span>}
                    </div>

                    {/* Saldo Inicial só aparece na Criação */}
                    {!isEditing && (
                        <div className="form-group">
                            <label>Saldo Inicial (R$)</label>
                            <input 
                                type="text"
                                {...register('initialBalance')}
                                onChange={handleAmountChange}
                                placeholder="0,00"
                            />
                            <small className="hint-text">O saldo inicial não pode ser alterado depois.</small>
                        </div>
                    )}

                    <div className="modal-actions">
                        <button type="button" onClick={onClose} className="cancel-button" disabled={isLoading}>
                            Cancelar
                        </button>
                        <button type="submit" className="save-button" disabled={isLoading}>
                            {isLoading ? 'Salvando...' : 'Salvar'}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}