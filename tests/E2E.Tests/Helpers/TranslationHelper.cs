using System.Text.Json;

namespace E2E.Tests.Helpers;

/// <summary>
/// Reads the frontend i18n translations file so E2E tests can use the same
/// string values as the UI — no more hardcoded English literals in selectors.
/// Source of truth: src/frontend/src/i18n/locales/en.json
/// </summary>
public static class T
{
    private static readonly Lazy<JsonDocument> _doc = new(LoadDocument);

    public static class Auth
    {
        public static string Title => Get("auth.title");
        public static string UsernameLabel => Get("auth.usernameLabel");
        public static string PasswordLabel => Get("auth.passwordLabel");
        public static string Submit => Get("auth.submit");
        public static string Submitting => Get("auth.submitting");
        public static string InvalidCredentials => Get("auth.invalidCredentials");
    }

    public static class ContentType
    {
        public static class Create
        {
            public static string Title => Get("contentType.create.title");
            public static string Description => Get("contentType.create.description");
            public static string NameLabel => Get("contentType.create.nameLabel");
            public static string NamePlaceholder => Get("contentType.create.namePlaceholder");
            public static string NameHint => Get("contentType.create.nameHint");
            public static string FieldsLabel => Get("contentType.create.fieldsLabel");
            public static string AddField => Get("contentType.create.addField");
            public static string Submit => Get("contentType.create.submit");
            public static string Submitting => Get("contentType.create.submitting");
            public static string Reset => Get("contentType.create.reset");
            public static string ErrorRetry => Get("contentType.create.errorRetry");

            public static class Field
            {
                public static string NamePlaceholder =>
                    Get("contentType.create.field.namePlaceholder");
                public static string NameLabel => Get("contentType.create.field.nameLabel");
                public static string TypeLabel => Get("contentType.create.field.typeLabel");
                public static string TypePlaceholder =>
                    Get("contentType.create.field.typePlaceholder");
                public static string IsRequiredLabel =>
                    Get("contentType.create.field.isRequiredLabel");
                public static string RemoveField => Get("contentType.create.field.removeField");
            }
        }
    }

    private static string Get(string dotPath)
    {
        string[] parts = dotPath.Split('.');
        JsonElement current = _doc.Value.RootElement;

        foreach (string part in parts)
        {
            if (!current.TryGetProperty(part, out JsonElement next))
            {
                throw new KeyNotFoundException(
                    $"Translation key '{dotPath}' not found (failed at '{part}')."
                );
            }

            current = next;
        }

        return current.GetString()
            ?? throw new InvalidOperationException($"Translation key '{dotPath}' is not a string.");
    }

    private static JsonDocument LoadDocument()
    {
        string path = FindTranslationsFile();
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static string FindTranslationsFile()
    {
        const string relativePath = "src/frontend/src/i18n/locales/en.json";

        string? dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            string candidate = Path.Combine(dir, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new FileNotFoundException(
            $"Could not find '{relativePath}' by walking up from '{AppContext.BaseDirectory}'."
        );
    }
}
