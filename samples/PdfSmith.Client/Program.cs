using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bogus;
using PdfSmith.Shared.Models;

var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", "key-2");
httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("it-IT"));

var productFaker = new Faker<Product>();
productFaker.RuleFor(p => p.Name, f => f.Commerce.ProductName());
productFaker.RuleFor(p => p.Description, f => f.Commerce.ProductDescription());
productFaker.RuleFor(p => p.UnitPrice, f => f.Random.Decimal(1, 100));
productFaker.RuleFor(p => p.Quantity, f => f.Random.Number(1, 10));

var order = new Order
{
    CustomerName = "Marco",
    Date = DateTime.Now,
    Products = productFaker.Generate(7)
};

var request = new PdfGenerationRequest("""
    <html>
        <head>
            <style media="all" type="text/css">
            html {
                font-size: 16px;
                font-family: Helvetica Neue, Helvetica, Arial, and sans-serif;
            }
            table {
                    border-collapse: collapse;
                    width: 100%;
                }
                th, td {
                    font-size: 16px;
                    padding: 8px;
                    text-align: left;
                }
                th {
                    background-color: green;
                    color: white;
                }
                .right { 
                    text-align: right; 
                }
                .even {
                    background-color: #f2f2f2;
                }
                .odd {
                    background-color: #ffffff;
                }
            </style>
        </head>
        <body>
            <p>
                Dear {{ Model.CustomerName }},
                <br />
                Here it is the details of the order you placed on {{ Model.Date | date.to_string '%d/%m/%Y %R' }}.
            </p>
            <table>
                <thead>
                    <tr>
                        <th>Name</th>
                        <th>Description</th>
                        <th class="right">Price</th>
                        <th class="right">Quantity</th>
                        <th class="right">Total</th>
                    </tr>
                </thead>
                <tbody>
                    {{- for product in Model.Products }}
                        <tr class="{{ if for.even }}even{{ else }}odd{{ end }}">
                            <td>{{ product.Name }}</td>
                            <td>{{ product.Description }}</td>
                            <td class="right">{{ product.UnitPrice | object.format "C" }}</td>
                            <td class="right">{{ product.Quantity }}</td>
                            <td class="right">{{ product.TotalPrice | object.format "C" }}</td>
                        </tr>
                    {{- end }}
                </tbody>
            </table>
            <p align="right">Total: {{ Model.Total | object.format "C" }}</p>
        </body>
    </html>
    """, order);

using var response = await httpClient.PostAsJsonAsync("https://localhost:7226/api/pdf", request);

Console.WriteLine($"Status code: {response.StatusCode}");

var bytes = await response.Content.ReadAsByteArrayAsync();
await File.WriteAllBytesAsync(@"D:\order.pdf", bytes);

public class Order
{
    public string? CustomerName { get; set; }

    public DateTime Date { get; set; }

    public IList<Product> Products { get; set; } = [];

    public decimal Total => Products?.Sum(p => p.TotalPrice) ?? 0;
}

public class Product
{
    public string? Name { get; set; }

    public string? Description { get; set; }

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal TotalPrice => UnitPrice * Quantity;
}