var builder = DistributedApplication.CreateBuilder(args);

var identidade = builder.AddProject<Projects.GestaoRestaurante_Identidade_Api>("identidade", launchProfileName: "https");
var estoque = builder.AddProject<Projects.GestaoRestaurante_Api>("estoque", launchProfileName: "https");
var vendas = builder.AddProject<Projects.GestaoRestaurante_Vendas_Api>("vendas", launchProfileName: "https");
builder.AddProject<Projects.GestaoRestaurante_Gateway_Api>("gateway", launchProfileName: "https")
    .WaitFor(identidade)
    .WaitFor(estoque)
    .WaitFor(vendas);
var pagamentos = builder.AddProject<Projects.GestaoRestaurante_Pagamentos_Api>("pagamentos", launchProfileName: "https");
builder.AddProject<Projects.GestaoRestaurante_Email_Api>("email", launchProfileName: "https");

builder.AddExecutable("stripe-listen", "stripe", ".",
        "listen",
        "--events", "payment_intent.succeeded,payment_intent.payment_failed",
        "--forward-to", "http://localhost:5268/api/v1/webhooks/stripe")
    .WaitFor(pagamentos);

builder.Build().Run();
