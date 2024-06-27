using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using loja.data;
using loja.models;
using loja.services;

var builder = WebApplication.CreateBuilder(args);

// Adicione a configuração para ler o arquivo appsettings.json
builder.Configuration.SetBasePath(Directory.GetCurrentDirectory());
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

// Adicione os serviços ao container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Loja API", Version = "v1" });
});

// Configurar conexão com BD
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<LojaDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Adicionar serviços para produtos, clientes, fornecedores e usuários
builder.Services.AddScoped<ProdutoService>();
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<FornecedorService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<VendaService>();

// Configurar autenticação JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("abc")),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero // Para considerar expiração exata do token
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) &&
                    context.Request.Path.StartsWithSegments("/hub"))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            }
        };
    });

// Adicionar serviço de autorização
builder.Services.AddAuthorization();

var app = builder.Build();

// Configurar Kestrel para HTTPS se estiver em produção
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts(); // HSTS para produção
}

// Habilitar autenticação e autorização
app.UseAuthentication();
app.UseAuthorization();

// Middleware para Swagger
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Loja API v1"));

// Mapeamento dos endpoints para produtos
app.MapGet("/produtos", async (ProdutoService productService) =>
{
    var produtos = await productService.GetAllProductsAsync();
    return Results.Ok(produtos);
}).RequireAuthorization(); // Proteger endpoint com autenticação

app.MapGet("/produtos/{id}", async (int id, ProdutoService productService) =>
{
    var produto = await productService.GetProductByIdAsync(id);
    if (produto == null)
    {
        return Results.NotFound($"Product with ID {id} not found.");
    }
    return Results.Ok(produto);
}).RequireAuthorization(); // Proteger endpoint com autenticação

app.MapPost("/produtos", async (Produto produto, ProdutoService productService) =>
{
    await productService.AddProductAsync(produto);
    return Results.Created($"/produtos/{produto.Id}", produto);
}).RequireAuthorization(); // Proteger endpoint com autenticação

app.MapPut("/produtos/{id}", async (int id, Produto produto, ProdutoService productService) =>
{
    if (id != produto.Id)
    {
        return Results.BadRequest("Product ID mismatch.");
    }
    await productService.UpdateProductAsync(produto);
    return Results.Ok();
}).RequireAuthorization(); // Proteger endpoint com autenticação

app.MapDelete("/produtos/{id}", async (int id, ProdutoService productService) =>
{
    await productService.DeleteProductAsync(id);
    return Results.Ok();
}).RequireAuthorization(); // Proteger endpoint com autenticação

// Mapeamento dos endpoints para clientes
app.MapPost("/clientes", async (Cliente cliente, ClienteService clienteService) =>
{
    await clienteService.AddClienteAsync(cliente);
    return Results.Created($"/clientes/{cliente.Id}", cliente);
}).RequireAuthorization(); // Proteger endpoint com autenticação

app.MapGet("/clientes", async (ClienteService clienteService) =>
{
    var clientes = await clienteService.GetAllClientesAsync();
    return Results.Ok(clientes);
}).RequireAuthorization(); // Proteger endpoint com autenticação

app.MapGet("/clientes/{id}", async (int id, ClienteService clienteService) =>
{
    var cliente = await clienteService.GetClienteByIdAsync(id);
    if (cliente == null)
    {
        return Results.NotFound($"Cliente with ID {id} not found.");
    }
    return Results.Ok(cliente);
}).RequireAuthorization(); // Proteger endpoint com autenticação

app.MapPut("/clientes/{id}", async (int id, Cliente cliente, ClienteService clienteService) =>
{
    if (id != cliente.Id)
    {
        return Results.BadRequest("Cliente ID mismatch.");
    }
    await clienteService.UpdateClienteAsync(cliente);
    return Results.Ok();
}).RequireAuthorization(); // Proteger endpoint com autenticação

app.MapDelete("/clientes/{id}", async (int id, ClienteService clienteService) =>
{
    await clienteService.DeleteClienteAsync(id);
    return Results.Ok();
}).RequireAuthorization(); // Proteger endpoint com autenticação

// Mapeamento dos endpoints para fornecedores
app.MapPost("/fornecedores", async (Fornecedor fornecedor, FornecedorService fornecedorService) =>
{
    await fornecedorService.AddFornecedorAsync(fornecedor);
    return Results.Created($"/fornecedores/{fornecedor.Id}", fornecedor);
}).RequireAuthorization(); // Proteger endpoint com autenticação

app.MapGet("/fornecedores", async (FornecedorService fornecedorService) =>
{
    var fornecedores = await fornecedorService.GetAllFornecedoresAsync();
    return Results.Ok(fornecedores);
}).RequireAuthorization(); // Proteger endpoint com autenticação

app.MapGet("/fornecedores/{id}", async (int id, FornecedorService fornecedorService) =>
{
    var fornecedor = await fornecedorService.GetFornecedorByIdAsync(id);
    if (fornecedor == null)
    {
        return Results.NotFound($"Fornecedor with ID {id} not found.");
    }
    return Results.Ok(fornecedor);
}).RequireAuthorization(); // Proteger endpoint com autenticação

app.MapPut("/fornecedores/{id}", async (int id, Fornecedor fornecedor, FornecedorService fornecedorService) =>
{
    if (id != fornecedor.Id)
    {
        return Results.BadRequest("Fornecedor ID mismatch.");
    }
    await fornecedorService.UpdateFornecedorAsync(fornecedor);
    return Results.Ok();
}).RequireAuthorization(); // Proteger endpoint com autenticação

app.MapDelete("/fornecedores/{id}", async (int id, FornecedorService fornecedorService) =>
{
    await fornecedorService.DeleteFornecedorAsync(id);
    return Results.Ok();
}).RequireAuthorization(); // Proteger endpoint com autenticação

// Mapeamento dos endpoints para usuários
app.MapPost("/usuarios", async (Usuario usuario, UsuarioService usuarioService) =>
{
    await usuarioService.AddUsuarioAsync(usuario);
    return Results.Created($"/usuarios/{usuario.Id}", usuario);
}).RequireAuthorization(); // Proteger endpoint com autenticação

app.MapGet("/usuarios", async (UsuarioService usuarioService) =>
{
    var usuarios = await usuarioService.GetAllUsuariosAsync();
    return Results.Ok(usuarios);
}).RequireAuthorization(); // Proteger endpoint com autenticação

app.MapGet("/usuarios/{id}", async (int id, UsuarioService usuarioService) =>
{
    var usuario = await usuarioService.GetUsuarioByIdAsync(id);
    if (usuario == null)
    {
        return Results.NotFound($"Usuario with ID {id} not found.");
    }
    return Results.Ok(usuario);
}).RequireAuthorization(); // Proteger endpoint com autenticação

app.MapPut("/usuarios/{id}", async (int id, Usuario usuario, UsuarioService usuarioService) =>
{
    if (id != usuario.Id)
    {
        return Results.BadRequest("Usuario ID mismatch.");
    }
    await usuarioService.UpdateUsuarioAsync(usuario);
    return Results.Ok();
}).RequireAuthorization(); // Proteger endpoint com autenticação

app.MapDelete("/usuarios/{id}", async (int id, UsuarioService usuarioService) =>
{
    await usuarioService.DeleteUsuarioAsync(id);
    return Results.Ok();
}).RequireAuthorization(); // Proteger endpoint com autenticação

// Endpoint para gravar uma venda
app.MapPost("/vendas", async (Venda venda, VendaService vendaService) =>
{
    try
    {
        var novaVenda = await vendaService.GravarVendaAsync(venda);
        return Results.Created($"/vendas/{novaVenda.Id}", novaVenda);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message);
    }
}).RequireAuthorization();

// Endpoint para consultar vendas por produto (detalhada)
app.MapGet("/vendas/produto/{produtoId}", async (int produtoId, VendaService vendaService) =>
{
    var vendasDetalhadas = await vendaService.ConsultarVendasPorProdutoDetalhadaAsync(produtoId);
    return Results.Ok(vendasDetalhadas);
}).RequireAuthorization();

// Endpoint para consultar vendas por produto (sumarizada)
app.MapGet("/vendas/produto/{produtoId}/sumarizada", async (int produtoId, VendaService vendaService) =>
{
    var vendasSumarizadas = await vendaService.ConsultarVendasPorProdutoSumarizadaAsync(produtoId);
    return Results.Ok(vendasSumarizadas);
}).RequireAuthorization();

// Endpoint para consultar vendas por cliente (detalhada)
app.MapGet("/vendas/cliente/{clienteId}", async (int clienteId, VendaService vendaService) =>
{
    var vendasDetalhadas = await vendaService.ConsultarVendasPorClienteDetalhadaAsync(clienteId);
    return Results.Ok(vendasDetalhadas);
}).RequireAuthorization();

// Endpoint para consultar vendas por cliente (detalhada)
app.MapGet("/vendas/cliente/{clienteId}", async (int clienteId, VendaService vendaService) =>
{
    var vendasDetalhadas = await vendaService.ConsultarVendasPorClienteDetalhadaAsync(clienteId);
    return Results.Ok(vendasDetalhadas);
}).RequireAuthorization();

// Endpoint para consultar vendas por cliente (sumarizada)
app.MapGet("/vendas/cliente/{clienteId}/sumarizada", async (int clienteId, VendaService vendaService) =>
{
    var vendasSumarizadas = await vendaService.ConsultarVendasPorClienteSumarizadaAsync(clienteId);
    return Results.Ok(vendasSumarizadas);
}).RequireAuthorization();

// Endpoint para login e geração de token JWT
app.MapPost("/login", async (HttpContext context, UsuarioService usuarioService) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    var json = JsonDocument.Parse(body);
    var email = json.RootElement.GetProperty("email").GetString();
    var senha = json.RootElement.GetProperty("senha").GetString();

    var usuario = await usuarioService.GetUsuarioByEmailAsync(email);
    if (usuario == null || usuario.Senha != senha)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Invalid email or password.");
        return;
    }

    var token = GenerateToken(email);
    await context.Response.WriteAsync(token);
});

// Método para gerar token JWT
string GenerateToken(string email)
{
    var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
    var key = Encoding.ASCII.GetBytes("abc");
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.Email, email)
        }),
        Expires = DateTime.UtcNow.AddHours(1),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
}

// Inicializa a aplicação
app.Run("http://localhost:5292");
