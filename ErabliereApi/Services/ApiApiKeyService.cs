﻿using ErabliereApi.Authorization;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Linq.Expressions;
using System.Security.Cryptography;

namespace ErabliereApi.Services;

/// <summary>
/// Implémentation de <see cref="IApiKeyService" /> gérant les clé avec la logique interne 
/// au projet ErabliereApi
/// </summary>
public class ApiApiKeyService : IApiKeyService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEmailService _emailService;
    private readonly EmailConfig _emailConfig;
    private readonly ILogger<ApiApiKeyService> _logger;

    /// <summary>
    /// Constructeur par initlaisation
    /// </summary>
    /// <param name="scopeFactory"></param>
    /// <param name="emailService"></param>
    /// <param name="emailConfig"></param>
    /// <param name="logger"></param>
    public ApiApiKeyService(
        IServiceScopeFactory scopeFactory, 
        IEmailService emailService, 
        IOptions<EmailConfig> emailConfig,
        ILogger<ApiApiKeyService> logger)
    {
        _scopeFactory = scopeFactory;
        _emailService = emailService;
        _emailConfig = emailConfig.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(ApiKey, string)> CreateApiKeyAsync(CreateApiKeyParameters param, CancellationToken token)
    {
        if (param.Customer == null)
        {
            throw new InvalidOperationException("A customer instance is required");
        }

        if (!param.Customer.Id.HasValue)
        {
            throw new InvalidOperationException("customer is supposed to have Id");
        }

        var apiKeyBytes = Guid.NewGuid().ToByteArray();

        var apiKeyObj = new ApiKey
        {
            Key = HashApiKey(apiKeyBytes),
            CustomerId = param.Customer.Id.Value,
            CreationTime = DateTimeOffset.Now,
            Name = param.Name ?? ""
        };

        var originalKey = Convert.ToBase64String(apiKeyBytes);

        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ErabliereDbContext>();

            await context.ApiKeys.AddAsync(apiKeyObj, token);

            await context.SaveChangesAsync(token);
        }        

        await SendEmailAsync(param.Customer.Email, originalKey, token);

        return (apiKeyObj, originalKey);
    }

    private async Task SendEmailAsync(string email, string originalKey, CancellationToken token)
    {
        if (_emailConfig.IsConfigured)
        {
            var mailMessage = new MimeMessage();
            mailMessage.From.Add(new MailboxAddress("ErabliereAPI - Your API Key", _emailConfig.Sender));
            mailMessage.To.Add(MailboxAddress.Parse(email));
            mailMessage.Subject = $"Your API Key for ErabliereAPI";
            mailMessage.Body = new TextPart("plain")
            {
                Text = $"Your api key is: {originalKey}\n" +
                       $"Use it in the header '{ApiKeyMiddleware.XApiKeyHeader}' for your API requests.\n\n" +
                       $"Please keep it safe and do not share it with anyone.\n" +
                       $"If you believe this key has been compromised, please revoke it immediately."
            };

            await _emailService.SendEmailAsync(mailMessage, token);
        }
    }

    /// <inheritdoc />
    public string HashApiKey(byte[] key)
    {
        using var sha = SHA256.Create();

        var hash = sha.ComputeHash(key);

        var hashedContent = Convert.ToBase64String(hash);

        return hashedContent;
    }

    /// <inheritdoc />
    public string HashApiKey(string key)
    {
        return HashApiKey(Convert.FromBase64String(key));
    }

    /// <inheritdoc />
    public async Task SetSubscriptionKeyAsync(
        Donnees.Customer customer, string id, CancellationToken token)
    {
        if (customer == null)
        {
            var message = "customer was null inside the SetSubscriptionKeyAsync method";
            _logger.LogCritical(message);
            throw new InvalidOperationException(message);
        }

        var now = DateTimeOffset.Now;

        var apiKey = await TryGetApiKeyAsync(a =>
                            a.CustomerId == customer.Id &&
                            a.CreationTime <= now &&
                            a.RevocationTime == null &&
                            a.DeletionTime == null, token);

        if (apiKey == null)
        {
            throw new InvalidOperationException("apiKey is required to continue the process");
        }

        apiKey.SubscriptionId = id;

        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ErabliereDbContext>();

            context.Update(apiKey);

            await context.SaveChangesAsync(token);
        }
    }

    private async Task<ApiKey?> TryGetApiKeyAsync(Expression<Func<ApiKey, bool>> predicat, CancellationToken token)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ErabliereDbContext>();

            var apikey = await context.ApiKeys
                .Where(predicat).OrderByDescending(a => a.CreationTime)
                .FirstAsync(token);

            return apikey;
        }
        catch (InvalidOperationException e)
        {
            _logger.LogWarning(e, "Customer was not found in the database");
        }

        return null;
    }

    /// <inheritdoc />
    public bool TryHashApiKey(string key, out string? hashApiKey)
    {
        try
        {
            hashApiKey = HashApiKey(key);

            return true;
        }
        catch { 
            // Do nothing
        }

        hashApiKey = null;

        return false;
    }
}
