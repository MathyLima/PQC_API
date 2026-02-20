using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PQC.API.Filters;
using PQC.INFRAESTRUCTURE.Data;
using PQC.INFRAESTRUCTURE.DependencyInjection;
using PQC.MODULES.Authentication.DependencyInjection;
using PQC.MODULES.Documents.DependencyInjection;
using PQC.MODULES.Documents.Domain.Interfaces;
using PQC.MODULES.Users.DependencyInjection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ========== 1️⃣ INFRAESTRUTURA COMPARTILHADA ==========
builder.Services.AddSharedInfrastructure(builder.Configuration);

// ========== 2️⃣ SERVIÇOS DA API ==========
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddHttpClient("PdfService", client =>
{
    client.BaseAddress = new Uri("http://localhost:8080"); // endereço do outro serviço
    client.Timeout = TimeSpan.FromMinutes(5);
});

// ========== 3️⃣ MÓDULOS ==========
builder.Services.AddUsersModule(builder.Configuration);
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddDocumentsModule();

// ========== 4️⃣ CONTROLLERS E FILTROS ==========
builder.Services.AddControllers();
builder.Services.AddMvc(options => options.Filters.Add<ExceptionFilter>());

// ========== 🆕 CORS - ADICIONE AQUI ==========
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5500",
            "http://127.0.0.1:5500",
            "http://localhost:3000",
            "http://127.0.0.1:3000",
            "http://localhost:5173"  // Caso use Vite
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

// ========== 5️⃣ SWAGGER COM JWT ==========
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.OperationFilter<SecurityRequirementsOperationFilter>();
});

// ========== 6️⃣ JWT AUTHENTICATION COM DEBUG ==========
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey não configurado");

Console.WriteLine($"🔑 JWT Config:");
Console.WriteLine($"   Issuer: {jwtSettings["Issuer"]}");
Console.WriteLine($"   Audience: {jwtSettings["Audience"]}");
Console.WriteLine($"   SecretKey length: {secretKey.Length} caracteres");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secretKey)
            ),
            ClockSkew = TimeSpan.Zero
        };

        // ========== DEBUG EVENTS ==========
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var authHeader = context.Request.Headers["Authorization"].ToString();
                Console.WriteLine($"📨 Authorization Header: {(string.IsNullOrEmpty(authHeader) ? "VAZIO" : authHeader.Substring(0, Math.Min(50, authHeader.Length)) + "...")}");
                return Task.CompletedTask;
            },

            OnTokenValidated = context =>
            {
                Console.WriteLine("✅ TOKEN VÁLIDO!");
                var claims = context.Principal?.Claims.Select(c => $"{c.Type}={c.Value}");
                Console.WriteLine($"   Claims: {string.Join(", ", claims ?? Array.Empty<string>())}");
                return Task.CompletedTask;
            },

            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"❌ FALHA NA AUTENTICAÇÃO!");
                Console.WriteLine($"   Tipo: {context.Exception.GetType().Name}");
                Console.WriteLine($"   Mensagem: {context.Exception.Message}");

                if (context.Exception is SecurityTokenExpiredException)
                {
                    Console.WriteLine("   ⏰ Token EXPIRADO!");
                }
                else if (context.Exception is SecurityTokenInvalidSignatureException)
                {
                    Console.WriteLine("   🔐 Assinatura INVÁLIDA! (SecretKey diferente?)");
                }
                else if (context.Exception is SecurityTokenInvalidIssuerException)
                {
                    Console.WriteLine("   🏢 Issuer INVÁLIDO!");
                }
                else if (context.Exception is SecurityTokenInvalidAudienceException)
                {
                    Console.WriteLine("   👥 Audience INVÁLIDO!");
                }

                return Task.CompletedTask;
            },

            OnChallenge = context =>
            {
                Console.WriteLine($"🚫 CHALLENGE!");
                Console.WriteLine($"   Error: {context.Error}");
                Console.WriteLine($"   ErrorDescription: {context.ErrorDescription}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ========== 7️⃣ BUILD APP ==========
var app = builder.Build();

// ========== 8️⃣ TESTAR CONEXÃO COM BANCO ==========
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.OpenConnectionAsync();
        Console.WriteLine("✅ Conectado ao banco com sucesso");
        await db.Database.CloseConnectionAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ ERRO AO CONECTAR NO BANCO:");
        Console.WriteLine(ex.ToString());
    }
}

// ========== 9️⃣ MIDDLEWARE PIPELINE ==========
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ========== 🆕 USE CORS - ADICIONE ANTES DE UseHttpsRedirection ==========
app.UseCors("AllowFrontend");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("🚀 API iniciada!");

app.Run();