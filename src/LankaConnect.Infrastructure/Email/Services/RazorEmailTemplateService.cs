using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Infrastructure.Email.Configuration;
using LankaConnect.Domain.Common;

namespace LankaConnect.Infrastructure.Email.Services;

public class RazorEmailTemplateService : IEmailTemplateService
{
    private readonly EmailSettings _emailSettings;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RazorEmailTemplateService> _logger;
    private readonly string _templateBasePath;

    // Phase 6A.34: Changed from static to instance-level to prevent caching issues across deployments
    // Static cache persisted "template not found" even after templates were deployed
    private readonly ConcurrentDictionary<string, bool> _templateExistsCache = new();

    public RazorEmailTemplateService(
        IOptions<EmailSettings> emailSettings,
        IMemoryCache cache,
        ILogger<RazorEmailTemplateService> logger)
    {
        _emailSettings = emailSettings.Value;
        _cache = cache;
        _logger = logger;
        _templateBasePath = Path.Combine(Directory.GetCurrentDirectory(), _emailSettings.TemplateBasePath);

        // Try to ensure template directory exists (will fail in read-only containers, but that's okay)
        // Templates should be included in the Docker image or read from a writable volume
        try
        {
            if (!Directory.Exists(_templateBasePath))
            {
                Directory.CreateDirectory(_templateBasePath);
                _logger.LogInformation("Created template directory: {Path}", _templateBasePath);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Cannot create template directory {Path} - templates must be included in container image or mounted volume", _templateBasePath);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Cannot create template directory {Path} - templates must be included in container image or mounted volume", _templateBasePath);
        }
    }

    public async Task<Result<RenderedEmailTemplate>> RenderTemplateAsync(
        string templateName, 
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await TemplateExistsAsync(templateName, cancellationToken))
            {
                return Result<RenderedEmailTemplate>.Failure($"Email template '{templateName}' not found");
            }

            var cacheKey = $"template_{templateName}_{GetDataHash(parameters)}";
            
            if (_emailSettings.CacheTemplates && _cache.TryGetValue(cacheKey, out var cachedResult))
            {
                _logger.LogDebug("Using cached template result for {TemplateName}", templateName);
                return Result<RenderedEmailTemplate>.Success((RenderedEmailTemplate)cachedResult!);
            }

            var result = await RenderTemplateInternalAsync(templateName, parameters, cancellationToken);

            var renderedTemplate = new RenderedEmailTemplate
            {
                Subject = result.subject,
                HtmlBody = result.htmlBody,
                PlainTextBody = result.textBody
            };

            if (_emailSettings.CacheTemplates)
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_emailSettings.TemplateCacheExpiryInMinutes),
                    SlidingExpiration = TimeSpan.FromMinutes(_emailSettings.TemplateCacheExpiryInMinutes / 2)
                };
                
                _cache.Set(cacheKey, renderedTemplate, cacheOptions);
            }

            _logger.LogInformation("Template {TemplateName} rendered successfully", templateName);
            return Result<RenderedEmailTemplate>.Success(renderedTemplate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render template {TemplateName}", templateName);
            return Result<RenderedEmailTemplate>.Failure($"Failed to render template: {ex.Message}");
        }
    }

    public async Task<Result<List<EmailTemplateInfo>>> GetAvailableTemplatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var templates = await GetAvailableTemplateNamesAsync(cancellationToken);
            var templateInfos = new List<EmailTemplateInfo>();

            foreach (var templateName in templates)
            {
                var info = new EmailTemplateInfo
                {
                    Name = templateName,
                    DisplayName = templateName.Replace("-", " ").Replace("_", " "),
                    Description = $"Template: {templateName}",
                    RequiredParameters = new List<string>(),
                    OptionalParameters = new List<string>(),
                    Category = "General",
                    IsActive = true,
                    LastModified = DateTime.UtcNow
                };
                templateInfos.Add(info);
            }

            return Result<List<EmailTemplateInfo>>.Success(templateInfos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available templates");
            return Result<List<EmailTemplateInfo>>.Failure($"Failed to get available templates: {ex.Message}");
        }
    }

    public async Task<Result<EmailTemplateInfo>> GetTemplateInfoAsync(string templateName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await TemplateExistsAsync(templateName, cancellationToken))
            {
                return Result<EmailTemplateInfo>.Failure($"Template '{templateName}' not found");
            }

            var info = new EmailTemplateInfo
            {
                Name = templateName,
                DisplayName = templateName.Replace("-", " ").Replace("_", " "),
                Description = $"Template: {templateName}",
                RequiredParameters = new List<string>(),
                OptionalParameters = new List<string>(),
                Category = "General",
                IsActive = true,
                LastModified = DateTime.UtcNow
            };

            return Result<EmailTemplateInfo>.Success(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get template info for {TemplateName}", templateName);
            return Result<EmailTemplateInfo>.Failure($"Failed to get template info: {ex.Message}");
        }
    }

    public async Task<Result> ValidateTemplateParametersAsync(string templateName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await TemplateExistsAsync(templateName, cancellationToken))
            {
                return Result.Failure($"Template '{templateName}' not found");
            }

            // Basic validation - template exists and parameters is not null
            if (parameters == null)
            {
                return Result.Failure("Parameters cannot be null");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate template parameters for {TemplateName}", templateName);
            return Result.Failure($"Failed to validate template parameters: {ex.Message}");
        }
    }

    public async Task<bool> TemplateExistsAsync(string templateName, CancellationToken cancellationToken = default)
    {
        if (_templateExistsCache.TryGetValue(templateName, out var exists))
        {
            _logger.LogInformation("[Phase 6A.35] Template '{TemplateName}' found in cache: {Exists}", templateName, exists);
            return exists;
        }

        var templatePath = GetTemplatePath(templateName);
        var subjectPath = GetSubjectPath(templateName);
        var textPath = GetTextBodyPath(templateName);
        var htmlPath = GetHtmlBodyPath(templateName);

        _logger.LogInformation("[Phase 6A.35] Checking template paths for '{TemplateName}':", templateName);
        _logger.LogInformation("[Phase 6A.35]   Base path: {BasePath}", _templateBasePath);
        _logger.LogInformation("[Phase 6A.35]   Template: {Path} (exists: {Exists})", templatePath, File.Exists(templatePath));
        _logger.LogInformation("[Phase 6A.35]   Subject: {Path} (exists: {Exists})", subjectPath, File.Exists(subjectPath));
        _logger.LogInformation("[Phase 6A.35]   Text: {Path} (exists: {Exists})", textPath, File.Exists(textPath));
        _logger.LogInformation("[Phase 6A.35]   HTML: {Path} (exists: {Exists})", htmlPath, File.Exists(htmlPath));

        // At minimum, we need either the main template file or separate files
        var templateExists = File.Exists(templatePath) ||
                           (File.Exists(subjectPath) && (File.Exists(textPath) || File.Exists(htmlPath)));

        _logger.LogInformation("[Phase 6A.35] Template '{TemplateName}' exists: {Exists}", templateName, templateExists);
        _templateExistsCache.TryAdd(templateName, templateExists);
        return await Task.FromResult(templateExists);
    }

    private async Task<IEnumerable<string>> GetAvailableTemplateNamesAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_templateBasePath))
        {
            return await Task.FromResult(Enumerable.Empty<string>());
        }

        var templates = new HashSet<string>();

        // Get templates from .cshtml files
        var cshtmlFiles = Directory.GetFiles(_templateBasePath, "*.cshtml", SearchOption.AllDirectories);
        foreach (var file in cshtmlFiles)
        {
            var relativePath = Path.GetRelativePath(_templateBasePath, file);
            var templateName = Path.GetFileNameWithoutExtension(relativePath);
            
            // Skip partial templates (starting with _)
            if (!templateName.StartsWith("_"))
            {
                templates.Add(templateName);
            }
        }

        // Get templates from separate subject/body files
        var subjectFiles = Directory.GetFiles(_templateBasePath, "*-subject.txt", SearchOption.AllDirectories);
        foreach (var file in subjectFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var templateName = fileName.Replace("-subject", "");
            templates.Add(templateName);
        }

        return await Task.FromResult(templates.OrderBy(t => t));
    }

    public async Task PrecompileTemplatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var templates = await GetAvailableTemplateNamesAsync(cancellationToken);
            var sampleData = new Dictionary<string, object>
            {
                ["Name"] = "Sample User",
                ["Email"] = "sample@example.com",
                ["Date"] = DateTime.Now,
                ["Url"] = "https://example.com"
            };

            foreach (var templateName in templates)
            {
                try
                {
                    await RenderTemplateInternalAsync(templateName, sampleData, cancellationToken);
                    _logger.LogDebug("Precompiled template {TemplateName}", templateName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to precompile template {TemplateName}", templateName);
                }
            }

            _logger.LogInformation("Template precompilation completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during template precompilation");
            throw;
        }
    }

    private async Task<(string subject, string textBody, string htmlBody)> RenderTemplateInternalAsync(
        string templateName, 
        Dictionary<string, object> data, 
        CancellationToken cancellationToken)
    {
        var templatePath = GetTemplatePath(templateName);
        var subjectPath = GetSubjectPath(templateName);
        var textPath = GetTextBodyPath(templateName);
        var htmlPath = GetHtmlBodyPath(templateName);

        string subject = string.Empty;
        string textBody = string.Empty;
        string htmlBody = string.Empty;

        // Try to load separate template files first
        if (File.Exists(subjectPath))
        {
            subject = await RenderTemplateFileAsync(subjectPath, data, cancellationToken);
        }

        if (File.Exists(textPath))
        {
            textBody = await RenderTemplateFileAsync(textPath, data, cancellationToken);
        }

        if (File.Exists(htmlPath))
        {
            htmlBody = await RenderTemplateFileAsync(htmlPath, data, cancellationToken);
        }

        // If we have a main template file, use it to fill in missing parts
        if (File.Exists(templatePath))
        {
            var mainTemplate = await RenderTemplateFileAsync(templatePath, data, cancellationToken);
            
            // Parse the main template (simple format with sections)
            var (parsedSubject, parsedTextBody, parsedHtmlBody) = ParseMainTemplate(mainTemplate);
            
            subject = !string.IsNullOrEmpty(subject) ? subject : parsedSubject;
            textBody = !string.IsNullOrEmpty(textBody) ? textBody : parsedTextBody;
            htmlBody = !string.IsNullOrEmpty(htmlBody) ? htmlBody : parsedHtmlBody;
        }

        // Validate that we have at least subject and one body type
        if (string.IsNullOrEmpty(subject))
        {
            throw new InvalidOperationException($"No subject found for template {templateName}");
        }

        if (string.IsNullOrEmpty(textBody) && string.IsNullOrEmpty(htmlBody))
        {
            throw new InvalidOperationException($"No body content found for template {templateName}");
        }

        return (subject.Trim(), textBody.Trim(), htmlBody.Trim());
    }

    private async Task<string> RenderTemplateFileAsync(string filePath, Dictionary<string, object> data, CancellationToken cancellationToken)
    {
        var template = await File.ReadAllTextAsync(filePath, cancellationToken);
        return RenderTemplate(template, data);
    }

    private static string RenderTemplate(string template, Dictionary<string, object> data)
    {
        var result = new StringBuilder(template);

        // Simple template variable replacement ({{variable}})
        foreach (var kvp in data)
        {
            var placeholder = $"{{{{{kvp.Key}}}}}";
            result.Replace(placeholder, kvp.Value?.ToString() ?? string.Empty);
        }

        return result.ToString();
    }

    private static (string subject, string textBody, string htmlBody) ParseMainTemplate(string template)
    {
        var lines = template.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var subject = string.Empty;
        var textBody = new StringBuilder();
        var htmlBody = new StringBuilder();
        
        var currentSection = "none";
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            if (trimmedLine.StartsWith("##SUBJECT##", StringComparison.OrdinalIgnoreCase))
            {
                currentSection = "subject";
                continue;
            }
            
            if (trimmedLine.StartsWith("##TEXTBODY##", StringComparison.OrdinalIgnoreCase))
            {
                currentSection = "textbody";
                continue;
            }
            
            if (trimmedLine.StartsWith("##HTMLBODY##", StringComparison.OrdinalIgnoreCase))
            {
                currentSection = "htmlbody";
                continue;
            }
            
            switch (currentSection)
            {
                case "subject":
                    if (string.IsNullOrEmpty(subject))
                        subject = trimmedLine;
                    break;
                case "textbody":
                    textBody.AppendLine(line);
                    break;
                case "htmlbody":
                    htmlBody.AppendLine(line);
                    break;
            }
        }
        
        return (subject, textBody.ToString(), htmlBody.ToString());
    }

    private string GetTemplatePath(string templateName)
    {
        return Path.Combine(_templateBasePath, $"{templateName}.cshtml");
    }

    private string GetSubjectPath(string templateName)
    {
        return Path.Combine(_templateBasePath, $"{templateName}-subject.txt");
    }

    private string GetTextBodyPath(string templateName)
    {
        return Path.Combine(_templateBasePath, $"{templateName}-text.txt");
    }

    private string GetHtmlBodyPath(string templateName)
    {
        return Path.Combine(_templateBasePath, $"{templateName}-html.html");
    }

    private static string GetDataHash(Dictionary<string, object> data)
    {
        // Simple hash for cache key - in production you might want a more sophisticated approach
        var combined = string.Join(",", data.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}:{kvp.Value}"));
        return combined.GetHashCode().ToString();
    }
}