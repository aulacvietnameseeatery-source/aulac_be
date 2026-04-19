#!/usr/bin/env python3
"""
Generate a Conceptual ERD (Level 1) for Âu Lạc Restaurant in draw.io XML.

Matches the visual style from the reference image:
- shape=table entities with 3-column rows (type | name | PK/FK)
- Crow's-foot (ER) cardinality on edges
- Only core business entities (no junction/infrastructure tables)
- Simplified field sets per entity
"""

import os
import html

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
OUTPUT_PATH = os.path.join(SCRIPT_DIR, "conceptual-erd.drawio.xml")

# Layout constants
TABLE_WIDTH = 200
COL_WIDTHS = (48, 112, 40)
ROW_HEIGHT = 26
HEADER_HEIGHT = 25
X_GAP = 70
Y_GAP = 60

# ─────────────────────────────────────────────────────────
# ENTITY DEFINITIONS — matching the reference image exactly
# ─────────────────────────────────────────────────────────

ENTITIES = {
    # ── Row 0: top-left cluster ──
    "supplier": {
        "name": "Supplier",
        "fields": [
            ("string", "supplierId", "PK"),
            ("string", "supplierName", ""),
            ("string", "phone", ""),
            ("string", "email", ""),
        ],
    },
    "ingredient_cat": {
        "name": "Ingredient Category",
        "fields": [
            ("string", "categoryId", "PK"),
            ("string", "categoryName", ""),
        ],
    },
    "dish_category": {
        "name": "Dish Category",
        "fields": [
            ("string", "categoryId", "PK"),
            ("string", "categoryName", ""),
        ],
    },
    "customer": {
        "name": "Customer",
        "fields": [
            ("string", "customerId", "PK"),
            ("string", "fullName", ""),
            ("string", "phone", ""),
            ("string", "email", ""),
            ("boolean", "isMember", ""),
            ("int", "loyaltyPoints", ""),
            ("datetime", "createdAt", ""),
        ],
    },
    "reservation": {
        "name": "Reservation",
        "fields": [
            ("string", "reservationId", "PK"),
            ("datetime", "reservationTime", ""),
            ("int", "partySize", ""),
            ("string", "status", ""),
            ("string", "source", ""),
            ("string", "note", ""),
        ],
    },
    "role": {
        "name": "Role",
        "fields": [
            ("string", "roleId", "PK"),
            ("string", "roleName", ""),
        ],
    },
    # ── Row 1: middle cluster ──
    "inventory_item": {
        "name": "Inventory Item",
        "fields": [
            ("string", "ingredientId", "PK"),
            ("string", "ingredientName", ""),
            ("string", "unit", ""),
            ("string", "supplierId", ""),
            ("string", "inStock", ""),
            ("string", "status", ""),
        ],
    },
    "dish": {
        "name": "Dish",
        "fields": [
            ("string", "dishId", "PK"),
            ("string", "dishName", ""),
            ("decimal", "price", ""),
            ("string", "status", ""),
        ],
    },
    "order": {
        "name": "Order",
        "fields": [
            ("string", "orderId", "PK"),
            ("datetime", "orderTime", ""),
            ("string", "status", ""),
            ("string", "source", ""),
            ("decimal", "totalAmount", ""),
            ("string", "note", ""),
            ("string", "table_id", ""),
        ],
    },
    "restaurant_table": {
        "name": "Restaurant Table",
        "fields": [
            ("string", "tableId", "PK"),
            ("string", "tableCode", ""),
            ("int", "capacity", ""),
            ("string", "tableType", ""),
            ("string", "tableStatus", ""),
        ],
    },
    "staff": {
        "name": "Staff Account",
        "fields": [
            ("string", "staffId", "PK"),
            ("string", "fullName", ""),
            ("string", "phone", ""),
            ("string", "email", ""),
            ("string", "username", ""),
            ("string", "status", ""),
            ("datetime", "lastLoginAt", ""),
        ],
    },
    "permission": {
        "name": "Permission",
        "fields": [
            ("string", "permissionId", "PK"),
            ("string", "screenName", ""),
            ("string", "actionCode", ""),
        ],
    },
    # ── Row 2: bottom cluster ──
    "inv_transaction": {
        "name": "Inventory Transaction",
        "fields": [
            ("string", "transactionId", "PK"),
            ("string", "createdBy", "FK"),
            ("string", "direction", ""),
            ("string", "note", ""),
            ("string", "createdAt", ""),
            ("string", "reason", ""),
        ],
    },
    "promotion": {
        "name": "Promotion",
        "fields": [
            ("string", "promotionId", "PK"),
            ("string", "promoCode", ""),
            ("string", "promoName", ""),
            ("string", "type", ""),
            ("datetime", "startTime", ""),
            ("datetime", "endTime", ""),
        ],
    },
    "media": {
        "name": "Media Assets",
        "fields": [
            ("string", "media_id", "PK"),
            ("string", "url", ""),
            ("string", "type", ""),
            ("string", "metadata", ""),
        ],
    },
    "payment": {
        "name": "Payment",
        "fields": [
            ("string", "paymentId", "PK"),
            ("decimal", "amount", ""),
            ("string", "method", ""),
            ("datetime", "paidAt", ""),
        ],
    },
    "service_error": {
        "name": "Service Error",
        "fields": [
            ("string", "errorId", "PK"),
            ("string", "message", ""),
            ("string", "severity", ""),
            ("datetime", "createdAt", ""),
            ("datetime", "resolvedAt", ""),
        ],
    },
    "service_error_cat": {
        "name": "Service Error Category",
        "fields": [
            ("string", "categoryId", "PK"),
            ("string", "categoryName", ""),
        ],
    },
    # ── Row 3: Shift & System ──
    "shift_template": {
        "name": "Shift Template",
        "fields": [
            ("long", "shiftTemplateId", "PK"),
            ("string", "templateName", ""),
            ("time", "defaultStartTime", ""),
            ("time", "defaultEndTime", ""),
            ("string", "description", ""),
            ("boolean", "isActive", ""),
        ],
    },
    "shift_assignment": {
        "name": "Shift Assignment",
        "fields": [
            ("long", "shiftAssignmentId", "PK"),
            ("long", "shiftTemplateId", "FK"),
            ("long", "staffId", "FK"),
            ("date", "workDate", ""),
            ("datetime", "plannedStartAt", ""),
            ("datetime", "plannedEndAt", ""),
            ("string", "status", ""),
        ],
    },
    "attendance": {
        "name": "Attendance Record",
        "fields": [
            ("long", "attendanceId", "PK"),
            ("long", "shiftAssignmentId", "FK"),
            ("string", "status", ""),
            ("datetime", "checkInAt", ""),
            ("datetime", "checkOutAt", ""),
            ("int", "workedMinutes", ""),
        ],
    },
    "dish_tag": {
        "name": "Dish Tag",
        "fields": [
            ("long", "dishTagId", "PK"),
            ("long", "dishId", "FK"),
            ("uint", "tagId", "FK"),
        ],
    },
    "sys_setting": {
        "name": "System Setting",
        "fields": [
            ("uint", "settingId", "PK"),
            ("string", "settingKey", ""),
            ("string", "settingName", ""),
            ("string", "valueType", ""),
            ("string", "valueString", ""),
            ("string", "description", ""),
        ],
    },
}

# ─────────────────────────────────────────────────────────
# LAYOUT — arranged to mirror the reference image layout
# Grid rows with manual placement for readability
# ─────────────────────────────────────────────────────────

LAYOUT_ROWS = [
    # Row 0 — top
    ["supplier", "ingredient_cat", "dish_category", "customer", "reservation", "role"],
    # Row 1 — middle
    ["inventory_item", "dish", "order", "restaurant_table", "staff", "permission"],
    # Row 2 — bottom-mid
    ["inv_transaction", "promotion", "media", "payment", "service_error", "service_error_cat"],
    # Row 3 — bottom (shift, tags, system)
    ["shift_template", "shift_assignment", "attendance", "dish_tag", "sys_setting"],
]

# ─────────────────────────────────────────────────────────
# RELATIONSHIPS — core business relationships from the image
# (source, target, label, start_arrow, start_fill, end_arrow, end_fill)
# ─────────────────────────────────────────────────────────

RELATIONSHIPS = [
    # Customer relationships
    ("customer",        "reservation",      "makes",        "ERmandOne", 1, "ERzeroToMany", 1),
    ("customer",        "order",            "places",       "ERmandOne", 1, "ERzeroToMany", 1),

    # Reservation ↔ Table
    ("reservation",     "restaurant_table", "reserves",     "ERzeroToMany", 1, "ERzeroToMany", 1),

    # Order relationships
    ("order",           "dish",             "contains",     "ERzeroToMany", 1, "ERzeroToMany", 1),
    ("restaurant_table","order",            "served_at",    "ERmandOne", 1, "ERzeroToMany", 1),
    ("order",           "payment",          "paid_by",      "ERmandOne", 1, "ERzeroToMany", 1),
    ("promotion",       "order",            "applies",      "ERzeroToMany", 0, "ERzeroToMany", 1),
    ("order",           "payment",          "may_have",     "ERmandOne", 1, "ERzeroToMany", 1),

    # Dish categorization
    ("dish_category",   "dish",             "classifies",   "ERmandOne", 1, "ERoneToMany", 1),
    ("ingredient_cat",  "inventory_item",   "classifies",   "ERmandOne", 1, "ERoneToMany", 1),

    # Supplier ↔ Inventory
    ("supplier",        "inventory_item",   "fulfills",     "ERmandOne", 1, "ERzeroToMany", 1),
    ("inventory_item",  "inv_transaction",  "has",          "ERmandOne", 1, "ERzeroToMany", 1),

    # Staff / Auth
    ("role",            "staff",            "assigns",      "ERmandOne", 1, "ERoneToMany", 1),
    ("role",            "permission",       "grants",       "ERzeroToMany", 1, "ERzeroToMany", 1),
    ("staff",           "inv_transaction",  "Created by",   "ERzeroToOne", 0, "ERzeroToMany", 1),
    ("staff",           "order",            "handles",      "ERzeroToOne", 0, "ERzeroToMany", 1),

    # Service Errors
    ("staff",           "service_error",    "causes",       "ERzeroToOne", 0, "ERzeroToMany", 1),
    ("restaurant_table","service_error",    "handles",      "ERmandOne", 1, "ERzeroToMany", 1),
    ("service_error",   "staff",            "resolves",     "ERzeroToMany", 1, "ERzeroToOne", 0),
    ("service_error_cat","service_error",   "categorizes",  "ERmandOne", 1, "ERzeroToMany", 1),

    # Media
    ("media",           "dish",             "has",          "ERzeroToMany", 0, "ERzeroToMany", 1),
    ("media",           "order",            "has",          "ERzeroToMany", 0, "ERzeroToMany", 1),

    # Shift & Attendance
    ("shift_template",  "shift_assignment", "schedules",    "ERmandOne", 1, "ERzeroToMany", 1),
    ("staff",           "shift_assignment", "assigned to",  "ERmandOne", 1, "ERzeroToMany", 1),
    ("shift_assignment","attendance",        "records",      "ERmandOne", 1, "ERzeroToOne", 0),

    # Dish Tags
    ("dish",            "dish_tag",         "tagged",       "ERmandOne", 1, "ERzeroToMany", 1),
]


def esc(text):
    return html.escape(str(text), quote=True)


def entity_height(ent):
    return HEADER_HEIGHT + len(ent["fields"]) * ROW_HEIGHT


def compute_positions():
    positions = {}
    current_y = 40

    for row_aliases in LAYOUT_ROWS:
        existing = [a for a in row_aliases if a in ENTITIES]
        if not existing:
            continue
        max_h = max(entity_height(ENTITIES[a]) for a in existing)
        for i, alias in enumerate(existing):
            x = 40 + i * (TABLE_WIDTH + X_GAP)
            positions[alias] = (x, current_y)
        current_y += max_h + Y_GAP

    return positions


def generate_xml():
    positions = compute_positions()
    L = []
    w = L.append

    max_x = max(p[0] for p in positions.values()) + TABLE_WIDTH + 200
    max_y = max(p[1] + entity_height(ENTITIES[a]) for a, p in positions.items()) + 200

    w('<?xml version="1.0" encoding="UTF-8"?>')
    w(f'<mxGraphModel dx="1422" dy="762" grid="1" gridSize="10" guides="1" tooltips="1" connect="1" arrows="1" fold="1" page="1" pageScale="1" pageWidth="{max_x}" pageHeight="{max_y}" math="0" shadow="0">')
    w('  <root>')
    w('    <mxCell id="0" />')
    w('    <mxCell id="1" parent="0" />')

    # ── ENTITY TABLES ──
    for alias, ent in ENTITIES.items():
        if alias not in positions:
            continue
        x, y = positions[alias]
        h = entity_height(ent)
        eid = f"entity_{alias}"

        w(f'    <mxCell id="{eid}" value="{esc(ent["name"])}" '
          f'style="shape=table;startSize={HEADER_HEIGHT};container=1;collapsible=0;'
          f'childLayout=tableLayout;fixedRows=1;rowLines=1;fontStyle=1;align=center;resizeLast=1;" '
          f'vertex="1" parent="1">')
        w(f'      <mxGeometry x="{x}" y="{y}" width="{TABLE_WIDTH}" height="{h}" as="geometry" />')
        w(f'    </mxCell>')

        for fi, (ftype, fname, fkey) in enumerate(ent["fields"]):
            row_y = HEADER_HEIGHT + fi * ROW_HEIGHT
            rid = f"row_{alias}_{fi}"

            w(f'    <mxCell id="{rid}" value="" '
              f'style="shape=tableRow;horizontal=0;startSize=0;swimlaneHead=0;swimlaneBody=0;'
              f'fillColor=none;collapsible=0;dropTarget=0;points=[[0,0.5],[1,0.5]];'
              f'portConstraint=eastwest;top=0;left=0;right=0;bottom=0;" '
              f'vertex="1" parent="{eid}">')
            w(f'      <mxGeometry y="{row_y}" width="{TABLE_WIDTH}" height="{ROW_HEIGHT}" as="geometry" />')
            w(f'    </mxCell>')

            # Col 1 — type
            c1id = f"c_{alias}_{fi}_t"
            w(f'    <mxCell id="{c1id}" value="{esc(ftype)}" '
              f'style="shape=partialRectangle;connectable=0;fillColor=none;'
              f'top=0;left=0;bottom=0;right=0;align=left;spacingLeft=2;overflow=hidden;fontSize=11;" '
              f'vertex="1" parent="{rid}">')
            w(f'      <mxGeometry width="{COL_WIDTHS[0]}" height="{ROW_HEIGHT}" as="geometry">')
            w(f'        <mxRectangle width="{COL_WIDTHS[0]}" height="{ROW_HEIGHT}" as="alternateBounds" />')
            w(f'      </mxGeometry>')
            w(f'    </mxCell>')

            # Col 2 — name
            c2id = f"c_{alias}_{fi}_n"
            w(f'    <mxCell id="{c2id}" value="{esc(fname)}" '
              f'style="shape=partialRectangle;connectable=0;fillColor=none;'
              f'top=0;left=0;bottom=0;right=0;align=left;spacingLeft=2;overflow=hidden;fontSize=11;" '
              f'vertex="1" parent="{rid}">')
            w(f'      <mxGeometry x="{COL_WIDTHS[0]}" width="{COL_WIDTHS[1]}" height="{ROW_HEIGHT}" as="geometry">')
            w(f'        <mxRectangle width="{COL_WIDTHS[1]}" height="{ROW_HEIGHT}" as="alternateBounds" />')
            w(f'      </mxGeometry>')
            w(f'    </mxCell>')

            # Col 3 — key
            c3id = f"c_{alias}_{fi}_k"
            c3x = COL_WIDTHS[0] + COL_WIDTHS[1]
            w(f'    <mxCell id="{c3id}" value="{esc(fkey)}" '
              f'style="shape=partialRectangle;connectable=0;fillColor=none;'
              f'top=0;left=0;bottom=0;right=0;align=left;spacingLeft=2;overflow=hidden;fontSize=11;" '
              f'vertex="1" parent="{rid}">')
            w(f'      <mxGeometry x="{c3x}" width="{COL_WIDTHS[2]}" height="{ROW_HEIGHT}" as="geometry">')
            w(f'        <mxRectangle width="{COL_WIDTHS[2]}" height="{ROW_HEIGHT}" as="alternateBounds" />')
            w(f'      </mxGeometry>')
            w(f'    </mxCell>')

    # ── EDGES ──
    for ri, (src, tgt, label, sa, sf, ea, ef) in enumerate(RELATIONSHIPS):
        if src not in ENTITIES or tgt not in ENTITIES:
            continue
        eid = f"edge_{ri}"
        style = (f"edgeStyle=orthogonalEdgeStyle;rounded=0;"
                 f"startArrow={sa};startSize=10;startFill={sf};"
                 f"endArrow={ea};endSize=10;endFill={ef};")

        w(f'    <mxCell id="{eid}" value="{esc(label)}" '
          f'style="{style}" '
          f'edge="1" parent="1" source="entity_{src}" target="entity_{tgt}">')
        w(f'      <mxGeometry relative="1" as="geometry" />')
        w(f'    </mxCell>')

    w('  </root>')
    w('</mxGraphModel>')
    return '\n'.join(L)


def main():
    xml = generate_xml()
    with open(OUTPUT_PATH, 'w', encoding='utf-8') as f:
        f.write(xml)
    print(f"Wrote: {OUTPUT_PATH} ({os.path.getsize(OUTPUT_PATH):,} bytes)")

    # Quick counts
    ent_count = len(ENTITIES)
    rel_count = len(RELATIONSHIPS)
    print(f"Entities: {ent_count}, Relationships: {rel_count}")

    # Validate
    import xml.etree.ElementTree as ET
    try:
        tree = ET.parse(OUTPUT_PATH)
        cells = tree.getroot().findall('.//')
        tables = [c for c in cells if 'shape=table;' in (c.get('style') or '')]
        edges = [c for c in cells if c.get('edge') == '1']
        print(f"XML validation: PASS ({len(tables)} tables, {len(edges)} edges)")
    except ET.ParseError as e:
        print(f"XML validation: FAIL - {e}")

    # List entities
    for alias, ent in ENTITIES.items():
        print(f"  {ent['name']} ({len(ent['fields'])} fields)")


if __name__ == '__main__':
    main()
