using Microsoft.EntityFrameworkCore;
using MyFinance.Infrastructure; // Para acessar o ApplicationDbContext
using MyFinance.Application.Interfaces.Services; // Para IAuthService, IUserRepository
using MyFinance.Application.Interfaces.Repositories; // Para IAuthService, IUserRepository
using MyFinance.Application.Services;   // Para AuthService
using MyFinance.Infrastructure.Repositories; // Para UserRepository
using Microsoft.AspNetCore.Authentication.JwtBearer; // Para JwtBearerDefaults
using Microsoft.IdentityModel.Tokens; // Para TokenValidationParameters, SymmetricSecurityKey
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- Configuração dos Serviços ---

// Ler a Connection String do appsettings.Development.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Registrar o DbContext com o provedor Npgsql
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Registrar os serviços dos Controllers
builder.Services.AddControllers();

// Configurar o Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configurar o CORS (Permitir acesso do Frontend React)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policyBuilder =>
    {
        // PERMITINDO AMBAS AS ORIGENS (HTTP E HTTPS) PARA localhost:5173
        policyBuilder.WithOrigins("http://localhost:5173", "https://localhost:5173")
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

// Ler configurações do JWT do appsettings
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"] ?? throw new ArgumentNullException("JwtSettings:Secret", "Chave secreta JWT não configurada.");

// Configurar Autenticação JWT Bearer
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true, // Verifica se o token não expirou
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero // Remove a tolerância padrão de 5 minutos na expiração
    };
});

// Registrar serviços e repositórios para Injeção de Dependência
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// --- Construção do App ---
var app = builder.Build();

// --- Configuração do Pipeline HTTP ---

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Habilitar Swagger UI em desenvolvimento
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Habilitar a política CORS que definimos
app.UseCors("AllowReactApp");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Mapear as rotas para os Controllers
app.MapControllers();

// --- Executar o App ---
app.Run();