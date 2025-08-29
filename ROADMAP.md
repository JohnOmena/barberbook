# ROADMAP (MVP → V1)
FASE 0 — Infra: subir Postgres/Redis/n8n/WAHA (docker-compose). Logar WAHA (QR).
FASE 1 — Dados: AppDbContext (Npgsql), Entities, Migrations, Seed (serviços, prof. padrão, disponibilidade).
FASE 2 — API: GET /api/services; GET /api/slots?serviceId&date; POST /api/book; POST /api/cancel; GET /api/status-dia.
FASE 3 — Web: /Admin (login), /Admin/Index (timeline, Check-in/Iniciar/Finalizar/No-show/Cancelar, Encaixe), caixa do dia.
FASE 4 — n8n/WAHA: wa-incoming (IA + estado), slots (consulta API), book (cria agendamento), lembretes 24h/2h.
FASE 5 — Qualidade/Operação: testes (unit/integration com Testcontainers), Serilog, backups pg_dump, /health, (opcional) acesso externo via Cloudflare Tunnel.
