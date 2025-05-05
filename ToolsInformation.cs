namespace FunctionsSnippetTool;

public static class ToolsInformation
{
    // Constantes pour GetSnippet
    public const string GetSnippetToolName = "getsnippet";
    public const string GetSnippetToolDescription = "Récupère un snippet de code par son nom.";

    // Constantes pour SaveSnippet
    public const string SaveSnippetToolName = "savesnippet";
    public const string SaveSnippetToolDescription = "Sauvegarde un snippet de code avec le nom spécifié.";

    // Constantes pour ListSnippets
    public const string ListSnippetsToolName = "listsnippets";
    public const string ListSnippetsToolDescription = "Liste tous les snippets disponibles.";

    // Constantes pour SearchSnippets
    public const string SearchSnippetsToolName = "searchsnippets";
    public const string SearchSnippetsToolDescription = "Recherche intelligente de snippets à partir d'une requête textuelle.";
    public const string SearchQueryPropertyName = "query";
    public const string SearchQueryPropertyDescription = "Requête de recherche (ex: 'authentification JWT', 'parsing JSON', etc.)";

    // Constantes pour EnhanceSnippet
    public const string EnhanceSnippetToolName = "enhancesnippet";
    public const string EnhanceSnippetToolDescription = "Améliore un snippet de code en utilisant l'IA. Types d'amélioration: optimize, document, refactor, explain.";
    public const string EnhanceTypePropertyName = "enhanceType";
    public const string EnhanceTypePropertyDescription = "Type d'amélioration: optimize (optimiser les performances), document (ajouter documentation), refactor (refactoriser), explain (expliquer le code).";

    // Constantes pour TranslateSnippet
    public const string TranslateSnippetToolName = "translatesnippet";
    public const string TranslateSnippetToolDescription = "Traduit un snippet d'un langage de programmation à un autre.";
    public const string TargetLanguagePropertyName = "targetLanguage";
    public const string TargetLanguagePropertyDescription = "Langage cible pour la traduction (ex: 'python', 'javascript', 'csharp', etc.)";

    // Constantes pour GenerateSnippet (prêt pour implémentation future)
    public const string GenerateSnippetToolName = "generatesnippet";
    public const string GenerateSnippetToolDescription = "Génère un snippet de code à partir d'une description textuelle.";
    public const string DescriptionPropertyName = "description";
    public const string DescriptionPropertyDescription = "Description de ce que le snippet doit faire.";
    public const string LanguagePropertyName = "language";
    public const string LanguagePropertyDescription = "Langage de programmation pour le snippet généré.";

    // Constantes pour ValidateSnippet (prêt pour implémentation future)
    public const string ValidateSnippetToolName = "validatesnippet";
    public const string ValidateSnippetToolDescription = "Valide un snippet de code, identifie les problèmes potentiels et propose des corrections.";

    // Constantes pour CategorizeSnippets (prêt pour implémentation future)
    public const string CategorizeSnippetsToolName = "categorizesnippets";
    public const string CategorizeSnippetsToolDescription = "Organise tous les snippets en catégories logiques basées sur leur contenu et leur fonction.";

    // Constantes pour GenerateTestsForSnippet (prêt pour implémentation future)
    public const string GenerateTestsToolName = "generatetests";
    public const string GenerateTestsToolDescription = "Génère des tests unitaires pour un snippet de code.";
    public const string TestFrameworkPropertyName = "testFramework";
    public const string TestFrameworkPropertyDescription = "Framework de test à utiliser (ex: 'jest', 'pytest', 'nunit', etc.)";

    // Constantes pour VersionSnippet (prêt pour implémentation future)
    public const string VersionSnippetToolName = "versionsnippet";
    public const string VersionSnippetToolDescription = "Crée une nouvelle version d'un snippet avec un message de commit.";
    public const string VersionPropertyName = "version";
    public const string VersionPropertyDescription = "Numéro ou identifiant de version (ex: '1.0', '2.3', etc.)";
    public const string CommitMessagePropertyName = "commitMessage";
    public const string CommitMessagePropertyDescription = "Message décrivant les modifications apportées dans cette version.";

    // Constantes partagées
    public const string SnippetNamePropertyName = "name";
    public const string SnippetNamePropertyDescription = "Le nom du snippet.";
    public const string SnippetPropertyName = "snippet";
    public const string SnippetPropertyDescription = "Le contenu du snippet.";
    public const string PropertyType = "string";
    // Constantes pour HelloTool
public const string HelloToolName = "hello";
public const string HelloToolDescription = "Un outil simple qui retourne un message de salutation.";
}