# RestaurantOS — Setup Guide

A Blazor Server restaurant management system: POS, payments (Cash/Card/bKash/Nagad/Rocket),
menu & table management, staff/role-based authorization, PDF & Excel reports, accounts/expenses,
and a live UI-customization panel — built on the same patterns as TurfControlSystem
(custom cookie auth, EF Core + SQL Server, QuestPDF, ClosedXML).

## 1. Prerequisites
- .NET 8 SDK
- SQL Server (LocalDB, Express, or full) — or swap the connection string for Azure SQL

## 2. First run
```bash
cd RestaurantOS
dotnet restore
```
Update `appsettings.json` → `ConnectionStrings:DefaultConnection` if your SQL Server instance
differs from the default (`.\SQLEXPRESS`).

```bash
dotnet run
```
On first launch, `DbInitializer` calls `EnsureCreated()` and seeds:
- Roles: **Admin, Manager, Cashier, Waiter** with a permission matrix
- Login: **admin / Admin@123** — change this immediately in Staff & Roles / DB
- 12 sample tables, 5 menu categories with sample items

> `EnsureCreated()` is fine for evaluating the app, but switch to real **EF Core Migrations**
> before production (`dotnet ef migrations add InitialCreate`, then `dotnet ef database update`),
> since `EnsureCreated()` won't track future schema changes.

## 3. What's implemented
| Area | Where |
|---|---|
| Auth & permissions | `Services/Auth`, `Models/Identity.cs` — custom cookie auth, BCrypt hashing, per-role permission claims (no ASP.NET Identity, matching your TurfControlSystem pattern) |
| POS / Order taking | `Components/Pages/Pos/PosTerminal.razor` — category filter, cart, live totals, checkout |
| Payments | `Services/Payments/PaymentService.cs` — Cash, Card, bKash, Nagad, Rocket; `TransactionReference` field is the hook point for a real gateway SDK/webhook |
| Menu management | `Components/Pages/Menu`, `Services/Menu` |
| Tables | `Components/Pages/Tables` — floor plan, tap to open/seat |
| Staff & Roles | `Components/Pages/Staff` — team list + permission matrix view |
| Reports | `Services/Reports/ReportService.cs` (QuestPDF receipts + sales report PDF), `ExcelExportService.cs` (ClosedXML) |
| Accounts | `Services/Accounts/AccountService.cs` — revenue, VAT, expenses, net profit, payment-method breakdown |
| UI customization | `Components/Pages/Settings/AppSettings.razor` + `Models/Accounting.cs::RestaurantSettings` — restaurant name, logo, primary/secondary/accent colors, dark mode, VAT/service charge defaults, all applied as CSS variables in `App.razor` |

## 4. Design system
`wwwroot/css/app.css` — warm "modern bistro" theme: charcoal sidebar with a burnt-orange
accent gradient, Sora (headings) + Plus Jakarta Sans (body), card-based dashboard, pill
category filters on the POS screen, dark-mode variables ready to go via the Settings page.

## 5. Known gaps / next steps
- **Not yet compiled** — this sandbox has no .NET SDK, so run `dotnet build` on your machine
  first and send me any errors; I'll fix them.
- Payment gateway calls are stubbed (`TransactionReference`) — wire in real bKash/Nagad/Stripe
  SDK calls inside `PaymentService.RecordPaymentAsync` when you have merchant credentials.
- Role editing UI shows the permission matrix but doesn't yet let you toggle/save it — easy
  follow-up (`RolePermission` CRUD) once you confirm the permission set you want.
- No kitchen-display / order-status board yet (Order.Status has the states — just needs a screen).
- No image upload for menu items/logo — currently URL-based (`ImageUrl`, `LogoUrl`).
