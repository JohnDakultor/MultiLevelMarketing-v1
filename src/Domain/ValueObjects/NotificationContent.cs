using System;
using System.Text.RegularExpressions;
using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.ValueObjects;

/// <summary>
/// An immutable, persistence-agnostic value object representing sanitized notification messaging content.
/// </summary>
public sealed record NotificationContent
{
    private const int MaxTitleLength = 150;
    private const int MaxBodyLength = 4000;
    private const int MaxPathLength = 2048;

    private static readonly Regex ScriptMarkupRegex = new(
        @"<[^>]*script.*?>|on\w+\s*=",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private static readonly Regex ControlCharactersRegex = new(
        @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]",
        RegexOptions.Compiled
    );

    public NotificationContent(string title, string plainTextBody, string? actionPath = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainInvariantException(
                "Notification title is required and cannot be empty."
            );

        if (string.IsNullOrWhiteSpace(plainTextBody))
            throw new DomainInvariantException("Notification plain text body is required.");

        if (title.Length > MaxTitleLength)
            throw new DomainInvariantException(
                $"Title length cannot exceed {MaxTitleLength} characters."
            );

        if (plainTextBody.Length > MaxBodyLength)
            throw new DomainInvariantException(
                $"Text body length cannot exceed {MaxBodyLength} characters."
            );

        ValidateSanitization(title, "Title");
        ValidateSanitization(plainTextBody, "Body");

        string? sanitizedPath = null;
        if (!string.IsNullOrWhiteSpace(actionPath))
        {
            var trimmedPath = actionPath.Trim();

            if (trimmedPath.Length > MaxPathLength)
                throw new DomainInvariantException(
                    $"Action path length cannot exceed {MaxPathLength} characters."
                );

            ValidateSanitization(trimmedPath, "ActionPath");

            bool isRelativePath = trimmedPath.StartsWith('/') && !trimmedPath.StartsWith("//");
            if (!isRelativePath || Uri.TryCreate(trimmedPath, UriKind.Absolute, out _))
            {
                throw new DomainInvariantException(
                    "Action path must be a valid, trusted relative application path starting with a single '/' forwarding slash."
                );
            }

            sanitizedPath = trimmedPath;
        }

        Title = title.Trim();
        PlainTextBody = plainTextBody.Trim();
        ActionPath = sanitizedPath;
    }

    public string Title { get; init; }
    public string PlainTextBody { get; init; }
    public string? ActionPath { get; init; }

    private static void ValidateSanitization(string input, string fieldName)
    {
        if (ControlCharactersRegex.IsMatch(input))
        {
            throw new DomainInvariantException(
                $"The notification {fieldName} contains illegal control or hidden unprintable characters."
            );
        }

        if (ScriptMarkupRegex.IsMatch(input))
        {
            throw new DomainInvariantException(
                $"The notification {fieldName} was rejected due to suspicious script or markup injection fragments."
            );
        }
    }
}
