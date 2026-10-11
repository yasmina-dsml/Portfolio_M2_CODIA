"""
Démonstration Level 2 — Knowledge Graph Construction
  1. JSON → Triples RDF  (exemple concret)
  2. Requêtes SPARQL sur le graphe peuplé
"""

import sys, json
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent))

from src.knowledge_graph import KnowledgeGraph

EXTRACTED_DIR = Path("data/extracted")
KG_DIR        = "data/knowledge_graph"


def load_kg() -> KnowledgeGraph:
    kg = KnowledgeGraph(KG_DIR)
    loaded = 0
    for f in EXTRACTED_DIR.glob("*.json"):
        try:
            data = json.loads(f.read_text(encoding="utf-8"))
            if isinstance(data, dict) and "model" in data:
                kg.add_battery(data)
                loaded += 1
        except Exception:
            pass
    print(f"Batteries chargées : {loaded}\n")
    return kg


# ─────────────────────────────────────────────────────────────
# 1. JSON → Triples
# ─────────────────────────────────────────────────────────────

def demo_json_to_triples(kg: KnowledgeGraph):
    print("=" * 60)
    print("PARTIE 1 — Conversion JSON → Triples RDF")
    print("=" * 60)

    example = {
        "model": "LIR2450",
        "manufacturer": "EEMB Co., Ltd.",
        "chemistry": "Li-ion rechargeable battery",
        "voltage": 3.6,
        "voltage_unit": "V",
        "capacity": 120,
        "capacity_unit": "mAh",
        "charge_current": 24,
        "charge_current_unit": "mA",
        "internal_resistance": 400,
        "internal_resistance_unit": "mΩ",
        "cycle_life": 300,
        "charge_cutoff_voltage": 4.2,
        "discharge_cutoff_voltage": 2.75
    }

    print("\nJSON extrait du PDF :")
    print(json.dumps(example, indent=2, ensure_ascii=False))

    print("\nTriples RDF générés (format Turtle) :")
    print("-" * 40)
    uri = f"battery:battery_{example['model']}"
    print(f"{uri}  rdf:type          battery:Battery .")
    print(f"{uri}  rdfs:label        \"{example['model']}\" .")
    print(f"{uri}  spec:manufacturer \"{example['manufacturer']}\" .")
    print(f"{uri}  spec:chemistry    \"{example['chemistry']}\" .")
    print(f"{uri}  spec:hasVoltage   {example['voltage']} .")
    print(f"{uri}  spec:hasCapacity  {example['capacity']} .")
    print(f"{uri}  spec:chargeFlowRate {example['charge_current']} .")
    print(f"{uri}  spec:internalResistance {example['internal_resistance']} .")
    print(f"{uri}  spec:cycleLife    {example['cycle_life']} .")
    print(f"{uri}  spec:chargeCutoffVoltage  {example['charge_cutoff_voltage']} .")
    print(f"{uri}  spec:dischargeCutoffVoltage {example['discharge_cutoff_voltage']} .")


# ─────────────────────────────────────────────────────────────
# 2. Requêtes SPARQL
# ─────────────────────────────────────────────────────────────

def demo_sparql(kg: KnowledgeGraph):
    print("\n" + "=" * 60)
    print("PARTIE 2 — Requêtes SPARQL sur le graphe peuplé")
    print("=" * 60)

    # Requête 1 — Toutes les batteries avec leur voltage
    print("\n--- Requête 1 : Toutes les batteries et leur tension ---")
    q1 = """
    PREFIX spec: <http://example.com/specs#>
    PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
    SELECT ?label ?voltage WHERE {
        ?battery spec:hasVoltage ?voltage .
        ?battery rdfs:label ?label .
    }
    ORDER BY ?voltage
    """
    for row in kg.rdf_graph.query(q1):
        print(f"  {str(row.label):<35} {float(row.voltage):.1f} V")

    # Requête 2 — Batteries Li-ion
    print("\n--- Requête 2 : Batteries par chimie ---")
    q2 = """
    PREFIX spec: <http://example.com/specs#>
    PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
    SELECT ?label ?chemistry WHERE {
        ?battery spec:chemistry ?chemistry .
        ?battery rdfs:label ?label .
    }
    ORDER BY ?chemistry
    """
    for row in kg.rdf_graph.query(q2):
        print(f"  {str(row.label):<35} {str(row.chemistry)}")

    # Requête 3 — Batteries avec voltage > 3.5V
    print("\n--- Requête 3 : Batteries avec tension > 3.5V ---")
    q3 = """
    PREFIX spec: <http://example.com/specs#>
    PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
    SELECT ?label ?voltage WHERE {
        ?battery spec:hasVoltage ?voltage .
        ?battery rdfs:label ?label .
        FILTER (?voltage > 3.5)
    }
    """
    for row in kg.rdf_graph.query(q3):
        print(f"  {str(row.label):<35} {float(row.voltage):.1f} V")

    # Requête 4 — Batteries avec cycle_life > 500
    print("\n--- Requête 4 : Batteries avec durée de vie > 500 cycles ---")
    q4 = """
    PREFIX spec: <http://example.com/specs#>
    PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
    SELECT ?label ?cycles WHERE {
        ?battery spec:cycleLife ?cycles .
        ?battery rdfs:label ?label .
        FILTER (?cycles > 500)
    }
    """
    results = list(kg.rdf_graph.query(q4))
    if results:
        for row in results:
            print(f"  {str(row.label):<35} {int(row.cycles)} cycles")
    else:
        print("  (aucune batterie > 500 cycles dans le graphe actuel)")


# ─────────────────────────────────────────────────────────────
# MAIN
# ─────────────────────────────────────────────────────────────

if __name__ == "__main__":
    kg = load_kg()
    demo_json_to_triples(kg)
    demo_sparql(kg)
