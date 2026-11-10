-- Add ColorHex and Icon to Roles table
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Roles' AND column_name='ColorHex') THEN
        ALTER TABLE "Roles" ADD COLUMN "ColorHex" text NOT NULL DEFAULT '#4f46e5';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Roles' AND column_name='Icon') THEN
        ALTER TABLE "Roles" ADD COLUMN "Icon" text NOT NULL DEFAULT 'shield';
    END IF;
END $$;

-- Update nice colors and icons for default roles
UPDATE "Roles" SET "ColorHex" = '#ef4444', "Icon" = 'shield' WHERE "Name" = 'Administrator';
UPDATE "Roles" SET "ColorHex" = '#8b5cf6', "Icon" = 'user-check' WHERE "Name" = 'Manager';
UPDATE "Roles" SET "ColorHex" = '#ec4899', "Icon" = 'users' WHERE "Name" = 'HR';
UPDATE "Roles" SET "ColorHex" = '#3b82f6', "Icon" = 'shopping-cart' WHERE "Name" = 'Sales';
UPDATE "Roles" SET "ColorHex" = '#06b6d4', "Icon" = 'truck' WHERE "Name" = 'Purchase';
UPDATE "Roles" SET "ColorHex" = '#f59e0b', "Icon" = 'archive' WHERE "Name" = 'Warehouse';
UPDATE "Roles" SET "ColorHex" = '#10b981', "Icon" = 'calculator' WHERE "Name" = 'Accountant';
UPDATE "Roles" SET "ColorHex" = '#6366f1', "Icon" = 'globe' WHERE "Name" = 'Import-Export';
UPDATE "Roles" SET "ColorHex" = '#64748b', "Icon" = 'user' WHERE "Name" = 'Employee';

-- Add Scope to RolePermissions table
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='RolePermissions' AND column_name='Scope') THEN
        ALTER TABLE "RolePermissions" ADD COLUMN "Scope" integer NOT NULL DEFAULT 3;
    END IF;
END $$;

-- Create TemporaryRoleAssignments table
CREATE TABLE IF NOT EXISTS "TemporaryRoleAssignments" (
    "Id" serial PRIMARY KEY,
    "UserId" text NOT NULL,
    "RoleId" text NOT NULL,
    "StartsAt" timestamp with time zone NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "Reason" text NOT NULL,
    "Status" integer NOT NULL DEFAULT 2,
    "GrantedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "GrantedBy" text,
    "RevokedAt" timestamp with time zone,
    "RevokedBy" text
);

CREATE INDEX IF NOT EXISTS "IX_TemporaryRoleAssignments_UserId" ON "TemporaryRoleAssignments" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_TemporaryRoleAssignments_RoleId" ON "TemporaryRoleAssignments" ("RoleId");
CREATE INDEX IF NOT EXISTS "IX_TemporaryRoleAssignments_Status" ON "TemporaryRoleAssignments" ("Status");
CREATE INDEX IF NOT EXISTS "IX_TemporaryRoleAssignments_ExpiresAt" ON "TemporaryRoleAssignments" ("ExpiresAt");

-- Clean up Employee role permissions: remove Customers (14) and Suppliers (15) from Employee role
DO $$
DECLARE
    emp_role_id text;
BEGIN
    SELECT "Id" INTO emp_role_id FROM "Roles" WHERE "Name" = 'Employee';
    IF emp_role_id IS NOT NULL THEN
        DELETE FROM "RolePermissions" 
        WHERE "RoleId" = emp_role_id 
          AND "Resource" IN (14, 15); -- 14 = Customers, 15 = Suppliers
    END IF;
END $$;
