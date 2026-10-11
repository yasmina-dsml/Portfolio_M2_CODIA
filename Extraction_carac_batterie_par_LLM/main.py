"""
Point d'entrée principal du pipeline d'extraction de datasheets de batteries.

Usage :
    python main.py --pdf <fichier.pdf>                  # Extraire un seul PDF
    python main.py --dir <dossier/>                     # Traiter tous les PDFs d'un dossier
    python main.py --eval --pdf <fichier.pdf>           # Évaluer zero-shot vs few-shot
    python main.py --export-graph                       # Exporter le knowledge graph (Turtle)
"""

import argparse
import json
import sys
from pathlib import Path

# Ajouter le dossier racine au path pour les imports src.*
sys.path.insert(0, str(Path(__file__).parent))

from src.battery_extractor import BatteryExtractor
from src.knowledge_graph import KnowledgeGraph


CONFIG_PATH = "config/config.yml"


def cmd_extract(args):
    """Extraire un ou plusieurs PDFs et sauvegarder les résultats en JSON."""
    method = args.method or "zero_shot"
    extractor = BatteryExtractor(CONFIG_PATH)

    if args.pdf:
        if method == "compare":
            _cmd_compare(extractor, args.pdf)
        else:
            result = extractor.extract_from_pdf(args.pdf, method=method)
            print(json.dumps(result.to_dict(), indent=2, ensure_ascii=False))

    elif args.dir:
        results = extractor.extract_batch(args.dir, method=method)
        report = extractor.generate_report(results)
        print(f"\n=== Rapport ({method}) ===")
        print(f"Fichiers traités  : {report['total_files']}")
        print(f"Extractions OK    : {report['successful_extractions']}")
        print(f"Taux de succès    : {report['success_rate']*100:.1f}%")
        print(f"Confiance moyenne : {report['average_confidence']:.2f}")
        if report['common_errors']:
            print(f"Erreurs fréquentes :")
            for err, count in report['common_errors'].items():
                print(f"  [{count}x] {err}")

    else:
        print("Spécifiez --pdf <fichier> ou --dir <dossier>", file=sys.stderr)
        sys.exit(1)


def _cmd_compare(extractor, pdf_path: str):
    """Extraire avec zero-shot et few-shot et afficher la comparaison côte à côte."""
    text = extractor.pdf_processor.extract_text(pdf_path)

    print("Extraction en cours (zero-shot)...")
    zs_raw = extractor.llm.extract_battery_info(text, method="zero_shot")
    print("Extraction en cours (few-shot)...")
    fs_raw = extractor.llm.extract_battery_info(text, method="few_shot")

    fields = sorted(set(zs_raw.keys()) | set(fs_raw.keys()))

    print(f"\n{'Champ':<25} {'Zero-shot':>20} {'Few-shot':>20}")
    print("-" * 67)
    for field in fields:
        zs_val = str(zs_raw.get(field, "—"))
        fs_val = str(fs_raw.get(field, "—"))
        match = "=" if zs_val == fs_val else "≠"
        print(f"{field:<25} {zs_val:>20} {fs_val:>20}  {match}")

    zs_known = sum(1 for v in zs_raw.values() if v not in (None, "unknown"))
    fs_known = sum(1 for v in fs_raw.values() if v not in (None, "unknown"))
    print(f"\nChamps extraits   : zero-shot={zs_known}/{len(fields)}   few-shot={fs_known}/{len(fields)}")


def cmd_evaluate(args):
    """Comparer zero-shot vs few-shot sur un ou plusieurs PDFs avec vérité terrain."""
    extractor = BatteryExtractor(CONFIG_PATH)

    if args.dir:
        _cmd_evaluate_batch(extractor, args)
        return

    if not args.pdf:
        print("--eval requiert --pdf <fichier> ou --dir <dossier>", file=sys.stderr)
        sys.exit(1)

    text = extractor.pdf_processor.extract_text(args.pdf)

    ground_truth = {}
    if args.ground_truth:
        with open(args.ground_truth, encoding='utf-8') as f:
            ground_truth = json.load(f)

    if ground_truth:
        eval_result = extractor.llm.evaluate_extraction(text, ground_truth)
        zs = eval_result["zero_shot"]["metrics"]
        fs = eval_result["few_shot"]["metrics"]

        print("\n=== Comparaison Zero-shot vs Few-shot ===")
        print(f"{'Métrique':<20} {'Zero-shot':>10} {'Few-shot':>10}")
        print("-" * 42)
        for metric in ("precision", "recall", "f1", "coverage"):
            print(f"{metric:<20} {zs[metric]:>10.4f} {fs[metric]:>10.4f}")
        print(f"\nMeilleure méthode : {eval_result['best_method']}")
        print(f"Gain F1           : {eval_result['comparison']['improvement']:+.4f}")

        print("\n--- Détail par champ (few-shot) ---")
        for field, info in fs["per_field"].items():
            if info.get("match") is None:
                continue
            status = "OK" if info["match"] else "ERR"
            pred = info.get("predicted", "—")
            exp  = info.get("expected", "—")
            print(f"  {status} {field:<25} prédit={pred}  attendu={exp}")
    else:
        # Sans vérité terrain : juste afficher les deux extractions
        zs_result = extractor.llm.extract_battery_info(text, "zero_shot")
        fs_result = extractor.llm.extract_battery_info(text, "few_shot")
        print("\n=== Zero-shot ===")
        print(json.dumps(zs_result, indent=2, ensure_ascii=False))
        print("\n=== Few-shot ===")
        print(json.dumps(fs_result, indent=2, ensure_ascii=False))


def _cmd_evaluate_batch(extractor, args):
    """Évaluer tous les PDFs d'un dossier avec les JSON ground truth associés."""
    pdf_dir = Path(args.dir)
    gt_dir = Path(args.ground_truth or "data/ground_truth")

    if not pdf_dir.is_dir():
        print(f"Dossier PDF introuvable: {pdf_dir}", file=sys.stderr)
        sys.exit(1)
    if not gt_dir.is_dir():
        print(f"Dossier ground truth introuvable: {gt_dir}", file=sys.stderr)
        sys.exit(1)

    rows = []
    for pdf_path in sorted(pdf_dir.glob("*.pdf")):
        gt_path = _find_ground_truth(pdf_path, gt_dir)
        if not gt_path:
            print(f"Ignoré {pdf_path.name}: aucun ground truth trouvé", file=sys.stderr)
            continue

        print(f"\nÉvaluation: {pdf_path.name}")
        text = extractor.pdf_processor.extract_text(str(pdf_path))
        with open(gt_path, encoding="utf-8") as f:
            ground_truth = json.load(f)

        eval_result = extractor.llm.evaluate_extraction(text, ground_truth)
        zs = eval_result["zero_shot"]["metrics"]
        fs = eval_result["few_shot"]["metrics"]
        rows.append({
            "pdf": pdf_path.name,
            "ground_truth": gt_path.name,
            "zero_precision": zs["precision"],
            "few_precision": fs["precision"],
            "zero_recall": zs["recall"],
            "few_recall": fs["recall"],
            "zero_f1": zs["f1"],
            "few_f1": fs["f1"],
            "zero_coverage": zs["coverage"],
            "few_coverage": fs["coverage"],
        })

        print(f"  Ground truth : {gt_path.name}")
        print(f"  Zero-shot    : precision={zs['precision']:.4f}  recall={zs['recall']:.4f}  f1={zs['f1']:.4f}  coverage={zs['coverage']:.4f}")
        print(f"  Few-shot     : precision={fs['precision']:.4f}  recall={fs['recall']:.4f}  f1={fs['f1']:.4f}  coverage={fs['coverage']:.4f}")
        print(f"  Gain F1      : {eval_result['comparison']['improvement']:+.4f}")

    if not rows:
        print("Aucune paire PDF/ground truth évaluée.", file=sys.stderr)
        sys.exit(1)

    def avg(metric: str) -> float:
        return sum(row[metric] for row in rows) / len(rows)

    print("\n=== Résumé batch Zero-shot vs Few-shot ===")
    print(
        f"{'PDF':<72} "
        f"{'Z-Prec':>7} {'F-Prec':>7} "
        f"{'Z-Rec':>7} {'F-Rec':>7} "
        f"{'Z-F1':>7} {'F-F1':>7} {'ΔF1':>7} "
        f"{'Z-Cov':>7} {'F-Cov':>7}"
    )
    print("-" * 145)
    for row in rows:
        print(
            f"{row['pdf']:<72} "
            f"{row['zero_precision']:>7.4f} {row['few_precision']:>7.4f} "
            f"{row['zero_recall']:>7.4f} {row['few_recall']:>7.4f} "
            f"{row['zero_f1']:>7.4f} {row['few_f1']:>7.4f} {row['few_f1'] - row['zero_f1']:>+7.4f} "
            f"{row['zero_coverage']:>7.4f} {row['few_coverage']:>7.4f}"
        )
    print("-" * 145)
    print(
        f"{'Moyenne':<72} "
        f"{avg('zero_precision'):>7.4f} {avg('few_precision'):>7.4f} "
        f"{avg('zero_recall'):>7.4f} {avg('few_recall'):>7.4f} "
        f"{avg('zero_f1'):>7.4f} {avg('few_f1'):>7.4f} {avg('few_f1') - avg('zero_f1'):>+7.4f} "
        f"{avg('zero_coverage'):>7.4f} {avg('few_coverage'):>7.4f}"
    )

    print("\n=== Gains moyens Few-shot - Zero-shot ===")
    print(f"Precision : {avg('few_precision') - avg('zero_precision'):+.4f}")
    print(f"Recall    : {avg('few_recall') - avg('zero_recall'):+.4f}")
    print(f"F1        : {avg('few_f1') - avg('zero_f1'):+.4f}")
    print(f"Coverage  : {avg('few_coverage') - avg('zero_coverage'):+.4f}")


def _find_ground_truth(pdf_path: Path, gt_dir: Path) -> Path | None:
    """Trouver le JSON de vérité terrain correspondant au PDF."""
    exact = gt_dir / f"{pdf_path.stem}.json"
    if exact.exists():
        return exact

    known_matches = {
        "CATL-86Ah-3.2V-LiFePO4-Prismatic-Battery-Cell-SpecificationDatasheet": "CATL-86Ah.json",
    }
    mapped = known_matches.get(pdf_path.stem)
    if mapped and (gt_dir / mapped).exists():
        return gt_dir / mapped

    for gt_path in gt_dir.glob("*.json"):
        if gt_path.stem in pdf_path.stem or pdf_path.stem in gt_path.stem:
            return gt_path
    return None


def cmd_export_graph(args):
    """Exporter le knowledge graph en Turtle (et optionnellement JSON-LD)."""
    kg = KnowledgeGraph("data/knowledge_graph")

    # Charger les JSONs extraits pour repeupler le graphe
    extracted_dir = Path("data/extracted")
    loaded = 0
    for json_file in extracted_dir.glob("*.json"):
        try:
            with open(json_file, encoding='utf-8') as f:
                data = json.load(f)
            if isinstance(data, list):
                for item in data:
                    kg.add_battery(item)
                    loaded += 1
            elif isinstance(data, dict) and 'model' in data:
                kg.add_battery(data)
                loaded += 1
        except Exception as e:
            print(f"Ignoré {json_file.name}: {e}", file=sys.stderr)

    if loaded == 0:
        print("Aucune donnée trouvée dans data/extracted/. Lancez d'abord une extraction.")
        sys.exit(1)

    ttl_path = kg.export_graph("turtle", "batteries.ttl")
    nx_path  = kg.export_networkx_graph("batteries_network.json")

    print(f"Batteries chargées : {loaded}")
    print(f"Graphe RDF exporté : {ttl_path}")
    print(f"Réseau NetworkX    : {nx_path}")
    print(kg.visualize_summary())


def main():
    parser = argparse.ArgumentParser(
        description="Pipeline d'extraction de datasheets de batteries par LLM"
    )
    parser.add_argument("--pdf",          help="Chemin vers un fichier PDF")
    parser.add_argument("--dir",          help="Dossier contenant des PDFs à traiter en lot")
    parser.add_argument("--method",       choices=["zero_shot", "few_shot", "compare"],
                        default="zero_shot",
                        help="Stratégie de prompting (défaut: zero_shot)")
    parser.add_argument("--eval",         action="store_true",
                        help="Comparer zero-shot vs few-shot avec métriques (requiert --pdf ou --dir)")
    parser.add_argument("--ground-truth",
                        help="Fichier JSON pour --pdf, ou dossier de JSON pour --dir")
    parser.add_argument("--export-graph", action="store_true",
                        help="Exporter le knowledge graph depuis data/extracted/")
    parser.add_argument("--config",       default=CONFIG_PATH,
                        help=f"Chemin vers la configuration YAML (défaut: {CONFIG_PATH})")

    args = parser.parse_args()

    if args.export_graph:
        cmd_export_graph(args)
    elif args.eval:
        cmd_evaluate(args)
    elif args.pdf or args.dir:
        cmd_extract(args)
    else:
        parser.print_help()


if __name__ == "__main__":
    main()