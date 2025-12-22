import './CreateAccountButton.css';

interface CreateAccountButtonProps {
    onClick: () => void;
}

export function CreateAccountButton({ onClick }: CreateAccountButtonProps) {
    return (
        <button className="create-account-btn" onClick={onClick}>
            + Nova Conta
        </button>
    );
}