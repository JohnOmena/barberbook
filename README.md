# BarberBook — Agendamentos (MVP)
Stack: .NET 8 • ASP.NET Core Minimal APIs • Razor Pages • PostgreSQL 15 (Npgsql/EF Core) • Redis 7 • n8n • WAHA • Docker

Arquitetura:
Cliente (WhatsApp) → WAHA → n8n → BarberBook.Api (.NET) → PostgreSQL
                                    └→ BarberBook.Web (Razor)
                          Redis (estado de conversa - opcional)

Responsabilidades
- Domain: regras puras (Entities/Enums/ValueObjects).
- Application: casos de uso (GetSlots, CreateBooking, CancelBooking, GetDayStatus), validação e portas (IRepository/IUnitOfWork/ISlotCalculator).
- Infrastructure: EF Core + Npgsql (DbContext, Migrations, Repositórios).
- Api: endpoints públicos (services, slots, book, cancel, status-dia) + /health.
- Web: painel Razor (visão do dia + ações).
- automation/n8n: fluxos (wa-incoming, slots, book, lembretes).
- deploy: docker-compose + env.example.

## Swagger (API)
- URL: `http://localhost:<porta>/swagger`
- Desenvolvimento local (profile http): `http://localhost:5160/swagger`
- Via Docker Compose: `http://localhost:8080/swagger`

Como subir rapidamente:
- Local: `dotnet run --project BarberBook.Api`
- Docker: `./scripts/enable-swagger.ps1` (habilita Development e abre o navegador)
