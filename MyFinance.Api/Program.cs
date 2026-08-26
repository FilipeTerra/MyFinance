using Microsoft.EntityFrameworkCore;
using MyFinance.Infrastructure;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Services;
using MyFinance.Infrastructure.HostedServices;
using MyFinance.Infrastructure.Integrations;
using MyFinance.Infrastructure.Repositories;
using MyFinance.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer; // Para JwtBearerDefaults
using Microsoft.IdentityModel.Tokens; // Para TokenValidationParameters, SymmetricSecurityKey
using System.Text;

// Carrega segredos de um .env local (não versionado — ver .gitignore), no mesmo
// espírito do python-dotenv já usado no MyFinance.AiAgent. Precisa rodar ANTES de
// CreateBuilder, pois é nesse momento que o provider de variáveis de ambiente lê
// o processo. Em produção o .env não existe e as variáveis reais do ambiente
// (ex: Docker, CI) prevalecem — LoadDotEnv nunca sobrescreve o que já está setado.
DotEnvLoader.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

// Ponte entre o nome do .env (BRAPI_TOKEN, mais curto para uso manual) e a chave
// hierárquica que o binding de configuração espera (ExternalServices:Brapi:Token,
// que via variável de ambiente vira ExternalServices__Brapi__Token).
var brapiToken = Environment.GetEnvironmentVariable("BRAPI_TOKEN");
if (!string.IsNullOrWhiteSpace(brapiToken))
    Environment.SetEnvironmentVariable("ExternalServices__Brapi__Token", brapiToken);

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    options.IncludeXmlComments(xmlPath);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policyBuilder =>
    {
        policyBuilder
               .AllowAnyOrigin()
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"] ?? throw new ArgumentNullException("JwtSettings:Secret", "Chave secreta JWT náo configurada.");

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
        ValidateLifetime = true, // Verifica se o token náo expirou
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero // Remove a toleráncia padráo de 5 minutos na expiração
    };
});

// Registrar serviáos e repositários para Injeção de Dependáncia
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IFinancialGoalRepository, FinancialGoalRepository>();
builder.Services.AddScoped<IFinancialGoalService, FinancialGoalService>();
builder.Services.AddScoped<IInvestimentoRepository, InvestimentoRepository>();
builder.Services.AddScoped<IInvestimentoService, InvestimentoService>();
builder.Services.AddScoped<IProjecaoInvestimentoService, ProjecaoInvestimentoService>();
builder.Services.AddScoped<ICotacaoHistoricoRepository, CotacaoHistoricoRepository>();
builder.Services.AddScoped<IMarketSyncService, MarketSyncService>();

// Integrações externas (brapi/B3, Banco Central, Agente de IA) — URLs, tokens e
// TTLs de cache vêm da seção ExternalServices da configuração.
builder.Services.AddIntegrations(builder.Configuration);

builder.Services.AddHostedService<StartupMarketSyncHostedService>();
var app = builder.Build();
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// Leitor minimalista de arquivos .env — sem dependência de pacote externo.
/// Formato: uma variável por linha (CHAVE=valor), linhas em branco e
/// iniciadas com # são ignoradas, aspas ao redor do valor são removidas.
/// Nunca sobrescreve uma variável de ambiente já definida no processo —
/// em produção/CI/Docker, o valor real do ambiente sempre vence.
/// </summary>
internal static class DotEnvLoader
{
    public static void Load(string path)
    {
        if (!File.Exists(path))
            return;

        foreach (var linha in File.ReadAllLines(path))
        {
            var trimmed = linha.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;

            var separador = trimmed.IndexOf('=');
            if (separador <= 0)
                continue;

            var chave = trimmed[..separador].Trim();
            var valor = trimmed[(separador + 1)..].Trim().Trim('"', '\'');

            if (Environment.GetEnvironmentVariable(chave) is null)
                Environment.SetEnvironmentVariable(chave, valor);
        }
    }
}