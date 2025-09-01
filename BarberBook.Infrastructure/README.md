# BarberBook.Infrastructure
EF Core 8 (Npgsql), DbContext, Migrations, Repositórios.
- timestamptz (UTC) p/ StartsAt/EndsAt
- uuid p/ IDs; numeric(10,2) p/ preços
- Índice único (TenantId, ProfessionalId, StartsAt) evita duplo booking
