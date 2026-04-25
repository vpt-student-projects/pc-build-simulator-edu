#!/usr/bin/env python3
"""
Экспорт каталога комплектующих из PostgreSQL в JSON для Unity (StreamingAssets/pc_catalog.json).

Зависимости: pip install psycopg2-binary

Пример:
  set PGPASSWORD=...
  python tools/export_pc_catalog.py --dsn "host=localhost dbname=pc_builder user=builder" -o StreamingAssets/pc_catalog.json
"""

import argparse
import json
import sys

try:
    import psycopg2
    import psycopg2.extras
except ImportError:
    print("Install: pip install psycopg2-binary", file=sys.stderr)
    sys.exit(1)


def fetch_all(conn):
    cur = conn.cursor(cursor_factory=psycopg2.extras.RealDictCursor)
    cur.execute(
        """
        SELECT c.id, cc.code AS category_code, c.name, c.vendor, c.model, c.description,
               c.price::float AS price, c.power_watts, c.model_tier, c.icon_path
        FROM components c
        JOIN component_categories cc ON cc.id = c.category_id
        WHERE c.is_active IS DISTINCT FROM FALSE
        ORDER BY c.id
        """
    )
    rows = list(cur.fetchall())
    cpus = {r["component_id"]: r for r in _fetch_specs(cur, "cpu_specs")}
    mbs = {r["component_id"]: r for r in _fetch_specs(cur, "motherboard_specs")}
    rams = {r["component_id"]: r for r in _fetch_specs(cur, "ram_specs")}
    gpus = {r["component_id"]: r for r in _fetch_specs(cur, "gpu_specs")}
    psus = {r["component_id"]: r for r in _fetch_specs(cur, "psu_specs")}
    stor = {r["component_id"]: r for r in _fetch_specs(cur, "storage_specs")}
    cool = {r["component_id"]: r for r in _fetch_specs(cur, "cooler_specs")}
    cur.execute("SELECT cooler_id, socket FROM cooler_socket_support")
    cooler_socks = {}
    for r in cur.fetchall():
        cooler_socks.setdefault(r["cooler_id"], []).append(r["socket"])
    cur.close()

    out = []
    for r in rows:
        cid = r["id"]
        code = r["category_code"]
        item = {
            "databaseId": cid,
            "categoryCode": code,
            "name": r["name"],
            "vendor": r["vendor"],
            "model": r["model"],
            "description": r["description"] or "",
            "price": float(r["price"] or 0),
            "powerWatts": int(r["power_watts"] or 0),
            "modelTier": int(r["model_tier"] or 1),
            "iconPath": r["icon_path"] or "",
            "socket": "",
            "ramType": "",
            "ramSlots": 0,
            "maxRamGb": 0,
            "requiredPsuW": 0,
            "psuWattage": 0,
            "gpuTdpW": 0,
            "storageType": "",
            "capacityGb": 0,
            "coolerSocketsCsv": "",
        }
        if code == "CPU" and cid in cpus:
            s = cpus[cid]
            item["socket"] = s["socket"] or ""
            item["ramType"] = s["ram_type"] or ""
        elif code == "MOTHERBOARD" and cid in mbs:
            s = mbs[cid]
            item["socket"] = s["socket"] or ""
            item["ramType"] = s["ram_type"] or ""
            item["ramSlots"] = int(s["ram_slots"] or 0)
            item["maxRamGb"] = int(s["max_ram_gb"] or 0)
        elif code == "RAM" and cid in rams:
            s = rams[cid]
            item["ramType"] = s["ram_type"] or ""
        elif code == "GPU" and cid in gpus:
            s = gpus[cid]
            item["requiredPsuW"] = int(s["required_psu_w"] or 0)
            item["gpuTdpW"] = int(s["tdp_w"] or 0)
        elif code == "PSU" and cid in psus:
            s = psus[cid]
            item["psuWattage"] = int(s["wattage"] or 0)
        elif code == "STORAGE" and cid in stor:
            s = stor[cid]
            item["storageType"] = s["storage_type"] or ""
            item["capacityGb"] = int(s["capacity_gb"] or 0)
        elif code == "CPU_COOLER" and cid in cool:
            socks = cooler_socks.get(cid, [])
            item["coolerSocketsCsv"] = ",".join(socks)
        out.append(item)
    return out


def _fetch_specs(cur, table):
    cur.execute(f"SELECT * FROM {table}")
    return cur.fetchall()


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--dsn", required=True, help='PostgreSQL connection string, e.g. "host=127.0.0.1 dbname=pc_builder user=builder"')
    p.add_argument("-o", "--output", default="StreamingAssets/pc_catalog.json")
    args = p.parse_args()
    conn = psycopg2.connect(args.dsn)
    try:
        data = fetch_all(conn)
    finally:
        conn.close()
    payload = {"components": data}
    with open(args.output, "w", encoding="utf-8") as f:
        json.dump(payload, f, ensure_ascii=False, indent=2)
    print(f"Wrote {len(data)} components to {args.output}")


if __name__ == "__main__":
    main()
