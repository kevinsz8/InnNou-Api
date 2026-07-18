# TODO — Próximos pasos

> Notas de trabajo para continuar en una próxima sesión. No es un diseño final, es la base de la idea para no perder el hilo.

---

## 1. Albaranes (Goods Receipts) y Facturas — pendientes, no confundir con Pedidos

Investigado 2026-07-18: en España/Andorra el flujo típico de compras son 3 documentos distintos, no uno solo:

- **Pedido** (orden de compra) — la solicitud enviada al proveedor: qué se quiere, cantidades, precio pactado. **Esto ya lo tenemos** — es exactamente `Order`/`PurchaseOrder` (ver sección "Módulo Pedidos / Orders" más abajo).
- **Albarán (de entrega)** — el documento que acompaña físicamente la mercancía al llegar. Refleja lo que **realmente se recibió** (puede diferir del pedido: entregas parciales, roturas, sustituciones, backorders), normalmente firmado por quien recibe, y dispara la actualización de stock. Un pedido puede generar varios albaranes (entregas parciales en el tiempo). **No lo tenemos construido** — es el módulo **Goods Receipts**, todavía pendiente. `PurchaseOrder.Status` en V1 solo llega a `Sent`/`Cancelled`, no hay `Received`/`PartiallyReceived` (confirmado en `.claude/OrdersModule.md`, que ya reserva campos como `PurchaseOrderLine.LastUpdatedUtc`/`LastUpdatedBy` para cuando este módulo exista).
- **Factura** — el documento fiscal/de facturación (IVA en España, IGI en Andorra, numeración secuencial obligatoria). Suele agrupar varios albaranes (p. ej. facturación mensual). **No existe en absoluto** en el código — "Facturas" en el menú lateral es solo una etiqueta placeholder sin servicio/tabla/endpoint detrás, igual que "Ventas"/"Inventario".

**Anotado aquí arriba con prioridad a propósito, aunque en la práctica probablemente se implemente después de todo lo demás que ya tengo en mente** (no listado todavía en este archivo) — el objetivo de esta nota es solo dejar constancia de que Pedidos ≠ Albaranes ≠ Facturas, y que los dos últimos siguen totalmente por construir, para no dar por hecho que "recepción" o "facturación" ya están cubiertas por el módulo de Pedidos.

---

## 2. Seed data de precios de Articles (para pruebas)

- Generar `ArticlePrices` para artículos existentes — **al menos para los Suppliers de tipo `PRODUCT`** (no `SERVICE`, ni por ahora `MIXED`), ya que el modelo de precio fijo por unidad/precio-por-catálogo no aplica bien a servicios variables (ver CLAUDE.md, sección "Supplier type" — esto sigue siendo una decisión de diseño pendiente y deliberadamente diferida).
- Objetivo: tener data realista de precios para poder probar el flujo de Pedidos/Orders (una línea de pedido necesita poder resolver un precio vigente del artículo).
- Pendiente de definir antes de generar el seed:
  - ¿Cuántos artículos / qué suppliers?
  - ¿Precios globales (`OrganizationId = NULL`) o precios de contrato por organización, o ambos?
  - ¿Qué monedas? (recordar: `ArticlePrices` es multi-moneda sin conversión FX, cada fila es una moneda independiente)
  - ¿Vía script SQL directo, vía el endpoint `POST /articlePrices/create`, o vía un mini bulk-import?

---

## 3. Módulo Pedidos / Orders — implementado (backend), 2026-07-17

**Backend completo, ver `.claude/OrdersModule.md`.** `Order`/`OrderLine`/`PurchaseOrder` (tablas, SPs, servicios, endpoints `/orders` y `/purchaseOrders`) implementados y verificados en vivo end-to-end: crear pedido → agregar líneas de 2 suppliers distintos (una resolvió correctamente el precio de contrato sembrado sobre el global) → submit → split en 2 `PurchaseOrder` → mutación post-submit rechazada con `ORDER_NOT_DRAFT` → cancelar → doble cancelación rechazada con `PURCHASE_ORDER_NOT_SENT`.

**Falta:** el frontend (`InnNou-Web` — página `Orders.tsx` + hooks/services). Resumen de las decisiones clave del diseño (ya construidas):

- Modelo en dos niveles: `Order` (carrito, multi-supplier, org/warehouse) → al confirmarse (`Submit`) se divide automáticamente en un `PurchaseOrder` por cada Supplier distinto entre sus líneas (1 supplier por `PurchaseOrder`, ahí sí). `PurchaseOrder` es directamente el módulo "Purchase Orders" que `.claude/ArticlesModule.md` dejaba como futuro — no es un módulo aparte.
- La parte "tricky" de elegir/cambiar Warehouse quedó resuelta: `WarehouseToken` es siempre un parámetro explícito del request (nunca una identidad a impersonar), validado con el mismo chequeo de jerarquía que ya usa `WarehouseService`. Cambiar de warehouse dentro de la misma sesión es solo elegir otro valor en el dropdown — no hace falta re-impersonar. Un `WarehouseContact` logueado cumple la regla trivialmente (su organización ya es la del warehouse padre).
- `OrderLine` usa el patrón FK + snapshot ya acordado (`PurchaseUnitId`/`PurchaseQuantity`/`ContentUnitId`/`ContentQuantity`/precio congelados al agregar la línea).
- V1 usa el flujo mínimo: `PurchaseOrder` nace directo en `SENT`, sin aprobación ni recepción — eso queda para cuando se construya Goods Receipts.
- Sigue siendo el primer paso de la familia `PurchaseOrders`/`GoodsReceipts`/`Inventory`/`HotelArticleConfig` (`OrganizationArticles`) que se borró el 2026-06-26 y no se reconstruyó — ver memoria `project_purchasing_inventory_impl`. `OrganizationArticles` en particular quedó deliberadamente diferida, ver memoria `project_organization_articles_deferred`.

**Próximo paso real:** implementar el schema + servicios + endpoints descritos en `.claude/OrdersModule.md` (tiene la lista de SPs y endpoints sugeridos al final del doc).

---

## Contexto relevante ya existente (no reinventar)

- `Warehouse` ya modela las capabilities relevantes (`CanReceivePurchases`, `CanTransferOut`, `CanConsumeInventory`, `RequireApproval`, `IsDefaultReceivingWarehouse`, etc.) — Orders debería apoyarse en estas, no crear una clasificación paralela.
- El patrón de shadow-user + impersonation ya está resuelto para `WarehouseContact` (`WAREHOUSE` role, `RoleLevel = 20`) — la sección "tricky" de arriba es sobre la *experiencia de uso* de ese patrón ya existente para el caso concreto de Orders, no sobre crear un nuevo mecanismo de impersonation.
- `ArticlePrices` (insert-only, multi-currency, global vs. contrato por organización) ya está implementado — Orders es el primer consumidor real de esa data.
