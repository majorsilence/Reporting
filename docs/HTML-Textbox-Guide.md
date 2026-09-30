# HTML Textbox Guide

## Overview

Majorsilence Reporting supports rendering HTML content within textboxes, similar to the HtmlTextbox control in Telerik Reporting. This feature allows you to display rich-formatted text including bold, italic, lists, links, images, and more.

## How to Enable HTML Rendering

To enable HTML rendering in a textbox, set the `Format` property to `"html"` in the Style section of your textbox.

### In RDL XML:

```xml
<Textbox Name="HtmlTextbox">
  <Value>Your HTML content here</Value>
  <Style>
    <Format>html</Format>
  </Style>
</Textbox>
```

### Programmatically:

When creating a textbox programmatically, ensure the Style.Format property is set to "html".

## Supported HTML Tags

The HTML parser supports a subset of HTML tags for formatting:

### Text Formatting
- `<b>` or `<strong>` - Bold text
- `<i>`, `<em>`, `<cite>`, `<var>` - Italic text
- `<u>` - Underlined text (through style attribute)
- `<code>`, `<samp>` - Monospace font (Courier New)
- `<kbd>` - Monospace bold font
- `<big>` - Increases font size by 20% per tag
- `<small>` - Decreases font size by 20% per tag

### Structure
- `<br>` or `<br/>` - Line break
- `<p>` - Paragraph (adds double line spacing)
- `<hr>` or `<hr/>` - Horizontal rule/line

### Lists
- `<ul>` - Unordered list (bullets)
- `<ol>` - Ordered list (numbers)
- `<li>` - List item

### Links and Images
- `<a href="url">Link text</a>` - Hyperlink
- `<img src="url" alt="description" width="100" height="100" />` - Images (supports http://, https://, and file:// URLs)

### Styling
- `<span style="...">` - Apply inline styles
- `<font color="color" size="size" face="font-family">` - Font styling

## Supported Style Attributes

When using `<span>` or `<font>` tags, you can apply CSS-like styles:

### Color
```html
<span style="color: red;">Red text</span>
<span style="color: #FF0000;">Red text (hex)</span>
<span style="background-color: yellow;">Yellow background</span>
```

### Font Properties
```html
<span style="font-family: Arial;">Arial font</span>
<span style="font-size: 14pt;">14 point font</span>
<span style="font-size: large;">Large font</span>
<span style="font-weight: bold;">Bold text</span>
<span style="font-style: italic;">Italic text</span>
```

### Font Size Values
- Named sizes: `xx-small`, `x-small`, `small`, `medium`, `large`, `x-large`, `xx-large`
- Numeric sizes: `1` (8pt), `2` (10pt), `3` (12pt), `4` (14pt), `5` (18pt), `6` (24pt), `7` (36pt)
- Point sizes: `12pt`, `14pt`, etc.
- Percentage: `120%` (relative to current size)

## Dynamic Expressions in HTML

You can embed dynamic expressions within HTML content using the `<expr>` tag:

```xml
<Textbox Name="HtmlWithExpression">
  <Value>="&lt;b&gt;Customer:&lt;/b&gt; &lt;expr&gt;Fields!CustomerName.Value&lt;/expr&gt;&lt;br/&gt;" &amp;
         "&lt;b&gt;Total:&lt;/b&gt; &lt;expr&gt;Fields!Total.Value&lt;/expr&gt;"</Value>
  <Style>
    <Format>html</Format>
  </Style>
</Textbox>
```

**Note:** In RDL XML, you need to escape HTML entities:
- `<` becomes `&lt;`
- `>` becomes `&gt;`
- `&` becomes `&amp;`

The expression inside `<expr>` tags will be evaluated and the result inserted into the HTML output.

## Complete Examples

### Example 1: Simple Formatted Text

```xml
<Textbox Name="SimpleHtml">
  <Value>="&lt;h1&gt;Report Title&lt;/h1&gt;" &amp;
         "&lt;p&gt;This is a paragraph with &lt;b&gt;bold&lt;/b&gt; and &lt;i&gt;italic&lt;/i&gt; text.&lt;/p&gt;"</Value>
  <Style>
    <Format>html</Format>
  </Style>
</Textbox>
```

### Example 2: Lists

```xml
<Textbox Name="ListExample">
  <Value>="&lt;p&gt;&lt;b&gt;Features:&lt;/b&gt;&lt;/p&gt;" &amp;
         "&lt;ul&gt;" &amp;
         "&lt;li&gt;Feature 1&lt;/li&gt;" &amp;
         "&lt;li&gt;Feature 2&lt;/li&gt;" &amp;
         "&lt;li&gt;Feature 3&lt;/li&gt;" &amp;
         "&lt;/ul&gt;"</Value>
  <Style>
    <Format>html</Format>
  </Style>
</Textbox>
```

### Example 3: With Dynamic Data

```xml
<Textbox Name="DynamicHtml">
  <Value>="&lt;div style='font-family: Arial;'&gt;" &amp;
         "&lt;h2&gt;Customer Report&lt;/h2&gt;" &amp;
         "&lt;p&gt;&lt;b&gt;Name:&lt;/b&gt; &lt;expr&gt;Fields!CustomerName.Value&lt;/expr&gt;&lt;/p&gt;" &amp;
         "&lt;p&gt;&lt;b&gt;Email:&lt;/b&gt; &lt;a href='mailto:&lt;expr&gt;Fields!Email.Value&lt;/expr&gt;'&gt;" &amp;
         "&lt;expr&gt;Fields!Email.Value&lt;/expr&gt;&lt;/a&gt;&lt;/p&gt;" &amp;
         "&lt;p&gt;&lt;b&gt;Total Orders:&lt;/b&gt; &lt;expr&gt;Fields!OrderCount.Value&lt;/expr&gt;&lt;/p&gt;" &amp;
         "&lt;/div&gt;"</Value>
  <Style>
    <Format>html</Format>
  </Style>
</Textbox>
```

### Example 4: Rich Text from Database

If you have HTML content stored in your database (e.g., from a rich text editor), you can display it directly:

```xml
<Textbox Name="RichTextFromDb">
  <Value>=Fields!RichTextContent.Value</Value>
  <Style>
    <Format>html</Format>
  </Style>
</Textbox>
```

Where `RichTextContent` is a field containing HTML markup like:
```html
<p>This is <b>rich text</b> from a database field.</p>
<ul>
<li>Item 1</li>
<li>Item 2</li>
</ul>
```

## Important Notes

### Limitations

1. **Subset of HTML**: Only the tags listed above are supported. Complex HTML structures or CSS may not render correctly.

2. **Images**: Image loading may fail if:
   - The URL is not accessible
   - Network security policies block the request
   - The image format is not supported

3. **Security**: Be cautious when displaying user-generated HTML content, as it could contain malicious scripts (though JavaScript is not executed in this context).

4. **Layout**: HTML content in a textbox flows vertically. Use `CanGrow` property to allow the textbox to expand based on content.

### Best Practices

1. **Use CanGrow**: For HTML textboxes with variable content, enable the `CanGrow` property:
   ```xml
   <CanGrow>true</CanGrow>
   ```

2. **Escape Special Characters**: When building HTML strings in expressions, ensure proper escaping:
   - In RDL XML: Use XML entities (`&lt;`, `&gt;`, `&amp;`)
   - In code: Use proper string escaping

3. **Test Rendering**: Always test HTML textboxes with various content lengths and formats to ensure proper rendering.

4. **Performance**: Complex HTML with many nested tags or images may impact rendering performance. Keep HTML structure simple when possible.

## Troubleshooting

### HTML Not Rendering
- Verify `Format` is set to `"html"` (lowercase)
- Check for proper XML escaping in RDL
- Ensure HTML tags are properly closed

### Images Not Showing
- Verify image URL is accessible
- Check image format (JPEG, PNG supported)
- Ensure network/firewall allows image requests

### Layout Issues
- Enable `CanGrow` for content that may expand
- Check that width is sufficient for content
- Use `<br>` or `<p>` tags for explicit line breaks

## Migration from Other Reporting Tools

If you're migrating from Telerik Reporting or similar tools:

1. **Telerik HtmlTextBox** → Set `Format="html"` on regular Textbox
2. **Most HTML tags** work similarly
3. **Expressions**: Use `<expr>` tags instead of `#=` syntax
4. **Styling**: Use inline `style` attributes within HTML

## See Also

- [Textbox Properties](https://github.com/majorsilence/My-FyiReporting/wiki) - General textbox documentation
- [Expression Reference](https://github.com/majorsilence/My-FyiReporting/wiki) - Using expressions in reports
- [Style Properties](https://github.com/majorsilence/My-FyiReporting/wiki) - Available style options
