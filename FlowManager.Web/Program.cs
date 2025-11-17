using FlowManager.Core.Services;
using FlowManager.Core.Services.KnowledgeBase;
using FlowManager.Core.Engine;
using FlowManager.Web.Components;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register Knowledge Base services first (before FlowOrchestrator)
var kbBasePath = builder.Configuration["KnowledgeBase:BasePath"] ?? "Data/KnowledgeBase";
var openAiApiKey = builder.Configuration["KnowledgeBase:OpenAIApiKey"] ?? "";
var bingApiKey = builder.Configuration["KnowledgeBase:BingSearchApiKey"];

// Ensure the KB base path is absolute
if (!Path.IsPathRooted(kbBasePath))
{
    kbBasePath = Path.Combine(Directory.GetCurrentDirectory(), kbBasePath);
}

builder.Services.AddSingleton<IKnowledgeBaseService>(sp =>
{
    return new KnowledgeBaseService(kbBasePath, openAiApiKey);
});

builder.Services.AddSingleton<IWebSearchService>(sp =>
{
    return new WebSearchService(bingApiKey);
});

//  Register FlowStorageService with custom storage path
var flowBasePath = builder.Configuration["FlowStorage:BasePath"] ?? "Data/Flows";

// Ensure the flow base path is absolute
if (!Path.IsPathRooted(flowBasePath))
{
    flowBasePath = Path.Combine(Directory.GetCurrentDirectory(), flowBasePath);
}

builder.Services.AddSingleton<FlowManager.Core.Services.FlowStorageService>(sp =>
{
    return new FlowStorageService(flowBasePath);
});

// ✨ NEW: Register FlowOrchestrator with Knowledge Base services
builder.Services.AddSingleton<FlowManager.Core.Engine.FlowOrchestrator>(sp =>
{
    var kbService = sp.GetService<IKnowledgeBaseService>();
    var webService = sp.GetService<IWebSearchService>();
    return new FlowManager.Core.Engine.FlowOrchestrator(kbService, webService);
});

//  Register conversation storage service
// Use FileBasedConversationStore for persistence (conversations survive app restarts)
// To switch to in-memory storage (dev/testing), replace with:
// builder.Services.AddSingleton<IConversationStore, InMemoryConversationStore>();
builder.Services.AddSingleton<FlowManager.Core.Services.IConversationStore, FlowManager.Core.Services.FileBasedConversationStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
