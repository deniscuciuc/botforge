# TelegramShop

A complete e-commerce bot demonstrating the **TeleForge.Payments** system with Telegram Stars, inline keyboard
catalog browsing, shopping cart, and order history.

## Features

- **Product Catalog** — Browse products with inline keyboard navigation
- **Product Details** — View descriptions and prices with add-to-cart buttons
- **Shopping Cart** — Add items, view cart, clear cart
- **Telegram Stars Checkout** — Create and send Star invoices via `IInvoiceBuilder`
- **Pre-Checkout Validation** — Validates orders before payment with `IPreCheckoutValidator`
- **Payment Processing** — Records successful payments with `IPaymentProcessor`
- **Order History** — View past purchases with `/orders`
- **EF Core + SQLite** — Persistent product catalog, cart, and order storage
- **TypedPayload** — Strongly-typed invoice payloads with automatic serialization

## Bot Commands

| Command    | Description                       |
|------------|-----------------------------------|
| `/start`   | Welcome message with instructions |
| `/catalog` | Browse all products               |
| `/cart`    | View your shopping cart           |
| `/orders`  | View order history                |

## How It Works

### Catalog & Product Browsing

1. `/catalog` loads all products from the database and displays them as inline keyboard buttons
2. Tapping a product triggers the `product:{id}` callback, showing product details
3. The detail view has "Add to Cart" and "Back to Catalog" buttons

### Shopping Cart

1. "Add to Cart" callback increments quantity (or inserts new cart item)
2. `/cart` shows all items with totals
3. "Clear Cart" removes all items for the user

### Payment Flow

1. "Checkout" callback creates an `Order` record with `OrderItem`s from the cart
2. An invoice is built using `InvoiceBuilder.Stars()` with a `ShopPayload(orderId)`
3. `IInvoiceService.SendInvoiceAsync` sends the Telegram Stars invoice
4. `ShopCheckoutValidator` validates the pre-checkout query (order exists, not paid, amount matches)
5. `ShopPaymentProcessor` records the Telegram charge ID, clears the cart, and sends a confirmation

### TypedPayload

`ShopPayload` extends `TypedPayload` with prefix `"shop"` and serializes as `"shop:orderId"`. This allows the payment
system to route checkout/payment events to the correct handlers.

## Project Structure

```text
TelegramShop/
├── Program.cs                          # Host setup, DI configuration
├── TelegramShop.csproj                 # Project file with package references
├── appsettings.json                    # Bot token configuration
├── Data/
│   └── ShopDbContext.cs                # EF Core context with Product, CartItem, Order, OrderItem
├── Handlers/
│   ├── CatalogHandlers.cs             # /start, /catalog, product:{id}, catalog:back
│   ├── CartHandlers.cs                # cart:add:{id}, /cart, cart:clear
│   ├── CheckoutHandlers.cs            # cart:checkout, ShopCheckoutValidator, ShopPaymentProcessor
│   ├── OrderHistoryHandler.cs         # /orders
│   └── ShopPayload.cs                 # TypedPayload implementation
└── README.md
```

## How to Run

1. Create a bot via [@BotFather](https://t.me/BotFather) and enable payments
2. Set your bot token in `appsettings.json`:

    ```json
    {
      "Telegram": {
        "BotToken": "123456:ABC-DEF..."
      }
    }
    ```

3. Run the project:

    ```bash
    dotnet run --project examples/TelegramShop
    ```

4. The SQLite database (`shop.db`) is created automatically with seed data (6 products)
5. Open your bot in Telegram and use `/catalog` to start shopping

## Payment Configuration

Payments are registered in `Program.cs`:

```csharp
builder.Services.AddTelegramPayments(
    options => { options.AutoRefundOnFailure = true; },
    handlers =>
    {
        handlers.AddValidator<ShopCheckoutValidator>("shop");
        handlers.AddProcessor<ShopPaymentProcessor>("shop");
    });
```

The `"shop"` prefix matches `ShopPayload.Prefix`, ensuring checkout and payment events are routed to the correct
validator and processor.

## Key Framework Features Demonstrated

- `InvoiceBuilder.Stars()` — Fluent invoice builder for Telegram Stars
- `TypedPayload` — Strongly-typed payload serialization
- `IPreCheckoutValidator` — Pre-checkout validation pipeline
- `IPaymentProcessor` — Post-payment processing
- `IInvoiceService.SendInvoiceAsync` — Send invoices via the Telegram API
- `[CallbackQuery("pattern:{param}")]` — Route parameter extraction from callbacks
- `CallbackContext.GetRouteParam<T>()` — Typed route parameter parsing
- `ITelegramMessage.WithInlineKeyboard()` — Inline keyboard support
- `ITelegramMessage.EditMessage()` — Edit existing messages on callback
