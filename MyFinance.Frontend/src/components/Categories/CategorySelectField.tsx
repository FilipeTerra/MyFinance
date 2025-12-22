import { useState } from 'react';
import { useTransactionFormLogic } from '../../hooks/useTransactionFormLogic';

export interface CategoryDto {
    id: string;
    name: string;
}

interface CategorySelectFieldProps {
    categories: CategoryDto[];
    selectedId: string;
    onChange: (id: string) => void;
    errorMessage?: string;
    disabled?: boolean;
    onCategoryCreated: (newCategory: CategoryDto) => void;
    allowCreation?: boolean;
}

export function CategorySelectField({
    categories,
    selectedId,
    onChange,
    errorMessage,
    disabled,
    onCategoryCreated,
    allowCreation = true
}: CategorySelectFieldProps) {
    const [isCreating, setIsCreating] = useState(false);
    const [newCategoryName, setNewCategoryName] = useState('');    
    const { createCategory, isLoading: isSaving, error, setError } = useTransactionFormLogic();

    const handleSaveClick = async () => {
        if (!newCategoryName.trim()) {
            setError('O nome da categoria é obrigatório.');
            return;
        }

        try {
            const newCategory = await createCategory({ name: newCategoryName });
            
            // Sucesso: notifica o pai e seleciona a nova categoria
            onCategoryCreated(newCategory); 
            onChange(newCategory.id);
            
            // Reseta o estado
            setIsCreating(false);
            setNewCategoryName('');
        } catch (err) {
            console.error(err);
        }
    };

    const handleCancel = () => {
        setIsCreating(false);
        setNewCategoryName('');
        setError(null);
    };

    // --- MODO CRIAÇÃO: Bloco Cinza "Adicional" ---
    if (isCreating) {
        return (
            <div className="create-category-form">
                <h4>Nova Categoria</h4>
                
                <div className="form-group">
                    <label htmlFor="newCategoryName">Nome da Categoria</label>
                    <input
                        id="newCategoryName"
                        type="text"
                        value={newCategoryName}
                        onChange={(e) => {
                            setNewCategoryName(e.target.value);
                            if(error) setError(null);
                        }}
                        placeholder="Ex: Alimentação, Transporte..."
                        disabled={isSaving || disabled}
                        className={error ? 'input-error' : ''}
                        autoFocus
                    />
                     {error && <span className="field-error-message">{error}</span>}
                </div>

                <div className="create-category-actions">
                    <button 
                        type="button" 
                        onClick={handleCancel} 
                        disabled={isSaving} 
                        className="cancel-button"
                    >
                        Cancelar
                    </button>
                    <button 
                        type="button" 
                        onClick={handleSaveClick} 
                        disabled={isSaving} 
                        className="save-button"
                    >
                        {isSaving ? 'Salvando...' : 'Salvar Categoria'}
                    </button>
                </div>
            </div>
        );
    }

    // --- MODO SELEÇÃO (Padrão) ---
    return (
        <div className="form-group form-group-with-button">
            <div className="input-wrapper">
                <label htmlFor="categoryId">Categoria</label>
                <select
                    id="categoryId"
                    value={selectedId}
                    onChange={(e) => onChange(e.target.value)}
                    disabled={disabled}
                    className={errorMessage ? 'input-error' : ''}
                >
                    <option value="" disabled>Selecione uma categoria</option>
                    {categories.map((cat) => (
                        <option key={cat.id} value={cat.id}>
                            {cat.name}
                        </option>
                    ))}
                    {categories.length === 0 && <option value="" disabled>Nenhuma categoria disponível</option>}
                </select>
                {errorMessage && <span className="field-error-message">{errorMessage}</span>}
            </div>
            {allowCreation && (
                <button
                    type="button"
                    className="add-new-button"
                    onClick={() => setIsCreating(true)}
                    disabled={disabled}
                    title="Criar nova categoria"
                >
                    +
                </button>
            )}
        </div>
    );
}