export interface FinancialGoalResponseDto {
  id: string;
  userId: string;
  name: string;
  targetAmount: number;
  currentAmount: number;
  deadline: string;
  createdAt: string;
  isCompleted: boolean;
}
