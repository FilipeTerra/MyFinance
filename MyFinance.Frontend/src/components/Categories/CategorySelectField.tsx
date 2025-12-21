import { useState } from 'react';
import { useTransactionFormLogic } from '../../hooks/useTransactionFormLogic';

export interface CategoryDto {
    id: string;
    name: string;
}

interface CategorySelectFieldProps {
    categories: CategoryDto[]; // Lista de categorias para exibir
    selectedId: string; // Valor controlado pelo React Hook Form do pai
    onChange: (id: string) => void; // Função para atualizar o React Hook Form do pai
    errorMessage?: string;
    disabled?: boolean;
    onCategoryCreated: (newCategory: CategoryDto) => void; // Callback para atualizar a lista no pai
}

export function CategorySelectField({
    categories,
    selectedId,
    onChange,
    errorMessage,
    disabled,
    onCategoryCreated
}: CategorySelectFieldProps) {
    const [isCreating, setIsCreating] = useState(false);
    const [newCategoryName, setNewCategoryName] = useState('');
    
    // Usamos o hook aqui para isolar a lógica de API deste componente
    const { createCategory, isLoading: isSaving, error, setError } = useTransactionFormLogic();

    const handleSaveClick = async () => {
        if (!newCategoryName.trim()) {
            setError('O nome da categoria é obrigatório.');
            return;
        }

        try {
            const newCategory = await createCategory({ name: newCategoryName });
            
            // Sucesso!
            onCategoryCreated(newCategory); // Avisa o pai que a lista cresceu
            onChange(newCategory.id); // Já seleciona a nova categoria automaticamente
            
            // Limpa estado local
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

    // Renderização: Modo Criação
    if (isCreating) {
        return (
            <div className="form-group">
                <label>Nova Categoria</label>
                <div className="input-wrapper-inline">
                    <input
                        type="text"
                        value={newCategoryName}
                        onChange={(e) => {
                            setNewCategoryName(e.target.value);
                            if(error) setError(null);
                        }}
                        placeholder="Nome da categoria"
                        disabled={isSaving || disabled}
                        className={error ? 'input-error' : ''}
                    />
                     <div className="action-buttons-inline">
                        <button type="button" onClick={handleCancel} disabled={isSaving} className="cancel-button-small">
                            ✕
                        </button>
                        <button type="button" onClick={handleSaveClick} disabled={isSaving} className="save-button-small">
                            {isSaving ? '...' : '✓'}
                        </button>
                    </div>
                </div>
                {error && <span className="field-error-message">{error}</span>}
            </div>
        );
    }

    // Renderização: Modo Seleção (Padrão)
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
                        </option> // <--- CORRIGIDO AQUI
                    ))}
                    {/* Fallback temporário caso não tenha categorias ainda */}
                    {categories.length === 0 && <option value="temp1">Exemplo (Sem dados)</option>}
                </select>
                {errorMessage && <span className="field-error-message">{errorMessage}</span>}
            </div>
            <button
                type="button"
                className="add-new-button"
                onClick={() => setIsCreating(true)}
                disabled={disabled}
                title="Criar nova categoria"
            >
                +
            </button>
        </div>
    );
}