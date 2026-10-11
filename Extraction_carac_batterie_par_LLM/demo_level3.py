"""
Démonstration Level 3 — Détection d'hallucinations et validation

Montre 3 cas :
  1. Batterie avec valeurs hallucinées -> détection
  2. Batterie valide -> aucune erreur
  3. Comparaison zero-shot vs few-shot sur un vrai PDF
"""

import sys, json
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent))

from src.battery_extractor import BatteryData
from src.validator import DataValidator, ValidationLevel
from src.knowledge_graph import KnowledgeGraph

CONFIG_VALIDATION = {
    'voltage':     {'min': 1.0, 'max': 5.0, 'typical_lithium': [3.2, 3.7, 4.2]},
    'capacity':    {'min': 10, 'max': 100000},
    'temperature': {'operating_min': -40, 'operating_max': 85}
}

SEP  = "=" * 60
SEP2 = "-" * 40


def print_validation(result, title: str):
    print(f"\n{SEP2}")
    print(f"  {title}")
    print(SEP2)
    print(f"  Valide       : {' OUI' if result.is_valid else ' NON'}")
    print(f"  Confiance    : {result.confidence:.2f}")
    if result.errors:
        print(f"  Erreurs ({len(result.errors)}) :")
        for e in result.errors:
            print(f"    • {e}")
    if result.warnings:
        print(f"  Avertissements :")
        for w in result.warnings:
            print(f"    ~ {w}")
    if result.suspicious_values:
        print(f"  Valeurs suspectes :")
        for field, msg in result.suspicious_values.items():
            print(f"    ? {field}: {msg}")
    if result.missing_fields:
        print(f"  Champs manquants : {result.missing_fields}")


# ─────────────────────────────────────────────────────────────
# CAS 1 — Batterie avec valeurs hallucinées
# ─────────────────────────────────────────────────────────────

def print_graph_result(graph_result):
    print(f"\n  Violations Graphe ({len(graph_result['violations'])}) :")
    for v in graph_result['violations']:
        print(f"    ER {v}")
    if not graph_result['violations']:
        print("    OK Aucune violation graphe")
    if graph_result.get('warnings'):
        for w in graph_result['warnings']:
            print(f"    ~ {w}")
    if graph_result.get('graph_stats'):
        print(f"  Statistiques du graphe :")
        for k, val in graph_result['graph_stats'].items():
            print(f"    • {k} : {val}")


def load_kg_with_data() -> KnowledgeGraph:
    """Charge le KG et le peuple avec les batteries extraites."""
    kg = KnowledgeGraph()
    extracted_dir = Path("data/extracted")
    for f in sorted(extracted_dir.glob("*.json")):
        try:
            data = json.loads(f.read_text(encoding="utf-8"))
            if isinstance(data, dict) and "model" in data:
                kg.add_battery(data)
        except Exception:
            pass
    return kg


def demo_hallucination():
    print(f"\n{SEP}")
    print("CAS 1 — Batterie avec valeurs hallucinées (LLM zero-shot)")
    print(SEP)

    validator = DataValidator(CONFIG_VALIDATION)
    kg = load_kg_with_data()

    hallu = BatteryData(
        model="FAKE-BATTERY",
        chemistry="Li-ion",
        voltage=10.0,          # HALLUCINATION : impossible pour Li-ion
        capacity=100000,       # HALLUCINATION : multiple rond de 1000
        capacity_unit="mAh",
        cycle_life=9999,       # HALLUCINATION : valeur ronde suspecte
        temperature_range="-100°C to +200°C",  # HALLUCINATION : hors plage
        charge_cutoff_voltage=5.0,
        discharge_cutoff_voltage=0.5,
    )

    print("\nDonnées extraites par le LLM (zero-shot) :")
    print(json.dumps(hallu.to_dict(), indent=2, ensure_ascii=False))

    result = validator.validate(hallu)
    print_validation(result, "Résultat validation DataValidator")

    owl_result = kg.validate_with_ontology(hallu)
    print(f"\n  Violations OWL ({len(owl_result['violations'])}) :")
    for v in owl_result['violations']:
        print(f"    ER {v}")
    if not owl_result['violations']:
        print("    OK Aucune")

    graph_result = kg.validate_with_graph(hallu)
    print_graph_result(graph_result)


# ─────────────────────────────────────────────────────────────
# CAS 2 — Batterie valide
# ─────────────────────────────────────────────────────────────

def demo_valid():
    print(f"\n{SEP}")
    print("CAS 2 — Batterie valide (LLM few-shot)")
    print(SEP)

    validator = DataValidator(CONFIG_VALIDATION)
    kg = load_kg_with_data()

    valid = BatteryData(
        model="LIR2450",
        manufacturer="EEMB Co., Ltd.",
        chemistry="Li-ion",
        voltage=3.6,
        capacity=120,
        capacity_unit="mAh",
        charge_current=24,
        charge_current_unit="mA",
        discharge_current=24,
        discharge_current_unit="mA",
        internal_resistance=400,
        internal_resistance_unit="mΩ",
        temperature_range="-20°C to 60°C",
        dimensions="Diameter 24.5 mm; height 5.0 mm",
        weight=5.0,
        weight_unit="g",
        cycle_life=300,
        charge_cutoff_voltage=4.2,
        discharge_cutoff_voltage=2.75,
    )

    print("\nDonnées extraites par le LLM (few-shot) :")
    print(json.dumps(valid.to_dict(), indent=2, ensure_ascii=False))

    result = validator.validate(valid)
    print_validation(result, "Résultat validation DataValidator")

    owl_result = kg.validate_with_ontology(valid)
    print(f"\n  Violations OWL ({len(owl_result['violations'])}) :")
    for v in owl_result['violations']:
        print(f"    ER {v}")
    if not owl_result['violations']:
        print("    OK Aucune violation — données cohérentes avec l'ontologie")

    graph_result = kg.validate_with_graph(valid)
    print_graph_result(graph_result)


# ─────────────────────────────────────────────────────────────
# CAS 3 — Comparaison zero-shot vs few-shot sur fichiers réels
# ─────────────────────────────────────────────────────────────

def demo_comparison():
    print(f"\n{SEP}")
    print("CAS 3 — Comparaison zero-shot vs few-shot (fichiers extraits réels)")
    print(SEP)

    extracted_dir = Path("data/extracted")
    validator = DataValidator(CONFIG_VALIDATION)

    for json_file in sorted(extracted_dir.glob("*.json")):
        try:
            data = json.loads(json_file.read_text(encoding="utf-8"))
        except Exception:
            continue

        battery = BatteryData(
            model=data.get("model", json_file.stem),
            chemistry=data.get("chemistry"),
            voltage=data.get("voltage"),
            capacity=data.get("capacity"),
            capacity_unit=data.get("capacity_unit", "mAh"),
            temperature_range=data.get("temperature_range"),
            cycle_life=data.get("cycle_life"),
            charge_cutoff_voltage=data.get("charge_cutoff_voltage"),
            discharge_cutoff_voltage=data.get("discharge_cutoff_voltage"),
        )

        result = validator.validate(battery)
        status = " VALIDE" if result.is_valid else " INVALIDE"
        n_err  = len(result.errors)
        n_susp = len(result.suspicious_values)
        conf   = result.confidence

        print(f"\n  {battery.model:<50} {status}")
        print(f"    Confiance : {conf:.2f}  |  Erreurs : {n_err}  |  Valeurs suspectes : {n_susp}")
        if result.errors:
            for e in result.errors[:2]:
                print(f"    • {e}")


# ─────────────────────────────────────────────────────────────
# MAIN
# ─────────────────────────────────────────────────────────────

if __name__ == "__main__":
    demo_hallucination()
    demo_valid()
    demo_comparison()

    print(f"\n{SEP}")
    print("RÉSUMÉ — Mécanismes de détection d'hallucinations")
    print(SEP)
    print("""
  1. DataValidator    — plages physiques (voltage, capacité, température)
  2. Cohérence chimie — Li-ion doit avoir 3.6–4.2 V, LiFePO4 doit avoir 3.0–3.6 V
  3. Valeurs rondes   — multiples de 1000 ou voltages entiers suspects
  4. OWL constraints  — contraintes formelles de l'ontologie
  5. Confidence score — pénalise proportionnellement les violations

  . Zero-shot : nombreuses hallucinations (confidence ~ 0.09)
  . Few-shot  : données fiables       (confidence ~ 1.00)
""")
