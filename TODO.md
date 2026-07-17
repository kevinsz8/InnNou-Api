# TODO — Próximos pasos

> Notas de trabajo para continuar en una próxima sesión. No es un diseño final, es la base de la idea para no perder el hilo.

---

## 1. Seed data de precios de Articles (para pruebas)

- Generar `ArticlePrices` para artículos existentes — **al menos para los Suppliers de tipo `PRODUCT`** (no `SERVICE`, ni por ahora `MIXED`), ya que el modelo de precio fijo por unidad/precio-por-catálogo no aplica bien a servicios variables (ver CLAUDE.md, sección "Supplier type" — esto sigue siendo una decisión de diseño pendiente y deliberadamente diferida).
- Objetivo: tener data realista de precios para poder probar el flujo de Pedidos/Orders (una línea de pedido necesita poder resolver un precio vigente del artículo).
- Pendiente de definir antes de generar el seed:
  - ¿Cuántos artículos / qué suppliers?
  - ¿Precios globales (`OrganizationId = NULL`) o precios de contrato por organización, o ambos?
  - ¿Qué monedas? (recordar: `ArticlePrices` es multi-moneda sin conversión FX, cada fila es una moneda independiente)
  - ¿Vía script SQL directo, vía el endpoint `POST /articlePrices/create`, o vía un mini bulk-import?

---

## 2. Módulo Pedidos / Orders — arrancar la base

Este es uno de los primeros procesos "core" del negocio. Todavía no hay diseño de datos ni de permisos definitivo — esto es la idea inicial tal como se discutió, para no perderla.

### Quién puede crear un pedido
- Usuarios de **organización hija** (Asociado — no Super Asociado) dentro de su propia organización.
- Usuarios de **Warehouse** (shadow user de `WarehouseContact`, rol `WAREHOUSE`).

### El flujo es un poco engañoso (como en otros sistemas parecidos)
En apariencia, para crear un pedido hay que:
1. Suplantar (impersonate) a la organización hija.
2. Al entrar a la sección de Pedidos, el sistema pide seleccionar un **Warehouse** de esa organización.
3. Pero en el fondo, **es el Warehouse quien realmente "hace" el pedido**, no la organización en sí — el pedido queda asociado/creado por el Warehouse (mismo espíritu que ya existe hoy: un `WarehouseContact` impersonado tiene su propio shadow `User` con `OrganizationId` = el de su Warehouse padre).

### La parte "tricky" a resolver más adelante
Si quiero hacer un pedido para **otro** Warehouse (de la misma organización o de otra), hoy en día tendría que:
- Salir y volver a impersonar el otro Warehouse (vía su `WarehouseContact`), **o**
- De alguna manera cambiar/seleccionar el nuevo Warehouse dentro de la misma sesión, sin tener que salir y re-impersonar.

Hay que decidir cuál de estos dos flujos (o ambos) va a soportar el sistema. Pendiente de diseño — no resolver todavía, solo dejar anotado.

### Lo que falta definir (para cuando retomemos esto en serio)
- Modelo de datos: `Order`/`OrderLine` (o el nombre que se decida), estados (borrador, enviado, aprobado, recibido, cancelado, etc.), relación con `Article`/`ArticlePrice`/`Warehouse`/`Supplier`/`Organization`.
- Snapshot de las líneas: recordar la decisión ya tomada para PO/GR/Inventory — cada línea de pedido debe guardar un **FK a `ArticleId` + snapshot de los campos estructurales y de precio** (unidad de compra, cantidad, unidad de contenido, precio) en el momento de creación, nunca depender de un join en vivo a `Articles`/`ArticlePrices` para el cálculo (ver memoria `project_article_versioning_and_line_snapshot_design` / CLAUDE.md "Article structural versioning").
- Permisos exactos: `RoleLevel`, pertenencia a organización/warehouse, quién puede ver/editar/cancelar/aprobar.
- Flujo de aprobación si aplica — `Warehouse.RequireApproval` ya existe como capability bit, probablemente hay que usarlo acá.
- A qué Warehouse(s) le puede llegar un pedido — relación con `Suppliers`, ¿el pedido es por Supplier o multi-supplier?
- Frontend (`InnNou-Web`): nueva página Orders, flujo de selección de Warehouse post-impersonation, cómo resolver el caso de "quiero pedir para otro warehouse" (ver arriba).
- `PurchaseOrders`/`GoodsReceipts`/`Inventory`/`HotelArticleConfig` (renombrado `OrganizationArticles`) siguen sin reconstruirse desde que se borraron el 2026-06-26 — Orders probablemente es el primer paso de esta familia de módulos, ver memoria `project_purchasing_inventory_impl`.

---

## Contexto relevante ya existente (no reinventar)

- `Warehouse` ya modela las capabilities relevantes (`CanReceivePurchases`, `CanTransferOut`, `CanConsumeInventory`, `RequireApproval`, `IsDefaultReceivingWarehouse`, etc.) — Orders debería apoyarse en estas, no crear una clasificación paralela.
- El patrón de shadow-user + impersonation ya está resuelto para `WarehouseContact` (`WAREHOUSE` role, `RoleLevel = 20`) — la sección "tricky" de arriba es sobre la *experiencia de uso* de ese patrón ya existente para el caso concreto de Orders, no sobre crear un nuevo mecanismo de impersonation.
- `ArticlePrices` (insert-only, multi-currency, global vs. contrato por organización) ya está implementado — Orders es el primer consumidor real de esa data.
