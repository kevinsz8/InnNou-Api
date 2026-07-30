# TODO — Próximos pasos

> Notas de trabajo para continuar en una próxima sesión. No es un diseño final, es la base de la idea para no perder el hilo.

**Orden acordado con el usuario (2026-07-28): primero terminar de cerrar bien el flujo Purchasing → Receiving → Inventory que ya existe (puntos 1-3 abajo); recién después entrar a Facturación/Contabilidad (punto 5). Costeo de recetas (punto 4) es una rama aparte, no bloquea ni depende de lo anterior. Los puntos 6-9 son un backlog adicional (2026-07-28) — ideas con investigación de mercado detrás, no necesariamente para implementar todas; quedan anotadas para no perderlas.**

---

## Completados (dejados aquí como registro, no como pendiente)

- **Albarán (Goods Receipts)** — **construido 2026-07-26.** Ver `.claude/GoodsReceiptsModule.md`. Cubre exactamente lo que la nota original de este archivo pedía: documento separado del Pedido, referencia a `PurchaseOrderLine` sin mutarlo, y el split contable con `QuantityAccepted`/`QuantityCourtesy`/`QuantityRejected`. `PurchaseOrder.Status` ya llega a `PARTIALLY_RECEIVED`/`RECEIVED`.
- **Seed data de precios de Articles** — resuelto en algún punto posterior a esta nota; verificado en vivo (`sqlcmd`): 26 filas en `ArticlePrices`, suficientes para que Orders resuelva precio de contrato/global en las pruebas de extremo a extremo ya hechas.
- **Módulo Pedidos / Orders** — backend **y frontend** completos (el frontend era lo único marcado pendiente en la nota original). Ver `.claude/OrdersModule.md` + `.claude/OrdersGoodsReceiptsOverview.md`. Desde entonces el módulo creció con Rectificaciones, Aprobaciones, PDF/email, numeración secuencial, copia e importación de líneas — todo documentado en sus propios `.claude/*Module.md`.
- **Inventory** (`StockLevels`+`InventoryMovements`, Receipt/Adjustment/Transfer) — construido 2026-07-27, ver `.claude/InventoryModule.md`. Cierra el ciclo Purchasing → Receiving → Inventory que esta nota daba por "familia futura" en la sección de contexto de abajo.
- **Niveles de par / reposición sugerida** — **construido 2026-07-28**, ver `.claude/ParLevelsModule.md`. Base par level por Warehouse+Article, más overrides de temporada (rango mes/día recurrente, con wrap-around de fin de año) y de evento puntual (fecha literal, prioridad EVENT > SEASONAL > BASE) — ambos confirmados en la conversación de diseño, incluido el override de evento que el usuario pidió meter desde el arranque aunque no todos lo usen. `LeadTimeDays` se muestra tal cual en la lista "Below Par", sin inventar un score de urgencia (no hay datos de consumo todavía). Verificado en vivo con Playwright: resolución de prioridad, rechazo de overlap del mismo tipo, aceptación de overlap cruzado, y cascade-delete del base hacia sus overrides.
- **Aperturas de inventario / Inventory Periods** — **construido 2026-07-27** (backend + frontend), ver `.claude/InventoryPeriodsModule.md`. Reemplaza y amplía lo que el punto 3 de abajo sugería: en vez de un conteo cíclico parcial sin cerrar el almacén, el usuario pidió una máquina de estados por Warehouse (`OPEN → IN_PROGRESS → PRE_CLOSED → CLOSED`, conteo completo obligatorio para cerrar, reapertura solo del período más reciente) — esto resolvió de paso el problema original de "reportes por fecha" sin necesitar ningún mecanismo de congelamiento por fecha (el cierre siempre es "ahora"). Verificado en vivo, primero por API (curl) y luego por navegador (Playwright, una vez `InnNou-Web` estuvo disponible en una sesión de seguimiento): apertura, transición automática de estado, freeze de Ajustes/Transferencias durante el conteo, cierre con variance real y reversión al reabrir, todo confirmado también desde la nueva pestaña "Periods". Commiteado y pusheado (Api d0d5ea7, Web 4010de9).
- **Dashboard / Home con datos reales** — **construido 2026-07-29** (backend + frontend), ver `.claude/DashboardModule.md`. Reemplaza el `/dashboard` 100% mockeado de `InnNou-Web` por datos reales — pedido explícitamente como módulo aislado (servicio, SPs y endpoint propios, sin reutilizar ni tocar ningún SP/servicio existente, aunque varios estaban cerca de servir) porque son solo lecturas y así el dashboard nunca puede desestabilizar un flujo real al evolucionar por separado. "Overdue receipts" (sin dato real detrás, no existe fecha de entrega esperada en el esquema) se reemplazó por "Artículos bajo mínimo" (mismo número de Below Par). Verificado cruzando cada número contra `sqlcmd` directo, tanto en vista global (SuperAdmin sin impersonar) como escopeada por organización — coincidencia exacta en ambos casos. Sin cambios de esquema. No commiteado.
- **Supplier ya no puede tocar nada del flujo Comanda→SubComanda→Albarán** — **hecho 2026-07-28**. Se sacó el bypass que `PurchaseOrder.CancelAsync` tenía para que un Supplier cancelara su propia PO directamente (Rectify/GoodsReceipt nunca lo tuvieron) — confirmado con el usuario que este sistema es 100% del lado comprador, el Supplier solo gestiona catálogo/precios y ve sus propios pedidos en modo lectura. Commiteado y pusheado (Api 79baa15, Web 692f329).
- **"Caso A" — Rectificar una PO parcialmente recibida** — **hecho 2026-07-28**. Cierra el gap de "el proveedor no va a mandar el resto de una línea ya parcialmente recibida": `CreateRectificationAsync` ahora acepta `PARTIALLY_RECEIVED` además de `SENT`, con un piso que nunca deja bajar la cantidad rectificada por debajo de lo ya `Accepted` (ni cancelar una línea que ya tiene algo recibido) — `PURCHASE_ORDER_RECTIFICATION_BELOW_ACCEPTED`. Si la rectificación deja todas las líneas restantes exactamente en lo ya aceptado, el PO pasa solo a `RECEIVED` (mismo recálculo que usa `CreateGoodsReceiptAsync`). Verificado en vivo con Playwright (bloqueo del piso + cierre automático a RECEIVED) y confirmado por `sqlcmd`. Commiteado y pusheado (Api 655c201, Web 1fa68b2).

**"Caso B" — Cerrar una PO parcialmente recibida como incompleta** — **hecho 2026-07-28**. Completa lo que el Caso A no cubre: cuando el faltante de una PO `PARTIALLY_RECEIVED` **no** es un acuerdo formal con el proveedor (eso lo resuelve el Caso A, rectificando la cantidad) sino que el proveedor simplemente no manda el resto y el comprador deja de perseguirlo. Nuevo status Id-backed `CLOSED_SHORT` (5º valor de `PurchaseOrderStatuses`, agregado sin renumerar) — simétrico a `CANCELLED` pero para "algo sí llegó, pero no todo, y no va a llegar más". No toca ninguna `PurchaseOrderLine`/cantidad — solo cierra la PO (`ClosedShortUtc`/`ClosedShortBy`/`ClosedShortReason`, motivo obligatorio, mismo patrón que `GoodsReceiptLine.RejectionReason`), preservando el faltante como dato real para el futuro Scorecard de proveedor (punto 1, OTIF/tasa de rechazo). Solo disponible desde `PARTIALLY_RECEIVED`, 100% del lado comprador (sin bypass de Supplier, mismo criterio que Cancel/Rectify/Receive). UI: botón "Close as incomplete" en la card de la PO + modal con motivo obligatorio. Verificado en vivo con Playwright (bloqueo de motivo vacío + cierre exitoso) y confirmado por `sqlcmd` (`PurchaseOrderLine.Quantity` intacta). No commiteado.

- **Ronda de UX Pedidos/Detalle de Pedido/Inventario (2026-07-28)** — 6 mejoras puntuales pedidas tras probar el flujo: columna de Acciones (Copy+Edit) y filtro persistente en la lista de Pedidos; layout de dos columnas + filtro Familia/SubFamilia/Categoría/SubCategoría en líneas de Detalle de Pedido; mismo filtro (las 4 dimensiones, decisión explícita del usuario) en Stock/Below Par de Inventario; vista de detalle de un Período de Inventario (clic en fila + botón "View" verde explícito, agregado en una segunda vuelta para que quede más visible); reordenamiento de tabs a Períodos/Bajo Par/Stock; filtro de rango de fechas en el historial de Períodos. De paso se encontró y arregló un bug real: los hooks de filtro mandaban `familyToken: ""` al backend, que un `Guid?` no puede deserializar (400). Verificado en vivo con Playwright. Commiteado y pusheado (Api 21e7752, Web d8f91dd).

**Factura** sigue sin construir en absoluto — ver el punto 5 más abajo, que retoma esto.

---

## 1. Scorecard de proveedor — CONSTRUIDO 2026-07-30

Investigado 2026-07-28: los estándares 2026 de la industria convergen en 5 KPIs — on-time delivery (~30% peso), on-time-in-full (~25%), tasa de rechazo de calidad (~25%, benchmark <2%), cumplimiento de lead time (~15%), exactitud de facturación (~5%).

**Construido 2026-07-30, puramente de reporting, sin tocar el modelo de datos existente** (`sp_Supplier_GetScorecard` + `ISupplierService.GetScorecardAsync` + página nueva `/suppliers/:supplierToken/scorecard`, botón "Scorecard" en la lista de Proveedores). Confirmado con el usuario: 4 números independientes (sin score único ponderado) y selector de fechas libre (sin default fijo, a diferencia del resto de la app) — decisiones tomadas en la conversación de diseño, no solo la investigación de mercado original:
- **Tasa de rechazo de calidad** = `Rechazado / (Aceptado+Cortesía+Rechazado)` sobre `GoodsReceiptLine`, directo.
- **On-Time Delivery** = % de líneas recibidas cuyo `(GoodsReceipt.CreatedUtc - PurchaseOrder.SentUtc)` en días fue ≤ `Article.LeadTimeDays` — primer uso real de ese campo (antes solo se mostraba en "Below Par" sin compararse contra nada), evaluado solo sobre líneas con `LeadTimeDays` configurado (`OtdEligibleLines`).
- **On-Time-In-Full** = a tiempo Y recibida en una sola entrega (sin partials, sin rechazo en esa recepción, cantidad completa) — mismo denominador que OTD.
- **Tiempo de entrega promedio** (días) — número crudo, no un %, como contexto adicional.
- **Exactitud de facturación (~5% del estándar) quedó afuera de este V1** — no existe módulo de Facturación todavía (ver punto 5).

Verificado en vivo (Iberian Food Distribution): números de la UI coinciden exactamente contra `sqlcmd` directo (8% rechazo, 100% OTD, 86.96% OTIF, 0.2 días promedio sobre 23 líneas). Estados "sin datos" nunca fabrican un 0%/100% falso — se distingue "no hay líneas en el período" de "hay líneas pero ninguna tiene lead time configurado". Sin errores de consola. Variación de precio (vía `PurchaseOrderLineRectification`) no se incluyó como KPI formal — no es uno de los 5 estándares de la industria, quedó fuera de alcance de este V1.

---

## 2. Devoluciones a proveedor / RMA — CONSTRUIDO 2026-07-30

Investigado 2026-07-28. Confirmado como práctica estándar: un RMA completo cubre restock/reemplazo/reparación/inspección/descarte, y genera nota de crédito hacia el proveedor con seguimiento hasta que el caso se cierra (reemplazo entregado, crédito aplicado). Access Procure Wizard Evo (hospitality-específico) lo vende explícitamente como "recuperación de crédito perdido".

**Construido 2026-07-30.** Cierra el gap real: `GoodsReceiptLine.QuantityRejected`/`RejectionReason` ya capturaban *que* algo llegó mal, pero no disparaban nada más. `Warehouse.CanReceiveReturns` (capability bit dormido desde 2026-07-15) ahora tiene su primer consumidor real. Diseño acordado con el usuario antes de construir:
- **`SupplierReturn`** (cabecera, referencia a `PurchaseOrder`) + **`SupplierReturnLine`** (referencia a una `GoodsReceiptLine` rechazada específica — una línea rechazada solo puede reclamarse una vez, `UNIQUE` constraint). Una devolución puede agrupar varias líneas rechazadas de distintas Recepciones del mismo Pedido.
- **`Status` de solo 2 valores** (`PENDING`/`CLOSED`) + un **`ResolutionType` separado** (`CREDITED`/`REPLACED`/`WRITTEN_OFF`, seteado solo al cerrar) — decisión explícita del usuario sobre la alternativa de 4 estados planos originalmente sugerida, para no conflatar "¿está abierto?" con "¿cómo se resolvió?".
- **Validación dura en la creación**: rechaza si el `Warehouse` no tiene `CanReceiveReturns` (decisión explícita del usuario — no cualquier almacén puede procesar devoluciones), si una línea ya fue reclamada por otra devolución, o si la lista de líneas viene vacía.
- **100% del lado comprador** — mismo criterio que Recibir/Rectificar, sin acceso de lectura para el Proveedor en este V1.
- Entrada contextual: botón "Devolver" en cada PO de `OrderDetail.tsx` (visible en cualquier estado salvo `CANCELLED` — un rechazo ya ocurrió en el pasado, no depende del estado actual del PO) → página `/purchaseOrders/:token/returns/create` mostrando las líneas rechazadas aún no reclamadas. Seguimiento cruzado en `/supplierReturns` (lista con filtros Organización/Proveedor/Estado/fechas) + `/supplierReturns/:token` (detalle + cerrar). Nueva entrada de menú "Devoluciones" en el grupo Operaciones.
- Ninguna nota de crédito fiscal real — eso sigue siendo parte de Facturación (punto 5); acá solo se trackea el estado del reclamo.

Verificado en vivo (Iberian Food Distribution / PO-2026-00012): creación de devolución con una línea rechazada real, cierre con resolución "Con crédito", aparece correctamente en la lista y el detalle. Verificado por API (curl) el bloqueo por `CanReceiveReturns=0` en otro almacén, y que una línea ya reclamada deja de aparecer como elegible. Sin errores de consola.

---

## 3. Conteo cíclico / stocktake estructurado — COMPLETADO, ver arriba

Investigado 2026-07-28 (WISK, Apicbase, Fast Inventory research original, resumida más abajo). El sketch original de este punto sugería un conteo cíclico parcial sin cerrar el almacén; en la sesión de construcción (2026-07-27→28) el usuario pidió explícitamente algo más completo — una máquina de estados por Warehouse con conteo íntegro obligatorio para cerrar y reapertura controlada — ver **"Aperturas de inventario / Inventory Periods"** en la sección Completados de arriba y `.claude/InventoryPeriodsModule.md` para el diseño final, que reemplaza este sketch.

<details><summary>Investigación original (referencia histórica)</summary>

Confirmado como práctica estándar de la industria: conteo cíclico (contar una porción del catálogo por vez, sin cerrar el almacén) es el método preferido sobre un conteo total anual; el flujo típico es contar físicamente, comparar contra el "teórico" (el balance del sistema), y reconciliar posteando un solo ajuste por variance con trazabilidad completa, no ediciones sueltas. `InventoryService.CreateAdjustmentAsync` ya cubría el ajuste manual línea por línea — lo que faltaba era la envoltura de sesión de conteo, que terminó construyéndose como el diseño más completo de "Inventory Periods" en vez de esta versión más ligera.

</details>

---

## 4. Costeo de recetas / menu engineering (la más diferenciadora, mayor alcance — rama aparte)

Investigado 2026-07-28: esto es lo que separa un ERP de compras genérico de un producto pensado específicamente para hostelería — Apicbase (citizenM, Penta Hotels), Ratatool y Resort Software lo tratan como núcleo: costo por plato = suma de costos de ingredientes ponderados por cantidad, comparado contra precio de venta → % food cost, y con eso "menu engineering" (qué platos reprecificar/rediseñar/retirar según margen vs. popularidad).

Requeriría entidades nuevas (`Recipe`/`RecipeIngredient` referenciando `Article`+`ArticlePrice`, bajando a la unidad de consumo real vía `ArticlePackagingLevel`, no solo `PurchaseUnitId`) — es la pieza que más se apalanca en el trabajo ya hecho de packaging levels e historial de precios, pero también la de mayor esfuerzo de todas. No depende de 1-3 ni de 5 — puede entrar cuando convenga, independiente del resto del roadmap.

---

## 5. Facturación / Contabilidad — 3-way matching PO↔Recepción↔Factura (siguiente capítulo, después de 1-3)

Adaco lo destaca explícitamente como feature. InnNou tiene Pedido+Albarán pero ningún concepto de Factura/Accounting todavía — "Facturas" en el menú lateral sigue siendo solo una etiqueta placeholder, sin servicio/tabla/endpoint detrás (igual que "Ventas"/"Inventario" cuando se escribió la nota original de este archivo). Necesitaría su propio módulo de facturas con numeración fiscal secuencial (IVA en España, IGI en Andorra), matching contra `PurchaseOrderLine`/`GoodsReceiptLine`, y probablemente conectar con las devoluciones del punto 2 para las notas de crédito reales. **Confirmado con el usuario: se quiere construir, pero recién después de cerrar los puntos 1-3.**

### Sub-diseño: modelo de Impuestos/IVA — investigado y acordado 2026-07-30, guardado aquí, NO implementar todavía

Investigado 2026-07-30: España no tiene una tasa única de IVA — 21% general, 10% reducido (hostelería, agua, pastas/aceites de semilla), 4% superreducido (pan/leche/fruta/huevos, aceite de oliva desde 2025), más regímenes regionales que **no son IVA** (IGIC en Canarias, IPSI en Ceuta/Melilla, cada uno con su propia estructura de tasas). Andorra usa IGI, con una estructura totalmente distinta: 4,5% general, 1% reducido, 0% superreducido, 2,5% especial, 9,5% incrementado. Confirmado con el usuario: **los regímenes regionales españoles entran en el alcance desde el arranque**, no se dejan para después.

Con regímenes regionales en alcance, un `Articles.TaxRateId` único no alcanza — el mismo artículo puede recibirse en un almacén en Madrid (IVA) y en otro en Tenerife (IGIC), con tasas distintas. La separación correcta (así lo resuelven los motores de impuestos reales — Avalara, y el propio enfoque de la UE de "categorías reducidas" que cada país mapea a su % local):

- **`TaxCategories`** (a nivel Artículo, independiente del país): `GENERAL / REDUCIDO / SUPERREDUCIDO / EXENTO` — clasifica qué tan esencial/regulado es el artículo, no un porcentaje.
- **`TaxJurisdictions`** (nueva, FK a `Countries` existente): España tendría 4 filas (Península+Baleares, Canarias, Ceuta, Melilla), Andorra 1 sola. Extensible a nuevos países sin tocar código.
- **`TaxRates`**: mapea `(TaxJurisdictionId, TaxCategoryId) → RatePercent`, Id-backed igual que `SupplierTypes`/`Currencies`. Ej: `(Península, SUPERREDUCIDO)=4%`, `(Canarias, SUPERREDUCIDO)=IGIC`, `(Andorra, SUPERREDUCIDO)=0%`.
- **`Articles.TaxCategoryId`** (FK nullable) — se configura una sola vez por artículo, sin importar en qué almacén se reciba.
- **`Warehouses.TaxJurisdictionId`** (FK nullable, nuevo) — determina qué jurisdicción aplica a lo recibido ahí. Concepto distinto de `Warehouses.ZoneId` (cobertura de reparto), aunque mismo patrón Country>subdivisión.
- **Freeze solo en `GoodsReceiptLine`** (confirmado con el usuario — no en `Order`/`PurchaseOrderLine`, que siguen netos): al crear la recepción se resuelve `(Article.TaxCategoryId, Warehouse.TaxJurisdictionId) → TaxRates` y se congela `TaxCategoryId, TaxRateId, TaxRatePercent, TaxableAmount, TaxAmount, TotalAmount` sobre `QuantityAccepted` (nunca Cortesía/Rechazado, que no se facturan) — la futura Facturación solo lee, no recalcula nada.
- Validación dura en `CreateGoodsReceiptAsync` (nunca un IVA en null por descuido): rechazar con código de error claro si el Warehouse no tiene `TaxJurisdictionId`, si un Artículo aceptado no tiene `TaxCategoryId`, o si no existe fila `TaxRates` para esa combinación.
- Superficie nueva no trivial: pantalla de administración para Jurisdicciones/Tasas, asignar categoría a cada Artículo existente, asignar jurisdicción a cada Warehouse existente — confirma que este sub-diseño espera su turno junto con el resto del punto 5.

---

## 6. Pronóstico de demanda / par levels dinámicos por consumo histórico (evolución de los Niveles de Par)

Investigado 2026-07-28: STAR Systems (AINE) y Controliza cruzan ocupación reservada/histórica (feed de PMS) + consumo histórico (feed de POS) para predecir demanda de F&B con >92% de precisión reportada, y disparar sugerencias de compra antes de quedarse corto — un par level *dinámico*, no un número fijo configurado a mano.

**Por qué es una evolución de Niveles de Par, no algo aparte:** InnNou no tiene integración con PMS ni POS todavía (no hay módulo de Consumo/`CONSUMPTION` en `InventoryMovementTypes` — confirmado como explícitamente fuera de alcance en `.claude/InventoryModule.md`, "no hay driver real todavía"). Pero una versión más simple y ya alcanzable con lo que existe: usar el historial de `OrderLine.Quantity` por artículo/warehouse a lo largo del tiempo como proxy de consumo, y sugerir (no fijar) un `ParLevels.MinimumQuantity` calculado en vez de puramente manual. Verdadera integración con PMS/POS es una fase muy posterior — anotarla aquí para no perder la idea, no para construirla pronto.

---

## 7. Dashboard de spend analytics — presupuesto vs. real por Family/Warehouse/Organización

Investigado 2026-07-28: los procurement dashboards 2026 (Spendflo, ProcureDesk, Suplari) convergen en un patrón: vista ejecutiva (spend total, variación de presupuesto), vista por categoría (spend por categoría/proveedor, dónde se sobrepasa el presupuesto), y drill-down hasta la transacción individual.

**Por qué es casi gratis con lo que ya existe:** `FamilyApprovalThreshold` ya modela un límite de gasto por `(Organization, Family, Level)`, y `OrderApprovalStep` ya congela `ActualFamilyAmount` en el momento de cada submit que cruzó un umbral — es decir, InnNou **ya tiene una serie histórica de gasto real vs. límite configurado**, solo que hoy vive dispersa en pasos de aprobación individuales, no agregada en ningún dashboard. Sumado a `PurchaseOrderLine`/`OrderLine` (montos reales por línea, con `Family`/`SubCategory` vía la clasificación de artículos), un dashboard de "gasto del mes por Family, comparado contra el umbral configurado" es una SP de agregación + una página, mismo patrón de bajo costo que el punto 1.

---

## 8. Escaneo de código de barras — recepción y conteo cíclico (complementa Goods Receipts y el punto 3)

Investigado 2026-07-28: confirmado como estándar en almacenes modernos — escanear en vez de buscar por nombre acelera tanto la recepción (Goods Receipts) como el conteo físico, actualiza cantidades en tiempo real al escanear, y funciona incluso desde un teléfono normal sin hardware dedicado para operaciones chicas/medianas.

**Por qué es barato y de alto impacto operativo:** `Article.Barcode` **ya existe como columna**, capturada desde el alta del artículo, pero hoy no la usa ningún flujo — ni Goods Receipts ni (cuando exista) el conteo cíclico del punto 3 la aprovechan para buscar el artículo más rápido que tipeando su nombre. No requiere una app nativa: un input que acepte el foco de un lector de código de barras USB/Bluetooth (que se comporta como teclado) alcanza para la mayoría de los casos de uso de almacén de un hotel — una app móvil dedicada sería una fase posterior, no un requisito para el primer valor.

---

## 9. Gestión de contratos con proveedor — vigencia, renovación, alertas

Investigado 2026-07-28: el software de contract management 2026 (Juro, Procurify, Graphite Connect) se centra en tres cosas: alertas antes de que un contrato/acuerdo de precio venza, visibilidad de los términos negociados para evitar "maverick spend" (comprar fuera de lo acordado), y comparación estructurada entre proveedores.

**Por qué esto cierra un gap real, no es un módulo nuevo desde cero:** `ArticlePrice` con `OrganizationId` seteado ya modela el "precio de contrato" (gana sobre el precio global en la misma fecha), pero es insert-only sin ningún concepto de vigencia — no hay `EffectiveUntil`/fecha de vencimiento, ni alerta de "este contrato vence en 30 días". Una extensión natural: agregar un campo opcional de vigencia a `ArticlePrice` (o una entidad `SupplierContract` más rica si se quiere guardar términos además del precio), y una vista/alerta de "contratos por vencer" — mismo principio de bajo esfuerzo que los puntos 1 y 7, apalancado sobre un modelo que ya existe.

---

## Contexto relevante ya existente (no reinventar)

- `Warehouse` ya modela las capabilities relevantes para todo lo de arriba (`IsInventoriable`, `CanAdjustInventory`, `CanTransferOut`, `CanReceiveTransfers`, `CanReceivePurchases`, `CanReceiveReturns`, etc.) — cualquier feature nueva debería apoyarse en estas, no crear una clasificación paralela.
- El patrón de shadow-user + impersonation ya está resuelto (`SupplierAccessModule.md`, `WarehousesModule.md`) — no hace falta un mecanismo nuevo si alguna de estas ideas necesita un actor nuevo (ej. un "responsable de compras" que revise el scorecard).
- `ArticlePackagingLevel` (N niveles ordenados, `IsDefinedUnit` marca el nivel final) es exactamente lo que el costeo de recetas necesitaría para bajar de `PurchaseUnitId` a la unidad de consumo real — ver `.claude/ArticlePackagingModule.md`.
- Los códigos de error e Id-backed lookups siguen la receta ya documentada en CLAUDE.md ("Status/type fields are Id-backed") — cualquier `Status` nuevo (RMA, sesión de conteo) debe seguirla desde el día uno, no empezar como `varchar` CHECK-constrained.
- `ParLevels`/`ParLevelOverrides` (ver `.claude/ParLevelsModule.md`) ya establecieron el patrón "resolución de prioridad en SQL, overlap-check en C#" para configuración con rangos de fechas — copiar esa división de responsabilidades si alguna idea futura (ej. RMA, conteo cíclico) necesita algo similar.
