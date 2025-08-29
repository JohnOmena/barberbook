# automation/n8n
Fluxos p/ importar:
- wa-incoming: intenção (IA), estado, roteamento
- slots: consulta /api/services e /api/slots
- book: chama /api/book, confirma e agenda lembretes (Cron)
Dica: proteja webhooks com path longo e token em header.
