// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentGuard.Engine.Abstractions;

namespace AgentGuard.Engine;

/// <summary>
/// Serializes grant tokens to and from disk and produces the canonical bytes that are signed and verified. The
/// canonical bytes are built by hand, not from the JSON, so signing and verifying never disagree over key order
/// or whitespace.
/// </summary>
public static class GrantTokenCodec
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Returns the exact bytes a token's signature covers.
    /// </summary>
    /// <param name="payload">The payload to canonicalize.</param>
    /// <returns>The canonical bytes.</returns>
    public static byte[] CanonicalBytes(GrantTokenPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        var builder = new StringBuilder();
        builder.Append(payload.Id).Append('\n');
        builder.Append(payload.Scope.ToString()).Append('\n');
        builder.Append(payload.ExpiresAt.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture)).Append('\n');
        foreach (string coveredPath in payload.CoveredPaths)
        {
            builder.Append("path:").Append(coveredPath).Append('\n');
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    /// <summary>
    /// Serializes a token to its on-disk JSON form.
    /// </summary>
    /// <param name="token">The token to serialize.</param>
    /// <returns>The JSON text.</returns>
    public static string Serialize(GrantToken token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return JsonSerializer.Serialize(token, Options);
    }

    /// <summary>
    /// Deserializes a token from its on-disk JSON form, rejecting a token with any missing required field so a
    /// malformed token surfaces as a <see cref="JsonException"/> the caller drops rather than a later null
    /// dereference.
    /// </summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The token.</returns>
    /// <exception cref="JsonException">Thrown when the text is not a well-formed, complete token.</exception>
    public static GrantToken Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);
        TokenDto? dto = JsonSerializer.Deserialize<TokenDto>(json, Options);
        if (dto?.Payload is null
            || dto.Signature is null
            || dto.Payload.Id is null
            || dto.Payload.CoveredPaths is null)
        {
            throw new JsonException("Grant token is missing one or more required fields.");
        }

        var payload = new GrantTokenPayload(
            dto.Payload.Id,
            dto.Payload.Scope,
            dto.Payload.CoveredPaths,
            dto.Payload.ExpiresAt);
        return new GrantToken(payload, dto.Signature);
    }

    private sealed record TokenDto(PayloadDto? Payload, string? Signature);

    private sealed record PayloadDto(
        string? Id,
        GrantScope Scope,
        IReadOnlyList<string>? CoveredPaths,
        DateTimeOffset ExpiresAt);
}
