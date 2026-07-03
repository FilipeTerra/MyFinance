export interface UserProfileResponseDto {
    id: string;
    name: string;
    email: string;
    monthlyIncome: number | null;
}

export interface UpdateUserProfileRequestDto {
    monthlyIncome: number | null;
}
