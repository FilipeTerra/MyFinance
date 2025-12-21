using Microsoft.EntityFrameworkCore;
using MyFinance.Infrastructure; 
using MyFinance.Application.Interfaces.Services;
using MyFinance.Application.Interfaces.Repositories; 
using MyFinance.Application.Services;   
using MyFinance.Infrastructure.Repositories; 
using Microsoft.AspNetCore.Authentication.JwtBearer; // Para JwtBearerDefaults
using Microsoft.IdentityModel.Tokens; // Para TokenValidationParameters, SymmetricSecurityKey
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- Configura��o dos Servi�os ---

// Ler a Connection String do appsettings.Development.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Registrar o DbContext com o provedor Npgsql
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Registrar os servi�os dos Controllers
builder.Services.AddControllers();

// Configurar o Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configurar o CORS (Permitir acesso do Frontend React)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policyBuilder =>
    {
        policyBuilder.WithOrigins("http://localhost:5173")
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

// Ler configura��es do JWT do appsettings
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"] ?? throw new ArgumentNullException("JwtSettings:Secret", "Chave secreta JWT n�o configurada.");

// Configurar Autentica��o JWT Bearer
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
        ValidateLifetime = true, // Verifica se o token n�o expirou
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero // Remove a toler�ncia padr�o de 5 minutos na expira��o
    };
});

// Registrar servi�os e reposit�rios para Inje��o de Depend�ncia
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

// --- Constru��o do App ---
var app = builder.Build();

// --- Configura��o do Pipeline HTTP ---

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Habilitar Swagger UI em desenvolvimento
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Habilitar a pol�tica CORS que definimos
app.UseCors("AllowReactApp");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Mapear as rotas para os Controllers
app.MapControllers();

// --- Executar o App ---
app.Run();