# TODO — Próximos pasos

> Notas de trabajo para continuar en una próxima sesión. No es un diseño final, es la base de la idea para no perder el hilo.

**Orden acordado con el usuario (2026-07-28): primero terminar de cerrar bien el flujo Purchasing → Receiving → Inventory que ya existe (puntos 1-4 abajo); recién después entrar a Facturación/Contabilidad (punto 6). Costeo de recetas (punto 5) es una rama aparte, no bloquea ni depende de lo anterior. Los puntos 7-10 son un backlog adicional (2026-07-28) — ideas con investigación de mercado detrás, no necesariamente para implementar todas; quedan anotadas para no perderlas.**

---

## Completados (dejados aquí como registro, no como pendiente)

- **Albarán (Goods Receipts)** — **construido 2026-07-26.** Ver `.claude/GoodsReceiptsModule.md`. Cubre exactamente lo que la nota original de este archivo pedía: documento separado del Pedido, referencia a `PurchaseOrderLine` sin mutarlo, y el split contable con `QuantityAccepted`/`QuantityCourtesy`/`QuantityRejected`. `PurchaseOrder.Status` ya llega a `PARTIALLY_RECEIVED`/`RECEIVED`.
- **Seed data de precios de Articles** — resuelto en algún punto posterior a esta nota; verificado en vivo (`sqlcmd`): 26 filas en `ArticlePrices`, suficientes para que Orders resuelva precio de contrato/global en las pruebas de extremo a extremo ya hechas.
- **Módulo Pedidos / Orders** — backend **y frontend** completos (el frontend era lo único marcado pendiente en la nota original). Ver `.claude/OrdersModule.md` + `.claude/OrdersGoodsReceiptsOverview.md`. Desde entonces el módulo creció con Rectificaciones, Aprobaciones, PDF/email, numeración secuencial, copia e importación de líneas — todo documentado en sus propios `.claude/*Module.md`.
- **Inventory** (`StockLevels`+`InventoryMovements`, Receipt/Adjustment/Transfer) — construido 2026-07-27, ver `.claude/InventoryModule.md`. Cierra el ciclo Purchasing → Receiving → Inventory que esta nota daba por "familia futura" en la sección de contexto de abajo.

**Factura** sigue sin construir en absoluto — ver el punto 6 más abajo, que retoma esto.

---

## 1. Niveles de par / reposición sugerida (prioridad alta)

Investigado 2026-07-28 contra BirchStreet y Adaco (los dos competidores directos que este mismo codebase ya usa como benchmark en `.claude/GoodsReceiptsModule.md`/`ConsolidatedPurchaseOrderModule.md`): ambos tienen esto como **la** característica central de su propuesta de valor para hostelería — par-levels (a veces atados a ocupación proyectada) disparan reposición predictiva, reduciendo compra de emergencia a precio inflado y desperdicio de perecederos.

**Por qué es barato construirlo ahora:** ya existe casi toda la infraestructura — `StockLevels` (balance por Warehouse+Article), `Warehouse.IsInventoriable`, y todo el flujo `Order`→`PurchaseOrder`. Falta solo una capa fina encima:
- Un `MinimumStockLevel`/`ReorderQuantity` opcional por `(Warehouse, Article)` — probablemente una tabla nueva pequeña, no una columna en `StockLevels` (que es un balance calculado, no configuración).
- Un endpoint/vista que compare contra `StockLevels.QuantityOnHand` y devuelva una lista de "artículos por debajo de par" — no un Order automático, mantiene control humano (mismo principio que Order Templates: "aplica, no auto-ejecuta").
- Frontend: una vista más en la página de Inventory ("Below par") o un botón para sembrar un Order nuevo desde esa lista.

No hay diseño de schema/SPs todavía — es la idea, no el plan.

---

## 2. Scorecard de proveedor (mejor ROI — casi cero captura nueva)

Investigado 2026-07-28: los estándares 2026 de la industria convergen en 5 KPIs — on-time delivery (~30% peso), on-time-in-full (~25%), tasa de rechazo de calidad (~25%, benchmark <2%), cumplimiento de lead time (~15%), exactitud de facturación (~5%).

**Lo notable: InnNou ya captura cada uno de estos datos, solo no los agrega en ningún lado:**
- OTD/lead time → `PurchaseOrder.SentUtc` vs `GoodsReceipt.CreatedUtc`.
- Tasa de rechazo → `GoodsReceiptLine.QuantityRejected`/`RejectionReason` (con el 3-way split que ya distingue courtesy de rejected, más fino que la mayoría del mercado).
- Variación de precio → historial de `PurchaseOrderLineRectification`.

Sería un módulo puramente de reporting: una SP de agregación por `(Supplier, período)` + una página nueva en el detalle del Supplier — sin tocar el modelo de datos existente. El tipo de feature que se hace en días, no semanas, con el mismo precedente de "agregar sobre datos ya existentes" que `ConsolidatedPurchaseOrderModule.md` ya sentó.

---

## 3. Devoluciones a proveedor / RMA (cierra un gap real, no un módulo nuevo desde cero)

Investigado 2026-07-28. Confirmado como práctica estándar: un RMA completo cubre restock/reemplazo/reparación/inspección/descarte, y genera nota de crédito hacia el proveedor con seguimiento hasta que el caso se cierra (reemplazo entregado, crédito aplicado). Access Procure Wizard Evo (hospitality-específico) lo vende explícitamente como "recuperación de crédito perdido".

**Por qué esto es "cerrar el flujo actual", no un módulo nuevo:** `GoodsReceiptLine.QuantityRejected`/`RejectionReason` ya capturan *que* algo llegó mal — pero hoy ese número no dispara nada más. `Warehouse.CanReceiveReturns` ya existe como capability bit desde el módulo de Warehouses (2026-07-15) y sigue **deliberadamente sin conectar** (confirmado en `.claude/GoodsReceiptsModule.md`). Lo que falta es la mitad que le da cierre real al rechazo:
- Una entidad `SupplierReturn`/`SupplierReturnLine` (o extender `GoodsReceiptLine` con un estado de seguimiento) referenciando la línea rechazada, con `Status` (`PENDING`/`CREDITED`/`REPLACED`/`CLOSED`) — Id-backed, siguiendo la convención ya establecida.
- Ninguna nota de crédito fiscal real todavía (eso ya es parte de Facturación, punto 6) — en esta fase solo trackear "se devolvió, se prometió crédito/reemplazo, se cerró", sin contabilidad detrás.

---

## 4. Conteo cíclico / stocktake estructurado (cierra otro gap real en Inventory)

Investigado 2026-07-28. Confirmado como práctica estándar de la industria (WISK, Apicbase, Fast Inventory): conteo cíclico (contar una porción del catálogo por vez, sin cerrar el almacén) es el método preferido sobre un conteo total anual; el flujo típico es contar físicamente, comparar contra el "teórico" (el balance del sistema), y reconciliar posteando **un solo ajuste por variance con trazabilidad completa**, no ediciones sueltas.

**Por qué esto es "cerrar el flujo actual", no un módulo nuevo:** `InventoryService.CreateAdjustmentAsync` (`.claude/InventoryModule.md`) ya cubre el ajuste manual línea por línea — lo que falta es la envoltura de **sesión de conteo**:
- Un `InventoryCount`/`InventoryCountLine` (header + líneas) por Warehouse: se abre una sesión, se lista el subconjunto de artículos a contar (todos, o filtrado por Family/Category), se registra `QuantityCounted` por línea, y al cerrar la sesión el sistema calcula `Variance = QuantityCounted - QuantityOnHand` y postea un `ADJUSTMENT` por cada línea con variance ≠ 0 — reusando `sp_StockLevel_ApplyDelta`/`sp_InventoryMovement_Create` tal cual existen hoy, no una ruta de escritura nueva.
- Esto es genuinamente la pieza que falta para que Inventory sea "cerrable" en la práctica (hoy si alguien hace un conteo físico completo, tendría que crear N ajustes manuales sueltos sin ningún registro de que fueron parte del mismo conteo).

---

## 5. Costeo de recetas / menu engineering (la más diferenciadora, mayor alcance — rama aparte)

Investigado 2026-07-28: esto es lo que separa un ERP de compras genérico de un producto pensado específicamente para hostelería — Apicbase (citizenM, Penta Hotels), Ratatool y Resort Software lo tratan como núcleo: costo por plato = suma de costos de ingredientes ponderados por cantidad, comparado contra precio de venta → % food cost, y con eso "menu engineering" (qué platos reprecificar/rediseñar/retirar según margen vs. popularidad).

Requeriría entidades nuevas (`Recipe`/`RecipeIngredient` referenciando `Article`+`ArticlePrice`, bajando a la unidad de consumo real vía `ArticlePackagingLevel`, no solo `PurchaseUnitId`) — es la pieza que más se apalanca en el trabajo ya hecho de packaging levels e historial de precios, pero también la de mayor esfuerzo de todas. No depende de 1-4 ni de 6 — puede entrar cuando convenga, independiente del resto del roadmap.

---

## 6. Facturación / Contabilidad — 3-way matching PO↔Recepción↔Factura (siguiente capítulo, después de 1-4)

Adaco lo destaca explícitamente como feature. InnNou tiene Pedido+Albarán pero ningún concepto de Factura/Accounting todavía — "Facturas" en el menú lateral sigue siendo solo una etiqueta placeholder, sin servicio/tabla/endpoint detrás (igual que "Ventas"/"Inventario" cuando se escribió la nota original de este archivo). Necesitaría su propio módulo de facturas con numeración fiscal secuencial (IVA en España, IGI en Andorra), matching contra `PurchaseOrderLine`/`GoodsReceiptLine`, y probablemente conectar con las devoluciones del punto 3 para las notas de crédito reales. **Confirmado con el usuario: se quiere construir, pero recién después de cerrar los puntos 1-4.**

---

## 7. Pronóstico de demanda / par levels dinámicos por consumo histórico (evolución del punto 1)

Investigado 2026-07-28: STAR Systems (AINE) y Controliza cruzan ocupación reservada/histórica (feed de PMS) + consumo histórico (feed de POS) para predecir demanda de F&B con >92% de precisión reportada, y disparar sugerencias de compra antes de quedarse corto — un par level *dinámico*, no un número fijo configurado a mano.

**Por qué es una evolución del punto 1, no algo aparte:** InnNou no tiene integración con PMS ni POS todavía (no hay módulo de Consumo/`CONSUMPTION` en `InventoryMovementTypes` — confirmado como explícitamente fuera de alcance en `.claude/InventoryModule.md`, "no hay driver real todavía"). Pero una versión más simple y ya alcanzable con lo que existe: usar el historial de `OrderLine.Quantity` por artículo/warehouse a lo largo del tiempo como proxy de consumo, y sugerir (no fijar) un `MinimumStockLevel` calculado en vez de puramente manual. Verdadera integración con PMS/POS es una fase muy posterior — anotarla aquí para no perder la idea, no para construirla pronto.

---

## 8. Dashboard de spend analytics — presupuesto vs. real por Family/Warehouse/Organización

Investigado 2026-07-28: los procurement dashboards 2026 (Spendflo, ProcureDesk, Suplari) convergen en un patrón: vista ejecutiva (spend total, variación de presupuesto), vista por categoría (spend por categoría/proveedor, dónde se sobrepasa el presupuesto), y drill-down hasta la transacción individual.

**Por qué es casi gratis con lo que ya existe:** `FamilyApprovalThreshold` ya modela un límite de gasto por `(Organization, Family, Level)`, y `OrderApprovalStep` ya congela `ActualFamilyAmount` en el momento de cada submit que cruzó un umbral — es decir, InnNou **ya tiene una serie histórica de gasto real vs. límite configurado**, solo que hoy vive dispersa en pasos de aprobación individuales, no agregada en ningún dashboard. Sumado a `PurchaseOrderLine`/`OrderLine` (montos reales por línea, con `Family`/`SubCategory` vía la clasificación de artículos), un dashboard de "gasto del mes por Family, comparado contra el umbral configurado" es una SP de agregación + una página, mismo patrón de bajo costo que el punto 2.

---

## 9. Escaneo de código de barras — recepción y conteo cíclico (complementa Goods Receipts y el punto 4)

Investigado 2026-07-28: confirmado como estándar en almacenes modernos — escanear en vez de buscar por nombre acelera tanto la recepción (Goods Receipts) como el conteo físico, actualiza cantidades en tiempo real al escanear, y funciona incluso desde un teléfono normal sin hardware dedicado para operaciones chicas/medianas.

**Por qué es barato y de alto impacto operativo:** `Article.Barcode` **ya existe como columna**, capturada desde el alta del artículo, pero hoy no la usa ningún flujo — ni Goods Receipts ni (cuando exista) el conteo cíclico del punto 4 la aprovechan para buscar el artículo más rápido que tipeando su nombre. No requiere una app nativa: un input que acepte el foco de un lector de código de barras USB/Bluetooth (que se comporta como teclado) alcanza para la mayoría de los casos de uso de almacén de un hotel — una app móvil dedicada sería una fase posterior, no un requisito para el primer valor.

---

## 10. Gestión de contratos con proveedor — vigencia, renovación, alertas

Investigado 2026-07-28: el software de contract management 2026 (Juro, Procurify, Graphite Connect) se centra en tres cosas: alertas antes de que un contrato/acuerdo de precio venza, visibilidad de los términos negociados para evitar "maverick spend" (comprar fuera de lo acordado), y comparación estructurada entre proveedores.

**Por qué esto cierra un gap real, no es un módulo nuevo desde cero:** `ArticlePrice` con `OrganizationId` seteado ya modela el "precio de contrato" (gana sobre el precio global en la misma fecha), pero es insert-only sin ningún concepto de vigencia — no hay `EffectiveUntil`/fecha de vencimiento, ni alerta de "este contrato vence en 30 días". Una extensión natural: agregar un campo opcional de vigencia a `ArticlePrice` (o una entidad `SupplierContract` más rica si se quiere guardar términos además del precio), y una vista/alerta de "contratos por vencer" — mismo principio de bajo esfuerzo que los puntos 2 y 8, apalancado sobre un modelo que ya existe.

---

## Contexto relevante ya existente (no reinventar)

- `Warehouse` ya modela las capabilities relevantes para todo lo de arriba (`IsInventoriable`, `CanAdjustInventory`, `CanTransferOut`, `CanReceiveTransfers`, `CanReceivePurchases`, `CanReceiveReturns`, etc.) — cualquier feature nueva debería apoyarse en estas, no crear una clasificación paralela.
- El patrón de shadow-user + impersonation ya está resuelto (`SupplierAccessModule.md`, `WarehousesModule.md`) — no hace falta un mecanismo nuevo si alguna de estas ideas necesita un actor nuevo (ej. un "responsable de compras" que revise el scorecard).
- `ArticlePackagingLevel` (N niveles ordenados, `IsDefinedUnit` marca el nivel final) es exactamente lo que el costeo de recetas necesitaría para bajar de `PurchaseUnitId` a la unidad de consumo real — ver `.claude/ArticlePackagingModule.md`.
- Los códigos de error e Id-backed lookups siguen la receta ya documentada en CLAUDE.md ("Status/type fields are Id-backed") — cualquier `Status` nuevo (RMA, sesión de conteo) debe seguirla desde el día uno, no empezar como `varchar` CHECK-constrained.
