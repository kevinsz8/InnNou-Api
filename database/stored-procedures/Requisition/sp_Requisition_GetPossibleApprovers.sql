SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
-- =============================================================
-- REQUISITION - GET POSSIBLE APPROVERS
-- Called right after a Requisition is created, to fan out the
-- Requisition_Requested notification. Mirrors RequisitionService's own
-- CanManageOrganizationAsync approval-eligibility rule exactly: RoleLevel >=
-- 20 (Staff+), same Organization as the Requisition (Associate orgs have no
-- children today, so no hierarchy walk is needed), and warehouse-scope
-- filtered (a WarehouseContact's own login only qualifies for their own
-- Warehouse; every other role is unscoped). @ExcludeUserToken is always the
-- requester themselves -- they can never approve their own Requisition
-- (REQUISITION_CANNOT_APPROVE_OWN), so there's no point notifying them as a
-- possible approver. Takes the token (not the internal UserId) since the
-- caller already has it on hand (context.ActorUserToken) and would otherwise
-- need an extra round trip just to resolve it.
-- =============================================================
CREATE OR ALTER PROCEDURE sp_Requisition_GetPossibleApprovers
    @OrganizationId    INT,
    @WarehouseId       INT,
    @ExcludeUserToken  UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT u.UserId, u.UserToken
    FROM   Users u
    JOIN   Roles r ON r.RoleId = u.RoleId
    LEFT JOIN WarehouseContacts wc ON wc.WarehouseContactId = u.WarehouseContactId
    WHERE  u.IsDeleted = 0
      AND  u.IsActive  = 1
      AND  u.OrganizationId = @OrganizationId
      AND  r.RoleLevel >= 20
      AND  (wc.WarehouseId IS NULL OR wc.WarehouseId = @WarehouseId)
      AND  (@ExcludeUserToken IS NULL OR u.UserToken <> @ExcludeUserToken);
END;
GO
