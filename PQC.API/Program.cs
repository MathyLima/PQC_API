using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PQC.API.Filters;
using PQC.MODULES.Auth.DependencyInjection;
using PQC.MODULES.Auth.Domain.Settings;
using PQC.MODULES.Documents.DependencyInjection;
using PQC.MODULES.Infraestructure.Data;
using PQC.MODULES.Users.DependencyInjection;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 36))
    )
);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"];


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
                Encoding.UTF8.GetBytes(secretKey ?? string.Empty)
            )
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddMvc(option => option.Filters.Add(typeof(ExceptionFilter)));


builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt")
);

//Modulos de dependency injection
builder.Services.AddUsersModule();
builder.Services.AddAuthModule();

builder.Services.AddUDocumentsModule(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.OpenConnection();
        Console.WriteLine("✅ Conectado ao banco com sucesso");
        db.Database.CloseConnection();
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ ERRO AO CONECTAR NO BANCO:");
        Console.WriteLine(ex.ToString());
    }
}


if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();