-- Tenant
INSERT INTO tenants ("Id","Name")
SELECT 'ef96daa0-19a7-f031-dc97-f8753865266a'::uuid, 'default'
WHERE NOT EXISTS (SELECT 1 FROM tenants WHERE "Id"='ef96daa0-19a7-f031-dc97-f8753865266a'::uuid OR "Name"='default');

-- Profissional default
INSERT INTO professionals ("Id","Name","Active","IsDefault","TenantId")
SELECT '91e00a6e-d606-9ee2-9918-2c70284b09b6'::uuid, 'Barbeiro', true, true, 'ef96daa0-19a7-f031-dc97-f8753865266a'::uuid
WHERE NOT EXISTS (SELECT 1 FROM professionals WHERE "TenantId"='ef96daa0-19a7-f031-dc97-f8753865266a'::uuid AND "IsDefault"=true);INSERT INTO availabilities ("Id","TenantId","ProfessionalId","Weekday","Start","End")
SELECT 'ffc6c4d0-8947-f23f-bc86-a22174debdaf'::uuid, 'ef96daa0-19a7-f031-dc97-f8753865266a'::uuid, '91e00a6e-d606-9ee2-9918-2c70284b09b6'::uuid, 1, '09:00'::time, '18:00'::time
WHERE NOT EXISTS (
  SELECT 1 FROM availabilities
  WHERE "TenantId"='ef96daa0-19a7-f031-dc97-f8753865266a'::uuid AND "ProfessionalId"='91e00a6e-d606-9ee2-9918-2c70284b09b6'::uuid
    AND "Weekday"=1 AND "Start"='09:00'::time AND "End"='18:00'::time
);INSERT INTO availabilities ("Id","TenantId","ProfessionalId","Weekday","Start","End")
SELECT 'cf291293-5320-f26f-9120-e9f9dcb0b9fb'::uuid, 'ef96daa0-19a7-f031-dc97-f8753865266a'::uuid, '91e00a6e-d606-9ee2-9918-2c70284b09b6'::uuid, 2, '09:00'::time, '18:00'::time
WHERE NOT EXISTS (
  SELECT 1 FROM availabilities
  WHERE "TenantId"='ef96daa0-19a7-f031-dc97-f8753865266a'::uuid AND "ProfessionalId"='91e00a6e-d606-9ee2-9918-2c70284b09b6'::uuid
    AND "Weekday"=2 AND "Start"='09:00'::time AND "End"='18:00'::time
);INSERT INTO availabilities ("Id","TenantId","ProfessionalId","Weekday","Start","End")
SELECT '2c0b334e-2c8a-907f-2890-2deb55a1df8e'::uuid, 'ef96daa0-19a7-f031-dc97-f8753865266a'::uuid, '91e00a6e-d606-9ee2-9918-2c70284b09b6'::uuid, 3, '09:00'::time, '18:00'::time
WHERE NOT EXISTS (
  SELECT 1 FROM availabilities
  WHERE "TenantId"='ef96daa0-19a7-f031-dc97-f8753865266a'::uuid AND "ProfessionalId"='91e00a6e-d606-9ee2-9918-2c70284b09b6'::uuid
    AND "Weekday"=3 AND "Start"='09:00'::time AND "End"='18:00'::time
);INSERT INTO availabilities ("Id","TenantId","ProfessionalId","Weekday","Start","End")
SELECT 'b984d5a6-27c0-278a-0fbf-6b2355d5cc0f'::uuid, 'ef96daa0-19a7-f031-dc97-f8753865266a'::uuid, '91e00a6e-d606-9ee2-9918-2c70284b09b6'::uuid, 4, '09:00'::time, '18:00'::time
WHERE NOT EXISTS (
  SELECT 1 FROM availabilities
  WHERE "TenantId"='ef96daa0-19a7-f031-dc97-f8753865266a'::uuid AND "ProfessionalId"='91e00a6e-d606-9ee2-9918-2c70284b09b6'::uuid
    AND "Weekday"=4 AND "Start"='09:00'::time AND "End"='18:00'::time
);INSERT INTO availabilities ("Id","TenantId","ProfessionalId","Weekday","Start","End")
SELECT '3ec9dc9d-bb9d-343a-2e67-47483fd07df4'::uuid, 'ef96daa0-19a7-f031-dc97-f8753865266a'::uuid, '91e00a6e-d606-9ee2-9918-2c70284b09b6'::uuid, 5, '09:00'::time, '18:00'::time
WHERE NOT EXISTS (
  SELECT 1 FROM availabilities
  WHERE "TenantId"='ef96daa0-19a7-f031-dc97-f8753865266a'::uuid AND "ProfessionalId"='91e00a6e-d606-9ee2-9918-2c70284b09b6'::uuid
    AND "Weekday"=5 AND "Start"='09:00'::time AND "End"='18:00'::time
);INSERT INTO availabilities ("Id","TenantId","ProfessionalId","Weekday","Start","End")
SELECT '0110dbce-d9f3-cd93-2141-017987d0cca6'::uuid, 'ef96daa0-19a7-f031-dc97-f8753865266a'::uuid, '91e00a6e-d606-9ee2-9918-2c70284b09b6'::uuid, 6, '09:00'::time, '18:00'::time
WHERE NOT EXISTS (
  SELECT 1 FROM availabilities
  WHERE "TenantId"='ef96daa0-19a7-f031-dc97-f8753865266a'::uuid AND "ProfessionalId"='91e00a6e-d606-9ee2-9918-2c70284b09b6'::uuid
    AND "Weekday"=6 AND "Start"='09:00'::time AND "End"='18:00'::time
);INSERT INTO services ("Id","TenantId","Name","DurationMin","Active","BufferMin","Price","Slug")
SELECT '9a6f7eb2-4faa-2db3-8ae0-538b1d5b9b45'::uuid, 'ef96daa0-19a7-f031-dc97-f8753865266a'::uuid, 'Degrade na zero', 30, true, 0, 0, 'degrade-na-zero'
WHERE NOT EXISTS (
  SELECT 1 FROM services WHERE "TenantId"='ef96daa0-19a7-f031-dc97-f8753865266a'::uuid AND "Name"='Degrade na zero'
);INSERT INTO services ("Id","TenantId","Name","DurationMin","Active","BufferMin","Price","Slug")
SELECT '682ced31-ca21-7ffd-9663-31d647d946a5'::uuid, 'ef96daa0-19a7-f031-dc97-f8753865266a'::uuid, 'Degrade navalhado', 40, true, 0, 0, 'degrade-navalhado'
WHERE NOT EXISTS (
  SELECT 1 FROM services WHERE "TenantId"='ef96daa0-19a7-f031-dc97-f8753865266a'::uuid AND "Name"='Degrade navalhado'
);INSERT INTO services ("Id","TenantId","Name","DurationMin","Active","BufferMin","Price","Slug")
SELECT 'e09e4b09-b4b8-cf6d-f646-12e9cfb83866'::uuid, 'ef96daa0-19a7-f031-dc97-f8753865266a'::uuid, 'Corte social máquina e tesoura', 20, true, 0, 0, 'corte-social-mquina-e-tesoura'
WHERE NOT EXISTS (
  SELECT 1 FROM services WHERE "TenantId"='ef96daa0-19a7-f031-dc97-f8753865266a'::uuid AND "Name"='Corte social máquina e tesoura'
);INSERT INTO services ("Id","TenantId","Name","DurationMin","Active","BufferMin","Price","Slug")
SELECT '20325efe-3e84-1a4a-bc61-8ccae1a78809'::uuid, 'ef96daa0-19a7-f031-dc97-f8753865266a'::uuid, 'Corte só máquina', 20, true, 0, 0, 'corte-s-mquina'
WHERE NOT EXISTS (
  SELECT 1 FROM services WHERE "TenantId"='ef96daa0-19a7-f031-dc97-f8753865266a'::uuid AND "Name"='Corte só máquina'
);INSERT INTO services ("Id","TenantId","Name","DurationMin","Active","BufferMin","Price","Slug")
SELECT '1b863413-9578-5434-d4cb-a67e1020b86b'::uuid, 'ef96daa0-19a7-f031-dc97-f8753865266a'::uuid, 'Corte na tesoura', 30, true, 0, 0, 'corte-na-tesoura'
WHERE NOT EXISTS (
  SELECT 1 FROM services WHERE "TenantId"='ef96daa0-19a7-f031-dc97-f8753865266a'::uuid AND "Name"='Corte na tesoura'
);INSERT INTO services ("Id","TenantId","Name","DurationMin","Active","BufferMin","Price","Slug")
SELECT '73c03c6b-8df9-4802-b0ec-e01e2032b453'::uuid, 'ef96daa0-19a7-f031-dc97-f8753865266a'::uuid, 'Barba', 20, true, 0, 0, 'barba'
WHERE NOT EXISTS (
  SELECT 1 FROM services WHERE "TenantId"='ef96daa0-19a7-f031-dc97-f8753865266a'::uuid AND "Name"='Barba'
);INSERT INTO services ("Id","TenantId","Name","DurationMin","Active","BufferMin","Price","Slug")
SELECT '4c99014d-bb6d-28f7-e5e5-c57c713f022a'::uuid, 'ef96daa0-19a7-f031-dc97-f8753865266a'::uuid, 'Pé de cabelo e sobrancelha', 10, true, 0, 0, 'p-de-cabelo-e-sobrancelha'
WHERE NOT EXISTS (
  SELECT 1 FROM services WHERE "TenantId"='ef96daa0-19a7-f031-dc97-f8753865266a'::uuid AND "Name"='Pé de cabelo e sobrancelha'
);INSERT INTO services ("Id","TenantId","Name","DurationMin","Active","BufferMin","Price","Slug")
SELECT '8d52b5c7-1aa2-1a44-4ee2-a9e33d655ba2'::uuid, 'ef96daa0-19a7-f031-dc97-f8753865266a'::uuid, 'Cabelo e barba', 50, true, 0, 0, 'cabelo-e-barba'
WHERE NOT EXISTS (
  SELECT 1 FROM services WHERE "TenantId"='ef96daa0-19a7-f031-dc97-f8753865266a'::uuid AND "Name"='Cabelo e barba'
);
