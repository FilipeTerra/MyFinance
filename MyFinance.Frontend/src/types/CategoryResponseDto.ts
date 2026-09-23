// src/types/CategoryResponseDto.ts
import type { ExpenseNature } from './ExpenseNature';

export interface CategoryResponseDto {
    id: string;
    name: string;
    /** Classificação de cortabilidade dada pelo usuário; base do plano de corte da sugestão. */
    nature: ExpenseNature;
}
