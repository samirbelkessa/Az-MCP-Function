using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs;
using Microsoft.Extensions.Logging;
using static FunctionsSnippetTool.ToolsInformation;
using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http;
using System.Net.Http.Json;
namespace FunctionsSnippetTool;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Azure.Functions.Worker.Extensions.Timer;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

public class SnippetsTool
{
    private readonly ILogger<SnippetsTool> _logger;
    private const string BlobPath = "snippets/{mcptoolargs." + SnippetNamePropertyName + "}.json";

    public SnippetsTool(ILogger<SnippetsTool> logger)
    {
        _logger = logger;
    }

    [Function(nameof(GetSnippet))]
    public object GetSnippet(
        [McpToolTrigger(GetSnippetToolName, GetSnippetToolDescription)]
            ToolInvocationContext context,
        [BlobInput(BlobPath)] string snippetContent
    )
    {
         _logger.LogInformation($"GetSnippet called");
    return snippetContent;
    }

    [Function(nameof(SaveSnippet))]
    [BlobOutput(BlobPath)]
    public string SaveSnippet(
        [McpToolTrigger(SaveSnippetToolName, SaveSnippetToolDescription)]
            ToolInvocationContext context,
        [McpToolProperty(SnippetNamePropertyName, PropertyType, SnippetNamePropertyDescription)]
            string name,
        [McpToolProperty(SnippetPropertyName, PropertyType, SnippetPropertyDescription)]
            string snippet
    )
    {
        _logger.LogInformation($"Saving snippet: {name}");
        return snippet;
    }

    [Function(nameof(ListSnippets))]
    public async Task<List<string>> ListSnippets(
        [McpToolTrigger(ListSnippetsToolName, ListSnippetsToolDescription)]
            ToolInvocationContext context,
        [BlobInput("snippets")] BlobContainerClient containerClient
    )
    {
        var snippetNames = new List<string>();
        
        // Assurez-vous que le conteneur existe
        await containerClient.CreateIfNotExistsAsync();
        
        // Récupérer tous les blobs dans le conteneur
        await foreach (var blobItem in containerClient.GetBlobsAsync())
        {
            // Extraire le nom du snippet à partir du nom du blob
            string blobName = blobItem.Name;
            if (blobName.EndsWith(".json"))
            {
                string snippetName = Path.GetFileNameWithoutExtension(blobName);
                snippetNames.Add(snippetName);
            }
        }
        
        _logger.LogInformation($"Found {snippetNames.Count} snippets");
        return snippetNames;
    }
/*
    [Function(nameof(SearchSnippets))]
    public async Task<List<SearchResult>> SearchSnippets(
        [McpToolTrigger(SearchSnippetsToolName, SearchSnippetsToolDescription)]
            ToolInvocationContext context,
        [McpToolProperty(SearchQueryPropertyName, PropertyType, SearchQueryPropertyDescription)]
            string query,
        [BlobInput("snippets")] BlobContainerClient containerClient
    )
    {
        _logger.LogInformation($"Searching snippets with query: {query}");
        
        try
        {
            // Valider la configuration OpenAI
            ValidateOpenAIConfiguration();
            
            var results = new List<SearchResult>();
            var apiKey = Environment.GetEnvironmentVariable("OpenAIApiKey");
            
            // Récupérer tous les snippets
            List<SnippetInfo> snippets = new List<SnippetInfo>();
            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                if (blobItem.Name.EndsWith(".json"))
                {
                    var blobClient = containerClient.GetBlobClient(blobItem.Name);
                    var content = await blobClient.DownloadContentAsync();
                    string snippetContent = content.Value.Content.ToString();
                    string snippetName = Path.GetFileNameWithoutExtension(blobItem.Name);
                    
                    snippets.Add(new SnippetInfo { 
                        Name = snippetName, 
                        Content = snippetContent 
                    });
                }
            }
            
            // Construire le prompt pour l'API OpenAI
            string systemPrompt = @"
                Vous êtes un assistant de recherche de code. Vous allez recevoir plusieurs snippets de code et une requête de recherche.
                Analysez les snippets et renvoyez uniquement ceux qui correspondent à la requête, classés par pertinence.
                Format your response as structured information with each relevant snippet having a name, a relevance score (0-100), and an explanation.
                Example format:
                Snippet: snippetName1
                Relevance Score: 95
                Explanation: Ce snippet est pertinent car...

                Snippet: snippetName2
                Relevance Score: 82
                Explanation: Ce snippet est pertinent car...
                
                Only include snippets with a relevance score of 60 or higher.
            ";
            
            string snippetsText = string.Join("\n\n", snippets.Select(s => $"Nom: {s.Name}\nCode:\n```\n{s.Content}\n```"));
            string userPrompt = $"Requête: {query}\n\nSnippets disponibles:\n{snippetsText}";
            
            // Utiliser OpenAI pour trouver les snippets pertinents
            var openAiResults = await CallOpenAI(apiKey, systemPrompt, userPrompt);
            
            // Analyser les résultats retournés par OpenAI
            foreach (var snippet in snippets)
            {
                if (openAiResults.Contains(snippet.Name))
                {
                    // Extraire le score et l'explication
                    int relevanceScore = ExtractRelevanceScore(openAiResults, snippet.Name);
                    string explanation = ExtractExplanation(openAiResults, snippet.Name);
                    
                    if (relevanceScore >= 60)
                    {
                        results.Add(new SearchResult
                        {
                            Name = snippet.Name,
                            RelevanceScore = relevanceScore,
                            Explanation = explanation
                        });
                    }
                }
            }
            
            // Trier par score de pertinence
            return results.OrderByDescending(r => r.RelevanceScore).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching snippets");
            throw;
        }
    }
*/
[Function(nameof(SearchSnippets))]
public async Task<List<SearchResult>> SearchSnippets(
    [McpToolTrigger(SearchSnippetsToolName, SearchSnippetsToolDescription)]
        ToolInvocationContext context,
    [McpToolProperty(SearchQueryPropertyName, PropertyType, SearchQueryPropertyDescription)]
        string query
)
{
    _logger.LogInformation($"Searching snippets with query: {query}");
    
    try
    {
        // Get Azure Search configuration
        string searchServiceEndpoint = Environment.GetEnvironmentVariable("AzureSearchEndpoint");
        string searchApiKey = Environment.GetEnvironmentVariable("AzureSearchApiKey");
        string indexName = Environment.GetEnvironmentVariable("AzureSearchIndexName") ?? "snippets";
        
        if (string.IsNullOrEmpty(searchServiceEndpoint) || string.IsNullOrEmpty(searchApiKey))
        {
            _logger.LogError("Azure Search configuration missing");
            throw new InvalidOperationException("Azure Search configuration is incomplete");
        }
        
        // Create Azure Search client
        Uri serviceEndpoint = new Uri(searchServiceEndpoint);
        AzureKeyCredential credential = new AzureKeyCredential(searchApiKey);
        SearchClient searchClient = new SearchClient(serviceEndpoint, indexName, credential);
        
        // Set up search options
        SearchOptions options = new SearchOptions()
        {
            IncludeTotalCount = true,
            Filter = "",
            OrderBy = { "search.score() desc" }
        };
        
        // Execute search
        _logger.LogInformation($"Executing search query: {query}");
        SearchResults<SnippetDocument> results = searchClient.Search<SnippetDocument>(query, options);
        
        // Format results for return
        List<SearchResult> searchResults = new List<SearchResult>();
        
        foreach (SearchResult<SnippetDocument> result in results.GetResults())
        {
            searchResults.Add(new SearchResult
            {
                Name = result.Document.Name,
                RelevanceScore = (int)(result.Score * 100), // Convert score to 0-100 scale
                Explanation = $"This snippet matches your search for '{query}'."
            });
        }
        
        return searchResults.OrderByDescending(r => r.RelevanceScore).ToList();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error searching snippets");
        throw;
    }
}

// Classe pour la désérialisation des documents Azure Search
private class SnippetDocument {
    [SimpleField(IsKey = true)]
    public string Name { get; set; }
    
    [SearchableField(IsFilterable = true)]
    public string Content { get; set; }
}

    [Function(nameof(CreateSearchIndex))]
public async Task<HttpResponseData> CreateSearchIndex(
   [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestData req)
{
   _logger.LogInformation("Creating/updating Azure Search index via HTTP trigger");
   
   try {
       // Configuration
       string searchServiceEndpoint = Environment.GetEnvironmentVariable("AzureSearchEndpoint");
       string searchApiKey = Environment.GetEnvironmentVariable("AzureSearchApiKey");
       string indexName = Environment.GetEnvironmentVariable("AzureSearchIndexName") ?? "snippets";
       string storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
       
       if (string.IsNullOrEmpty(searchServiceEndpoint) || string.IsNullOrEmpty(searchApiKey)) {
           _logger.LogError("Azure Search configuration missing");
           var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
           await errorResponse.WriteStringAsync("Configuration Azure Search incomplète");
           return errorResponse;
           
       }
       
       // Clients
       var credential = new AzureKeyCredential(searchApiKey);
       var adminClient = new SearchIndexClient(new Uri(searchServiceEndpoint), credential);
       var indexerClient = new SearchIndexerClient(new Uri(searchServiceEndpoint), credential);

       // 1. Création de l'index s'il n'existe pas
       try {
           await adminClient.GetIndexAsync(indexName);
           _logger.LogInformation($"Index {indexName} déjà existant");
       }
       catch (RequestFailedException ex) when (ex.Status == 404) {
           _logger.LogInformation($"Création de l'index {indexName}");
           
           var fieldBuilder = new FieldBuilder();
           var searchFields = fieldBuilder.Build(typeof(SnippetDocument));
           
           var definition = new SearchIndex(indexName, searchFields);
           await adminClient.CreateOrUpdateIndexAsync(definition);
       }
       
       // 2. Configuration de la source de données
       string dataSourceName = $"{indexName}-blob-datasource";
       var dataSource = new SearchIndexerDataSourceConnection(
           dataSourceName,
           SearchIndexerDataSourceType.AzureBlob,
           storageConnectionString,
           new SearchIndexerDataContainer("snippets"));
           
       await indexerClient.CreateOrUpdateDataSourceConnectionAsync(dataSource);
       
       // 3. Configuration de l'indexeur
       string indexerName = $"{indexName}-indexer";
       var indexer = new SearchIndexer(
           indexerName,
           dataSourceName,
           indexName)
       {
           Schedule = new IndexingSchedule(TimeSpan.FromHours(1))
       };
       
       await indexerClient.CreateOrUpdateIndexerAsync(indexer);
       
       // 4. Exécution immédiate
       await indexerClient.RunIndexerAsync(indexerName);
       
       _logger.LogInformation("Index setup completed successfully");
       
       // Créer une réponse de succès
       var response = req.CreateResponse(HttpStatusCode.OK);
       await response.WriteStringAsync("Index Azure Search créé et configuré avec succès");
       return response;
   }
   catch (Exception ex) {
       _logger.LogError(ex, "Error setting up Azure Search index");
       var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
       await errorResponse.WriteStringAsync($"Erreur lors de la création de l'index: {ex.Message}");
       return errorResponse;
   }
}



    [Function(nameof(EnhanceSnippet))]
    public async Task<string> EnhanceSnippet(
        [McpToolTrigger(EnhanceSnippetToolName, EnhanceSnippetToolDescription)]
            ToolInvocationContext context,
        [McpToolProperty(SnippetNamePropertyName, PropertyType, SnippetNamePropertyDescription)]
            string name,
        [McpToolProperty(EnhanceTypePropertyName, PropertyType, EnhanceTypePropertyDescription)]
            string enhanceType,
        [BlobInput(BlobPath)] string snippetContent
    )
    {
        _logger.LogInformation($"Enhancing snippet {name} using AI. Enhancement type: {enhanceType}");
        
        try
        {
            // Valider la configuration OpenAI
            ValidateOpenAIConfiguration();
            
            // Récupérer la clé API depuis les variables d'environnement
            var apiKey = Environment.GetEnvironmentVariable("OpenAIApiKey");
            var model = Environment.GetEnvironmentVariable("OpenAIModel") ?? "gpt-4";
            
            // Préparer le prompt en fonction du type d'amélioration
            string systemPrompt = "Vous êtes un expert en programmation chargé d'améliorer du code.";
            string userPrompt = enhanceType.ToLowerInvariant() switch
            {
                "optimize" => $"Optimisez ce code pour de meilleures performances et expliquez les améliorations:\n\n```\n{snippetContent}\n```",
                "document" => $"Ajoutez une documentation détaillée à ce code (commentaires, docstrings):\n\n```\n{snippetContent}\n```",
                "refactor" => $"Refactorisez ce code pour améliorer sa lisibilité et sa maintenabilité:\n\n```\n{snippetContent}\n```",
                "explain" => $"Expliquez en détail comment fonctionne ce code, ligne par ligne:\n\n```\n{snippetContent}\n```",
                _ => $"Améliorez ce code avec les meilleures pratiques:\n\n```\n{snippetContent}\n```"
            };
            
            // Appeler OpenAI pour l'amélioration
            string enhancedCode = await CallOpenAI(apiKey, systemPrompt, userPrompt, model);
            
            if (enhancedCode.StartsWith("Erreur:"))
            {
                _logger.LogError($"Error from OpenAI: {enhancedCode}");
                return enhancedCode;
            }
            
            // Nettoyer le code généré (enlever les blocs de code markdown si présents)
            enhancedCode = CleanGeneratedCode(enhancedCode);
            
            _logger.LogInformation($"Successfully enhanced snippet {name}");
            return enhancedCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error enhancing snippet {name}");
            return $"Erreur lors de l'amélioration du snippet: {ex.Message}";
        }
    }

    [Function(nameof(TranslateSnippet))]
    public async Task<string> TranslateSnippet(
        [McpToolTrigger(TranslateSnippetToolName, TranslateSnippetToolDescription)]
            ToolInvocationContext context,
        [McpToolProperty(SnippetNamePropertyName, PropertyType, SnippetNamePropertyDescription)]
            string name,
        [McpToolProperty(TargetLanguagePropertyName, PropertyType, TargetLanguagePropertyDescription)]
            string targetLanguage,
        [BlobInput(BlobPath)] string snippetContent
    )
    {
        _logger.LogInformation($"Translating snippet {name} to {targetLanguage}");
        
        try
        {
            // Valider la configuration OpenAI
            ValidateOpenAIConfiguration();
            
            if (string.IsNullOrEmpty(snippetContent))
            {
                _logger.LogWarning($"Empty snippet content for {name}");
                return "Erreur: Le contenu du snippet est vide.";
            }
            
            var apiKey = Environment.GetEnvironmentVariable("OpenAIApiKey");
            
            // Détecter le langage source
            string sourceLanguage = DetectLanguage(snippetContent);
            _logger.LogInformation($"Detected source language: {sourceLanguage}");
            
            // Préparer le prompt pour la traduction
            string systemPrompt = $"Vous êtes un traducteur expert de code. Traduisez le code de {sourceLanguage} vers {targetLanguage} en conservant sa fonctionnalité et sa lisibilité.";
            string userPrompt = $"Voici le code en {sourceLanguage} à traduire en {targetLanguage}:\n\n```\n{snippetContent}\n```";
            
            _logger.LogInformation("Calling OpenAI API for translation");
            string translatedCode = await CallOpenAI(apiKey, systemPrompt, userPrompt);
            
            if (translatedCode.StartsWith("Erreur:"))
            {
                _logger.LogError($"Error from OpenAI: {translatedCode}");
                return translatedCode;
            }
            
            // Nettoyer le code traduit (enlever les blocs de code markdown si présents)
            translatedCode = CleanGeneratedCode(translatedCode);
            
            _logger.LogInformation($"Successfully translated snippet {name} to {targetLanguage}");
            return translatedCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error translating snippet {name}");
            return $"Erreur lors de la traduction du snippet: {ex.Message}";
        }
    }

    // Classes de support
    private class SnippetInfo
    {
        public string Name { get; set; }
        public string Content { get; set; }
    }

    public class SearchResult
    {
        public string Name { get; set; }
        public int RelevanceScore { get; set; }
        public string Explanation { get; set; }
    }

    // Méthodes utilitaires
    private void ValidateOpenAIConfiguration()
    {
        var apiKey = Environment.GetEnvironmentVariable("OpenAIApiKey");
        var endpoint = Environment.GetEnvironmentVariable("OpenAIEndpoint");
        var useAzureOpenAI = bool.TryParse(Environment.GetEnvironmentVariable("UseAzureOpenAI") ?? "false", out bool result) && result;
        
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogError("OpenAI API key is not configured");
            throw new InvalidOperationException("La clé API OpenAI n'est pas configurée. Veuillez configurer la variable d'environnement 'OpenAIApiKey'.");
        }
        
        if (string.IsNullOrEmpty(endpoint))
        {
            _logger.LogWarning("OpenAI endpoint is not configured, using default endpoint");
        }
        
        if (useAzureOpenAI)
        {
            var deployment = Environment.GetEnvironmentVariable("OpenAIDeployment");
            var apiVersion = Environment.GetEnvironmentVariable("OpenAIApiVersion");
            
            if (string.IsNullOrEmpty(deployment))
            {
                _logger.LogError("Azure OpenAI deployment name is not configured");
                throw new InvalidOperationException("Le nom du déploiement Azure OpenAI n'est pas configuré. Veuillez configurer la variable d'environnement 'OpenAIDeployment'.");
            }
            
            if (string.IsNullOrEmpty(apiVersion))
            {
                _logger.LogWarning("Azure OpenAI API version is not configured, using default version");
            }
            
            _logger.LogInformation($"Using Azure OpenAI with deployment: {deployment}");
        }
        else
        {
            _logger.LogInformation("Using standard OpenAI API");
        }
    }

    private async Task<string> CallOpenAI(string apiKey, string systemPrompt, string userPrompt, string model = "gpt-4")
    {
        bool useAzureOpenAI = bool.TryParse(Environment.GetEnvironmentVariable("UseAzureOpenAI") ?? "false", out bool result) && result;
        
        // Configuration de l'endpoint
        string endpoint;
        
        if (useAzureOpenAI)
        {
            // Configuration Azure OpenAI
            var baseEndpoint = Environment.GetEnvironmentVariable("OpenAIEndpoint")?.TrimEnd('/');
            var deployment = Environment.GetEnvironmentVariable("OpenAIDeployment");
            var apiVersion = Environment.GetEnvironmentVariable("OpenAIApiVersion") ?? "2023-07-01-preview";
            
            if (string.IsNullOrEmpty(baseEndpoint) || string.IsNullOrEmpty(deployment))
            {
                _logger.LogError("Azure OpenAI configuration is incomplete. Check OpenAIEndpoint and OpenAIDeployment settings.");
                throw new InvalidOperationException("Configuration Azure OpenAI incomplète. Vérifiez les paramètres OpenAIEndpoint et OpenAIDeployment.");
            }
            
            endpoint = $"{baseEndpoint}/openai/deployments/{deployment}/chat/completions?api-version={apiVersion}";
            _logger.LogInformation($"Using Azure OpenAI endpoint: {endpoint}");
        }
        else
        {
            // Configuration OpenAI standard
            var baseEndpoint = Environment.GetEnvironmentVariable("OpenAIEndpoint") ?? "https://api.openai.com/v1";
            endpoint = $"{baseEndpoint.TrimEnd('/')}/chat/completions";
            _logger.LogInformation($"Using standard OpenAI endpoint: {endpoint}");
        }
        
        using var httpClient = new HttpClient();
        
        // Configuration des en-têtes selon le service utilisé
        if (useAzureOpenAI)
        {
            httpClient.DefaultRequestHeaders.Add("api-key", apiKey);
        }
        else
        {
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }
        
        // Préparation de la requête
        var requestData = new
        {
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.7,
            max_tokens = 3000
        };
        
        // Pour l'API OpenAI standard, nous devons spécifier le modèle
        object requestDataFinal = useAzureOpenAI 
            ? requestData 
            : new { model, messages = requestData.messages, temperature = requestData.temperature, max_tokens = requestData.max_tokens };
        
        try
        {
            _logger.LogInformation("Sending request to OpenAI API");
            var response = await httpClient.PostAsJsonAsync(endpoint, requestDataFinal);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"OpenAI API error: {response.StatusCode}. Response: {responseContent}");
                return $"Erreur de l'API OpenAI: {response.StatusCode} - {responseContent}";
            }
            
            _logger.LogInformation($"OpenAI raw response (preview): {responseContent.Substring(0, Math.Min(200, responseContent.Length))}...");
            
            var responseObject = JsonSerializer.Deserialize<OpenAIResponse>(responseContent);
            
            if (responseObject == null || responseObject.Choices == null || !responseObject.Choices.Any())
            {
                _logger.LogError($"Invalid or empty response from OpenAI: {responseContent}");
                return "Erreur: Réponse invalide ou vide de l'API";
            }
            
            return responseObject.Choices[0]?.Message?.Content?.Trim() ?? 
                "Erreur: Contenu de la réponse non disponible";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calling OpenAI API at {endpoint}");
            return $"Erreur lors de l'appel à l'API OpenAI: {ex.Message}";
        }
    }

    // Extraction du score de pertinence à partir de la réponse texte
    private int ExtractRelevanceScore(string response, string snippetName)
    {
        try
        {
            var pattern = $"Snippet:\\s*{Regex.Escape(snippetName)}[\\s\\S]*?Relevance Score:\\s*(\\d+)";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            var match = regex.Match(response);
            
            if (match.Success && match.Groups.Count > 1)
            {
                if (int.TryParse(match.Groups[1].Value, out int score))
                {
                    return score;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error extracting relevance score for {snippetName}");
        }
        
        // Valeur par défaut
        return 60;
    }

    // Extraction de l'explication à partir de la réponse texte
    private string ExtractExplanation(string response, string snippetName)
    {
        try
        {
            var pattern = $"Snippet:\\s*{Regex.Escape(snippetName)}[\\s\\S]*?Explanation:\\s*([\\s\\S]*?)(?=\\n\\s*Snippet:|$)";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            var match = regex.Match(response);
            
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value.Trim();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error extracting explanation for {snippetName}");
        }
        
        // Valeur par défaut
        return "Snippet pertinent pour la recherche";
    }

    // Détection du langage de programmation
    private string DetectLanguage(string code)
    {
        // Détection basique par mots-clés et syntaxe
        if (code.Contains("import java.") || code.Contains("public class "))
            return "Java";
        if (code.Contains("using System;") || code.Contains("namespace "))
            return "C#";
        if (code.Contains("import React") || code.Contains("const ") && code.Contains("=>"))
            return "JavaScript";
        if (code.Contains("def ") && code.Contains(":") || code.Contains("import numpy"))
            return "Python";
        if (code.Contains("<?php"))
            return "PHP";
        if (code.Contains("#include <"))
            return "C++";
        if (code.Contains("func ") && code.Contains("package main"))
            return "Go";
        if (code.Contains("module ") && code.Contains("do\n") && code.Contains("end"))
            return "Ruby";
        
        // Par défaut
        return "unknown";
    }

    // Nettoyage du code généré par OpenAI
    private string CleanGeneratedCode(string generatedCode)
    {
        // Enlever les marqueurs de bloc de code Markdown
        var codePattern = "```(?:[a-zA-Z]*)?\\s*(.*?)\\s*```";
        var match = Regex.Match(generatedCode, codePattern, RegexOptions.Singleline);
        
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }
        
        // Si pas de marqueurs, retourner tel quel
        return generatedCode;
    }

    // Classes pour désérialiser la réponse d'OpenAI
    private class OpenAIResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice> Choices { get; set; } = new();
    }

    private class Choice
    {
        [JsonPropertyName("message")]
        public Message Message { get; set; } = new();
    }

    private class Message
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}