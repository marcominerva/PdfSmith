# PDF Smith

A powerful REST API service that generates PDF documents from dynamic HTML templates. PDF Smith supports multiple template engines, offers flexible PDF configuration options, and provides secure API key-based authentication with rate limiting.

[![CodeQL](https://github.com/marcominerva/PdfSmith/actions/workflows/github-code-scanning/codeql/badge.svg)](https://github.com/marcominerva/PdfSmith/actions/workflows/github-code-scanning/codeql)
[![Lint Code Base](https://github.com/marcominerva/PdfSmith/actions/workflows/linter.yml/badge.svg)](https://github.com/marcominerva/PdfSmith/actions/workflows/linter.yml)
[![Copilot](https://github.com/marcominerva/PdfSmith/actions/workflows/copilot-swe-agent/copilot/badge.svg)](https://github.com/marcominerva/PdfSmith/actions/workflows/copilot-swe-agent/copilot)
[![Deploy on Azure](https://github.com/marcominerva/PdfSmith/actions/workflows/deploy.yml/badge.svg)](https://github.com/marcominerva/PdfSmith/actions/workflows/deploy.yml)

## 🚀 Features

- **Dynamic PDF Generation**: Create PDFs from HTML templates with dynamic data injection
- **Multiple Template Engines**: Support for Razor, Scriban, and Handlebars template engines
- **Flexible PDF Options**: Configure page size, orientation, margins, and more
- **API Key Authentication**: Secure access with subscription-based API keys
- **Rate Limiting**: Configurable request limits per subscription
- **Localization Support**: Multi-language template rendering
- **Time Zone Support**: Correct handling and conversion of dates/times based on the user's specified time zone
- **OpenAPI Documentation**: Built-in Swagger documentation

## 📋 Table of Contents

- [Installation](#️-installation)
- [Authentication](#-authentication)
- [API Reference](#-api-reference)
- [Template Engines](#-template-engines)
- [PDF Configuration](#-pdf-configuration)
- [Time Zone Support](#-time-zone-support)
- [Usage Examples](#-usage-examples)
- [Error Handling](#️-error-handling)
- [Rate Limiting](#️-rate-limiting)
- [Configuration](#️-configuration)

## 🛠️ Installation

### Prerequisites

- .NET 10.0 SDK or later
- Chromium browser (automatically installed via Playwright)

### Setup

1. Clone the repository:

```bash
git clone https://github.com/marcominerva/PdfSmith.git
cd PdfSmith
```

2. Build the solution:
 
```bash
dotnet build
```

3. Configure your database connection in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "SqlConnection": "your-connection-string-here"
  }
}
```

4. Run the application:

```bash
dotnet run --project src/PdfSmith
```

The API will be available at `https://localhost:7226` (or your configured port).

## 🔐 Authentication

PdfSmith uses API key authentication with subscription-based access control.

### API Key Setup

1. Include your API key in the request header:
```
x-api-key: your-api-key-here
```

2. Each subscription has configurable rate limits:
   - **Requests per window**: Number of allowed requests
   - **Window duration**: Time window in minutes
   - **Validity period**: API key expiration dates

### Default Administrator Account

A default administrator account is created automatically with the following configuration:

- Username: Set via `AppSettings:AdministratorUserName`
- API Key: Set via `AppSettings:AdministratorApiKey`
- Default limits: 10 requests per minute

## 📚 API Reference

### Generate PDF

**Endpoint:** `POST /api/pdf`

**Headers:**

- `x-api-key`: Your API key (required)
- `Accept-Language`: Language preference (optional, e.g., "en-US", "it-IT")
- `x-time-zone`: The IANA time zone identifier to handle different time zones (optional, if not present UTC will be used)

**Request Body:**

```json
{
  "template": "HTML template string",
  "model": {
    "key": "value",
    "nested": {
      "data": "structure"
    }
  },
  "templateEngine": "razor",
  "fileName": "document.pdf",
  "options": {
    "pageSize": "A4",
    "orientation": "Portrait",
    "margin": {
      "top": "2.5cm",
      "bottom": "2cm",
      "left": "2cm",
      "right": "2cm"
    }
  }
}
```

**Response:**

- **Content-Type:** `application/pdf`
- **Content-Disposition:** `attachment; filename="document.pdf"`
- **Body:** PDF file binary data

## 🎨 Template Engines

PdfSmith supports three powerful template engines:

### Razor Template Engine

Razor provides C#-based templating with full programming capabilities.

**Key:** `razor`

**Example:**

```html
<html>
<body>
    <h1>Hello @Model.Name!</h1>
    <p>Order Date: @Model.Date.ToString("dd/MM/yyyy")</p>
    <ul>
    @foreach(var item in Model.Items)
    {
        <li>@item.Name - @item.Price.ToString("C")</li>
    }
    </ul>
    <p>Total: @Model.Total.ToString("C")</p>
</body>
</html>
```

### Scriban Template Engine

Scriban is a fast, powerful, safe, and lightweight text templating language.

**Key:** `scriban`

**Example:**

```html
<html>
<body>
    <h1>Hello {{ Model.Name }}!</h1>
    <p>Order Date: {{ Model.Date | date.to_string '%d/%m/%Y' }}</p>
    <ul>
    {{- for item in Model.Items }}
        <li>{{ item.Name }} - {{ item.Price | object.format "C" }}</li>
    {{- end }}
    </ul>
    <p>Total: {{ Model.Total | object.format "C" }}</p>
</body>
</html>
```

### Handlebars Template Engine

Handlebars provides logic-less templates with a designer-friendly syntax, ideal for collaborative development workflows.

**Key:** `handlebars`

**Example:**

```html
<html>
<body>
    <h1>Hello {{Model.Name}}!</h1>
    <p>Order Date: {{formatDate Model.Date "dd/MM/yyyy"}}</p>
    <ul>
    {{#each Items}}
        <li>{{Name}} - {{formatCurrency Price}}</li>
    {{/each}}
    </ul>
    <p>Total: {{formatCurrency Model.Total}}</p>
</body>
</html>
```

**Built-in Helpers:**

- `formatNumber` - Formats number values as string using the specified format and the current culture
- `formatCurrency` - Formats decimal values as currency using current culture
- `formatDate` - Formats dates with optional format string
- `now` - Gets the current datetime with optional format string
- `utcNow` - Gets the current UTC datetime with optional format string
- `add` - Adds two numeric values for calculations within templates
- `subtract` - Subtracts two numeric values for calculations within templates
- `multiply` - Multiplies two numeric values for calculations within templates
- `divide` - Divides two numeric values for calculations within templates
- `round` - Rounds a numberic value to the specified number of decimals

## 📄 PDF Configuration

### Page Size Options

- Standard sizes: `"A4"`, `"A3"`, `"A5"`, `"Letter"`, `"Legal"`
- Custom sizes: `"210mm x 297mm"` or `"8.5in x 11in"`

### Orientation

- `"Portrait"` (default)
- `"Landscape"`

### Margins

Configure margins using CSS units:

```json
{
  "margin": {
    "top": "2.5cm",
    "bottom": "2cm", 
    "left": "2cm",
    "right": "2cm"
  }
}
```

Supported units: `mm`, `cm`, `in`, `px`, `pt`, `pc`

## 🕒 Time Zone Support

When generating PDFs that include dates and times, it is important to ensure that these values are represented in the correct time zone for the end user. By default, .NET's `DateTime.Now` returns the server's local time, which may not match the user's intended time zone. Similarly, when converting or displaying dates from the input model, the correct time zone context is essential to avoid confusion or errors in generated documents.

**PdfSmith** allows clients to specify the desired time zone using the `x-time-zone` HTTP header (with an [IANA time zone identifier](https://en.wikipedia.org/wiki/List_of_tz_database_time_zones)). If this header is not provided, UTC is used by default. This ensures that:

- All date and time values (such as those produced by `DateTime.Now` or parsed from the model) are converted and rendered according to the specified time zone.
- Templates using date/time expressions (e.g., `@Model.Date`, `date.now`, or similar) will reflect the correct local time for the user, not just the server.
- Consistency is maintained across different users and regions, especially for time-sensitive documents like invoices, reports, or logs.

## 💡 Usage Examples

### Basic PDF Generation

```csharp
using System.Net.Http.Json;
using PdfSmith.Shared.Models;

var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("x-api-key", "your-api-key");
httpClient.DefaultRequestHeaders.Add("x-time-zone", "Europe/Rome")

var request = new PdfGenerationRequest(
    template: "<html><body><h1>Hello @Model.Name!</h1></body></html>",
    model: new { Name = "John Doe" },
    templateEngine: "razor"
);

var response = await httpClient.PostAsJsonAsync("https://localhost:7226/api/pdf", request);
var pdfBytes = await response.Content.ReadAsByteArrayAsync();
await File.WriteAllBytesAsync("output.pdf", pdfBytes);
```

### Advanced PDF with Custom Options

```csharp
var request = new PdfGenerationRequest(
    template: htmlTemplate,
    model: orderData,
    options: new PdfOptions
    {
        PageSize = "A4",
        Orientation = PdfOrientation.Portrait,
        Margin = new PdfMargin(
            Top: "50mm",
            Bottom: "30mm", 
            Left: "25mm",
            Right: "25mm"
        )
    },
    templateEngine: "razor",
    fileName: "invoice.pdf"
);
```

### Invoice Generation Example with Razor Template

```csharp
var order = new
{
    CustomerName = "Acme Corp",
    Date = DateTime.Now,
    InvoiceNumber = "INV-2024-001",
    Items = new[]
    {
        new { Name = "Product A", Quantity = 2, Price = 29.99m },
        new { Name = "Product B", Quantity = 1, Price = 49.99m }
    },
    Total = 109.97m
};

var razorTemplate = """
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; }
        .header { text-align: center; margin-bottom: 30px; }
        .invoice-details { margin-bottom: 20px; }
        table { width: 100%; border-collapse: collapse; }
        th, td { padding: 10px; text-align: left; border-bottom: 1px solid #ddd; }
        .total { font-weight: bold; text-align: right; }
    </style>
</head>
<body>
    <div class="header">
        <h1>Invoice</h1>
        <p>Invoice #@Model.InvoiceNumber</p>
    </div>
    
    <div class="invoice-details">
        <p><strong>Customer:</strong> @Model.CustomerName</p>
        <p><strong>Date:</strong> @Model.Date.ToString("dd/MM/yyyy")</p>
    </div>
    
    <table>
        <thead>
            <tr>
                <th>Item</th>
                <th>Quantity</th>
                <th>Price</th>
                <th>Total</th>
            </tr>
        </thead>
        <tbody>
            @foreach(var item in Model.Items)
            {
            <tr>
                <td>@item.Name</td>
                <td>@item.Quantity</td>
                <td>@item.Price.ToString("C")</td>
                <td>@((item.Quantity * item.Price).ToString("C"))</td>
            </tr>
            }
        </tbody>
        <tfoot>
            <tr>
                <td colspan="3" class="total">Total:</td>
                <td class="total">@Model.Total.ToString("C")</td>
            </tr>
        </tfoot>
    </table>
</body>
</html>
""";

var request = new PdfGenerationRequest(razorTemplate, order, templateEngine: "razor");
```

### Handlebars Template Example

```csharp
var order = new
{
    CustomerName = "Acme Corp",
    Date = DateTime.Now,
    InvoiceNumber = "INV-2024-001",
    Items = new[]
    {
        new { Name = "Product A", Quantity = 2, Price = 29.99m },
        new { Name = "Product B", Quantity = 1, Price = 49.99m }
    },
    Total = 109.97m
};

var handlebarsTemplate = """
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; }
        .header { text-align: center; margin-bottom: 30px; }
        .invoice-details { margin-bottom: 20px; }
        table { width: 100%; border-collapse: collapse; }
        th, td { padding: 10px; text-align: left; border-bottom: 1px solid #ddd; }
        .total { font-weight: bold; text-align: right; }
    </style>
</head>
<body>
    <div class="header">
        <h1>Invoice</h1>
        <p>Invoice #{{Model.InvoiceNumber}}</p>
    </div>
    
    <div class="invoice-details">
        <p><strong>Customer:</strong> {{Model.CustomerName}}</p>
        <p><strong>Date:</strong> {{formatDate Model.Date "dd/MM/yyyy"}}</p>
    </div>
    
    <table>
        <thead>
            <tr>
                <th>Item</th>
                <th>Quantity</th>
                <th>Price</th>
                <th>Total</th>
            </tr>
        </thead>
        <tbody>
            {{#each Model.Items}}
            <tr>
                <td>{{Name}}</td>
                <td>{{Quantity}}</td>
                <td>{{formatCurrency Price}}</td>
                <td>{{formatCurrency (multiply Price Quantity)}}</td>
            </tr>
            {{/each}}
        </tbody>
        <tfoot>
            <tr>
                <td colspan="3" class="total">Total:</td>
                <td class="total">{{formatCurrency Model.Total}}</td>
            </tr>
        </tfoot>
    </table>
</body>
</html>
""";

var request = new PdfGenerationRequest(handlebarsTemplate, order, templateEngine: "handlebars");
```

## ⚠️ Error Handling

The API returns appropriate HTTP status codes and error details:

### Common Status Codes

- **200 OK**: PDF generated successfully
- **400 Bad Request**: Invalid request data or template errors
- **401 Unauthorized**: Invalid or missing API key
- **408 Request Timeout**: Generation took longer than 30 seconds
- **429 Too Many Requests**: Rate limit exceeded
- **500 Internal Server Error**: Unexpected server error

### Error Response Format

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Bad Request",
  "status": 400,
  "detail": "Template engine 'invalid' has not been registered",
  "instance": "/api/pdf"
}
```

## 🛡️ Rate Limiting

Rate limiting is enforced per subscription:

- **Window-based limiting**: Requests are counted within specified time windows
- **Per-user limits**: Each API key has its own rate limit configuration
- **429 Response**: When limits are exceeded, includes `Retry-After` header
- **Configurable**: Administrators can set custom limits per subscription

Example rate limit headers:

```
Retry-After: 60
```

## ⚙️ Configuration

### Application Settings

Key configuration options in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "SqlConnection": "Server=.;Database=PdfSmith;Trusted_Connection=true;"
  },
  "AppSettings": {
    "AdministratorUserName": "admin",
    "AdministratorApiKey": "your-admin-api-key"
  }
}
```

### Environment Variables

- `PLAYWRIGHT_BROWSERS_PATH`: Custom path for Playwright browsers installation

### Playwright Configuration

Chromium is automatically installed via Playwright. If `PLAYWRIGHT_BROWSERS_PATH` isn't specified, browsers are installed to the default locations:

**Windows:**

```
%USERPROFILE%\AppData\Local\ms-playwright
```

**Linux:**

```
~/.cache/ms-playwright
```

**macOS:**

```
~/Library/Caches/ms-playwright
```

## 🔧 Development

### Project Structure

```
PdfSmith/
├── src/
│   ├── PdfSmith/                    # Main API project
│   ├── PdfSmith.BusinessLayer/      # Business logic and services
│   ├── PdfSmith.DataAccessLayer/    # Data access and entities
│   └── PdfSmith.Shared/             # Shared models and contracts
├── samples/
│   └── PdfSmith.Client/             # Example client application
└── README.md
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

For issues, questions, or contributions, please visit the [GitHub repository](https://github.com/marcominerva/PdfSmith).
