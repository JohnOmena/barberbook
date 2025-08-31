DELETE FROM availabilities a USING tenants t WHERE a."TenantId"=t."Id" AND t."Name"='default';
DELETE FROM professionals  p USING tenants t WHERE p."TenantId"=t."Id" AND t."Name"='default';
DELETE FROM services       s USING tenants t WHERE s."TenantId"=t."Id" AND t."Name"='default';
DELETE FROM tenants        t WHERE t."Name"='default';
