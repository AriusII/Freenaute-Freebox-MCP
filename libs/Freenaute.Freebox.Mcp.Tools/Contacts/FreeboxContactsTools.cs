namespace Freenaute.Freebox.Mcp.Tools.Contacts;

[McpServerToolType]
public sealed class FreeboxContactsTools(Func<CancellationToken, Task<ContactEntry[]>> getContacts)
{
    private const int DefaultLimit = 25;
    private const int MaxContacts = 64;
    private const int MaxQueryLength = 128;

    private readonly Func<CancellationToken, Task<ContactEntry[]>> _getContacts =
        getContacts ?? throw new ArgumentNullException(nameof(getContacts));

    public FreeboxContactsTools(IFreeboxClient client)
        : this(cancellationToken => client.Services.Contacts.GetAllAsync(cancellationToken))
    {
        ArgumentNullException.ThrowIfNull(client);
    }

    [McpServerTool(Name = "freebox_contacts_list", Title = "List Freebox contacts", ReadOnly = true, Idempotent = true,
        Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(ContactsToolResult))]
    [Description(
        "Returns a bounded list of contacts with safe summary fields only. Optional query filtering is supported.")]
    public Task<ContactsToolResult> ListAsync(
        [Description("Optional case-insensitive filter applied to display, first, last, or company name.")]
        string? query = null,
        [Description("Maximum number of contacts to return (1-64). Defaults to 25.")]
        int limit = DefaultLimit,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(query, limit, false, cancellationToken);
    }

    [McpServerTool(Name = "freebox_contacts_search", Title = "Search Freebox contacts", ReadOnly = true,
        Idempotent = true, Destructive = false, OpenWorld = true, UseStructuredContent = true,
        OutputSchemaType = typeof(ContactsToolResult))]
    [Description("Searches contacts by display, first, last, or company name and returns bounded safe summaries.")]
    public Task<ContactsToolResult> SearchAsync(
        [Description("Case-insensitive search query (1-128 chars, no control characters).")]
        string query,
        [Description("Maximum number of contacts to return (1-64). Defaults to 25.")]
        int limit = DefaultLimit,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(query, limit, true, cancellationToken);
    }

    private async Task<ContactsToolResult> ExecuteAsync(
        string? query,
        int limit,
        bool requireQuery,
        CancellationToken cancellationToken)
    {
        string? normalizedQuery = ValidateAndNormalizeQuery(query, requireQuery);
        int boundedLimit = ValidateLimit(limit);

        try
        {
            ContactEntry[] contacts = await _getContacts(cancellationToken).ConfigureAwait(false);

            ContactEntry[] filtered = [.. contacts.Where(item => MatchesQuery(item, normalizedQuery))];
            ContactSummaryToolResult[] mapped =
            [
                .. filtered
                    .Take(boundedLimit)
                    .Select(MapContact)
            ];

            return new ContactsToolResult(normalizedQuery, filtered.Length, mapped);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return ToolResultFactory.Failure<ContactsToolResult>(FreeboxErrorMapper.Map(exception));
        }
    }

    private static int ValidateLimit(int limit)
    {
        if (limit is < 1 or > MaxContacts)
        {
            ThrowInvalidRequest("limit must be between 1 and 64.");
        }

        return limit;
    }

    private static string? ValidateAndNormalizeQuery(string? query, bool requireQuery)
    {
        if (query is null)
        {
            if (requireQuery)
            {
                ThrowInvalidRequest("query is required.");
            }

            return null;
        }

        string normalized = query.Trim();
        if (normalized.Length == 0)
        {
            if (requireQuery)
            {
                ThrowInvalidRequest("query cannot be blank.");
            }

            return null;
        }

        if (normalized.Length > MaxQueryLength || normalized.Any(char.IsControl))
        {
            ThrowInvalidRequest("query must be 1 to 128 characters and contain no control characters.");
        }

        return normalized;
    }

    private static ContactSummaryToolResult MapContact(ContactEntry contact)
    {
        return new ContactSummaryToolResult(
            Safe.StableIdentifier(contact.Id?.ToString(CultureInfo.InvariantCulture)),
            ResolveDisplayName(contact),
            Safe.Text(contact.Company, 128),
            contact.LastUpdate,
            contact.Numbers?.Length ?? 0,
            contact.Emails?.Length ?? 0,
            contact.Addresses?.Length ?? 0,
            contact.Urls?.Length ?? 0);
    }

    private static string ResolveDisplayName(ContactEntry contact)
    {
        string[] candidates =
        [
            contact.DisplayName ?? string.Empty,
            $"{contact.FirstName} {contact.LastName}".Trim(),
            contact.Company ?? string.Empty
        ];

        foreach (string candidate in candidates)
        {
            string value = Safe.Text(candidate, 128);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return "unknown";
    }

    private static bool MatchesQuery(ContactEntry item, string? query)
    {
        if (query is null)
        {
            return true;
        }

        return Contains(item.DisplayName, query) ||
               Contains(item.FirstName, query) ||
               Contains(item.LastName, query) ||
               Contains(item.Company, query);
    }

    private static bool Contains(string? value, string query)
    {
        return value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static void ThrowInvalidRequest(string message)
    {
        throw new InvalidOperationException($"{FreeboxCoreErrorCode.InvalidRequest}: {message}");
    }
}
