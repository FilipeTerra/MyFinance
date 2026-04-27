export interface UploadFileResponseDto<T> {
    data: T;           
    success: boolean; 
    message?: string; 
}