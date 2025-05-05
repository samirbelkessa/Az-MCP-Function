using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using static FunctionsSnippetTool.ToolsInformation;

var builder = FunctionsApplication.CreateBuilder(args);

// Configuration des services de base
builder.ConfigureFunctionsWebApplication();

// Activer les métadonnées MCP
builder.EnableMcpToolMetadata();

// Configuration pour GetSnippet
builder
    .ConfigureMcpTool(GetSnippetToolName)
    .WithProperty(SnippetNamePropertyName, PropertyType, SnippetNamePropertyDescription);

// Configuration pour SaveSnippet
builder
    .ConfigureMcpTool(SaveSnippetToolName)
    .WithProperty(SnippetNamePropertyName, PropertyType, SnippetNamePropertyDescription)
    .WithProperty(SnippetPropertyName, PropertyType, SnippetPropertyDescription);

// Configuration pour ListSnippets
builder
    .ConfigureMcpTool(ListSnippetsToolName);

// Configuration pour SearchSnippets
builder
    .ConfigureMcpTool(SearchSnippetsToolName)
    .WithProperty(SearchQueryPropertyName, PropertyType, SearchQueryPropertyDescription);

// Configuration pour EnhanceSnippet
builder
    .ConfigureMcpTool(EnhanceSnippetToolName)
    .WithProperty(SnippetNamePropertyName, PropertyType, SnippetNamePropertyDescription)
    .WithProperty(EnhanceTypePropertyName, PropertyType, EnhanceTypePropertyDescription);

// Configuration pour TranslateSnippet
builder
    .ConfigureMcpTool(TranslateSnippetToolName)
    .WithProperty(SnippetNamePropertyName, PropertyType, SnippetNamePropertyDescription)
    .WithProperty(TargetLanguagePropertyName, PropertyType, TargetLanguagePropertyDescription);

// Construction de l'application
var app = builder.Build();

// Diagnostic - Affiche les outils MCP enregistrés au démarrage
try
{
    Console.WriteLine("Starting the Azure Function App with MCP tools");
    Console.WriteLine($"GetSnippet configured: {GetSnippetToolName}");
    Console.WriteLine($"SaveSnippet configured: {SaveSnippetToolName}");
    Console.WriteLine($"ListSnippets configured: {ListSnippetsToolName}");
    Console.WriteLine($"SearchSnippets configured: {SearchSnippetsToolName}");
    Console.WriteLine($"EnhanceSnippet configured: {EnhanceSnippetToolName}");
    Console.WriteLine($"TranslateSnippet configured: {TranslateSnippetToolName}");
    
    // Vérifier le chemin du fichier JSON
    var jsonPath = Environment.GetEnvironmentVariable("MCP_TOOL_JSON_FILE") ?? "D:\\home\\site\\wwwroot\\mcp-tools.json";
    Console.WriteLine($"MCP Tools JSON file path: {jsonPath}");
    
    // Vérifier la configuration OpenAI
    var apiKey = Environment.GetEnvironmentVariable("OpenAIApiKey");
    var endpoint = Environment.GetEnvironmentVariable("OpenAIEndpoint");
    var useAzureOpenAI = Environment.GetEnvironmentVariable("UseAzureOpenAI");
    var deployment = Environment.GetEnvironmentVariable("OpenAIDeployment");
    Console.WriteLine($"OpenAI API Key configured: {!string.IsNullOrEmpty(apiKey)}");
    Console.WriteLine($"OpenAI Endpoint configured: {!string.IsNullOrEmpty(endpoint)}");
    Console.WriteLine($"Using Azure OpenAI: {useAzureOpenAI}");
    
    if (bool.TryParse(useAzureOpenAI, out bool isAzure) && isAzure)
    {
        Console.WriteLine($"Azure OpenAI deployment: {deployment}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error during startup: {ex.Message}");
}

// Démarrage de l'application
app.Run();