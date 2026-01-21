# Internationalization (i18n) Support

Majorsilence Reporting includes internationalization support for multiple languages through .NET resource files (.resx).

## Supported Languages

The following languages are currently supported:

- **English (en-US)** - Default/Base language
- **Russian (ru-RU)** - Full translation
- **French (fr)** - Partial translation
- **Spanish (es)** - NEW! Full translation with 453+ translated strings

## Language Configuration

### Setting the Language in the Report Designer

1. Open the Report Designer
2. Navigate to **Tools** → **Options** 
3. In the **Desktop** tab, select your preferred language from the Language dropdown
4. Restart the application for changes to take effect

### Setting the Language Programmatically

The language is determined by the current thread's `CurrentCulture` and `CurrentUICulture`. You can set it in your application:

```csharp
using System.Globalization;
using System.Threading;

// Set Spanish as the UI language
var culture = new CultureInfo("es");
Thread.CurrentThread.CurrentCulture = culture;
Thread.CurrentThread.CurrentUICulture = culture;
```

For ASP.NET applications:

```csharp
using System.Globalization;

// In your Startup.cs or Program.cs
var supportedCultures = new[] { "en-US", "ru-RU", "fr", "es" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("en-US")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);
```

## Translation Coverage

### Spanish (es)

The es translation includes:

- ✅ **Core Engine** (RdlEngine) - Error messages, data processing strings
- ✅ **Report Viewer** (RdlViewer) - UI elements, viewer controls
- ✅ **Report Designer** (RdlDesign) - All designer dialogs and controls
- ✅ **Map Editor** (RdlMapFile) - Map file editor interface
- ✅ **Report Reader** (RdlReader) - Reader application UI

**Total: 453+ translated strings across 79 resource files**

Common translations include:
- UI buttons and menus (Open, Save, Close, Print, etc.)
- Error messages and warnings
- Report elements (Table, Chart, Matrix, Subreport, etc.)
- Data operations (Grouping, Sorting, Filtering, etc.)
- Formatting options (Font, Style, Border, Background, etc.)

## Adding a New Language

To add support for a new language:

1. **Identify the language code** (e.g., `de-DE` for German, `pt-BR` for Portuguese-Brazil)

2. **Copy existing resource files**: For each base `.resx` file, create a new file with your language code:
   ```
   Strings.resx → Strings.[language-code].resx
   ```

3. **Translate strings**: Open the `.resx` files and translate the `<value>` elements while keeping the `<data name>` attributes unchanged.

4. **Test your translation**:
   - Build the solution
   - Set the culture in your application
   - Verify translations appear correctly in the UI

5. **Submit a Pull Request**: Share your translation with the community!

## Resource File Locations

Resource files are located in the following directories:

- `RdlEngine/Resources/` - Core engine strings
- `RdlViewer/Resources/` - Viewer component strings
- `RdlDesign/Resources/` - Designer main strings
- `RdlDesign/*.resx` - Individual control/dialog translations
- `RdlMapFile/Resources/` - Map editor strings
- `RdlReader/Resources/` - Reader application strings
- `RdlDesktop/Resources/` - Desktop application strings

## Translation Guidelines

When contributing translations:

1. **Be consistent** with terminology across all resource files
2. **Use appropriate locale** conventions (date formats, number formats, etc.)
3. **Keep technical terms** in English when appropriate (e.g., SQL, HTML, XML)
4. **Preserve placeholders** like `{0}`, `{1}` in format strings
5. **Test thoroughly** especially with longer translations that might affect UI layout
6. **Consider cultural context** - use translations appropriate for the target region

## Known Issues

- Some UI elements may require application restart to display translated text
- Very long translations may cause layout issues in some dialogs
- Not all third-party controls support full internationalization

## Contributing

We welcome translations for new languages! Please see our [Contributing Guide](https://github.com/majorsilence/My-FyiReporting/wiki/Contribute) for more information.

To contribute a translation:

1. Fork the repository
2. Create a new branch for your translation
3. Add your translated resource files
4. Test the translation
5. Submit a pull request with a description of your changes

## Resources

- [.NET Globalization and Localization](https://docs.microsoft.com/en-us/dotnet/core/extensions/globalization-and-localization)
- [ResX Resource File Format](https://docs.microsoft.com/en-us/dotnet/framework/resources/creating-resource-files-for-desktop-apps#resources-in-resx-files)
- [CultureInfo Class](https://docs.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo)

## Support

If you have questions about internationalization or need help with translations:

- [Discussion Forum](https://groups.google.com/d/forum/myfyireporting)
- [GitHub Issues](https://github.com/majorsilence/My-FyiReporting/issues)
- [Wiki](https://github.com/majorsilence/My-FyiReporting/wiki)
