"""
Deux visualisations du Knowledge Graph :
  1. Ontologie OWL  — classes, sous-classes, unités
  2. Graphe peuplé  — batteries extraites des PDFs + relations sémantiques
"""

import json
import networkx as nx
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
from rdflib import Graph, RDF, RDFS, OWL, Namespace

OWL_FILE        = "ontology/battery_ontology.owl"
NETWORK_FILE    = "data/knowledge_graph/batteries_network.json"
OUT_ONTOLOGY    = "data/knowledge_graph/ontology_graph.png"
OUT_POPULATED   = "data/knowledge_graph/batteries_graph.png"

BATTERY = Namespace("http://example.com/battery#")
SPEC    = Namespace("http://example.com/specs#")

# ─────────────────────────────────────────────────────────────
# PARTIE 1 — Ontologie OWL
# ─────────────────────────────────────────────────────────────

COLOR_MAIN     = "#4A90D9"
COLOR_SUBCLASS = "#85C1E9"
COLOR_SPEC     = "#27AE60"
COLOR_UNIT     = "#F39C12"


def short_name(uri):
    return uri.split("#")[-1] if "#" in uri else uri.split("/")[-1]


def fr_label(g, uri):
    for lbl in g.objects(uri, RDFS.label):
        if getattr(lbl, "language", None) == "fr":
            return str(lbl)
    return short_name(str(uri))


def draw_ontology():
    g = Graph()
    g.parse(OWL_FILE, format="xml")
    print(f"Ontologie chargée : {len(g)} triples")

    G = nx.DiGraph()
    colors, labels = {}, {}

    owl_classes      = set(g.subjects(RDF.type, OWL.Class))
    unit_individuals = set(g.subjects(RDF.type, SPEC.Unit))

    for cls in owl_classes:
        n = str(cls)
        if "battery#" not in n and "specs#" not in n:
            continue
        G.add_node(n)
        labels[n] = fr_label(g, cls)
        if n == str(BATTERY.Battery):
            colors[n] = COLOR_MAIN
        elif "battery#" in n:
            colors[n] = COLOR_SUBCLASS
        else:
            colors[n] = COLOR_SPEC

    for unit in unit_individuals:
        n = str(unit)
        G.add_node(n)
        labels[n] = fr_label(g, unit)
        colors[n] = COLOR_UNIT

    for child, _, parent in g.triples((None, RDFS.subClassOf, None)):
        c, p = str(child), str(parent)
        if c in G and p in G:
            G.add_edge(p, c, rel="subClassOf")

    unit_class = str(SPEC.Unit)
    if unit_class in G:
        for unit in unit_individuals:
            u = str(unit)
            if u in G:
                G.add_edge(unit_class, u, rel="instance")

    fig, ax = plt.subplots(figsize=(14, 9))
    pos = nx.spring_layout(G, seed=7, k=2.8)

    node_colors = [colors.get(n, "#95A5A6") for n in G.nodes]
    nx.draw_networkx_nodes(G, pos, node_color=node_colors, node_size=1800, alpha=0.92, ax=ax)
    nx.draw_networkx_labels(G, pos,
                            labels={n: labels.get(n, short_name(n)) for n in G.nodes},
                            font_size=7, font_weight="bold", ax=ax)

    sub_edges = [(u, v) for u, v, d in G.edges(data=True) if d.get("rel") == "subClassOf"]
    ins_edges = [(u, v) for u, v, d in G.edges(data=True) if d.get("rel") == "instance"]
    nx.draw_networkx_edges(G, pos, edgelist=sub_edges, edge_color="#2C3E50",
                           arrows=True, arrowsize=18, width=1.8,
                           connectionstyle="arc3,rad=0.05", ax=ax)
    nx.draw_networkx_edges(G, pos, edgelist=ins_edges, edge_color="#E67E22",
                           arrows=True, arrowsize=14, width=1.2, style="dashed",
                           connectionstyle="arc3,rad=0.05", ax=ax)

    ax.legend(handles=[
        mpatches.Patch(color=COLOR_MAIN,     label="Classe principale (Battery)"),
        mpatches.Patch(color=COLOR_SUBCLASS, label="Sous-classe (Li-ion, LiFePO4…)"),
        mpatches.Patch(color=COLOR_SPEC,     label="Spécification / Unité de mesure"),
        mpatches.Patch(color=COLOR_UNIT,     label="Instance unité (V, mAh, A…)"),
        mpatches.Patch(color="#2C3E50",      label="subClassOf"),
        mpatches.Patch(color="#E67E22",      label="instance de"),
    ], loc="upper left", fontsize=8)
    ax.set_title("Ontologie des Batteries (OWL) — Schéma", fontsize=14, fontweight="bold")
    ax.axis("off")
    plt.tight_layout()
    plt.savefig(OUT_ONTOLOGY, dpi=150, bbox_inches="tight")
    print(f"Sauvegardé : {OUT_ONTOLOGY}")
    plt.show()


# ─────────────────────────────────────────────────────────────
# PARTIE 2 — Graphe peuplé (batteries extraites)
# ─────────────────────────────────────────────────────────────

CHEMISTRY_COLORS = {
    "li-ion":              "#4A90D9",
    "polymer lithium-ion": "#7B68EE",
    "lifepo4":             "#27AE60",
    "lithium iron phosphate": "#27AE60",
    "lithium":             "#52BE80",
    "nimh":                "#E67E22",
    "unknown":             "#95A5A6",
}

EDGE_COLORS = {
    "similar_voltage":  "#E74C3C",
    "same_chemistry":   "#3498DB",
    "similar_capacity": "#F39C12",
}


def node_color(chemistry):
    chem = str(chemistry).lower()
    # Correspondance partielle pour les variantes de Li-ion
    if any(k in chem for k in ["li-ion", "lithium-ion", "lithium ion"]):
        return CHEMISTRY_COLORS["li-ion"]
    if "lifepo4" in chem or "lithium iron" in chem or "iron phosphate" in chem:
        return CHEMISTRY_COLORS["lifepo4"]
    if "polymer" in chem:
        return CHEMISTRY_COLORS["polymer lithium-ion"]
    if "nimh" in chem or "nickel" in chem:
        return CHEMISTRY_COLORS["nimh"]
    return CHEMISTRY_COLORS.get(chem, "#95A5A6")


def draw_populated():
    with open(NETWORK_FILE, encoding="utf-8") as f:
        data = json.load(f)

    G = nx.node_link_graph(data)

    fig, ax = plt.subplots(figsize=(13, 8))
    pos = nx.spring_layout(G, seed=42, k=2.5)

    colors = [node_color(G.nodes[n].get("chemistry", "unknown")) for n in G.nodes]
    labels = {n: (
        f"{G.nodes[n].get('model', n.replace('battery_',''))}\n"
        f"{G.nodes[n].get('voltage', '?')}V\n"
        f"{G.nodes[n].get('chemistry', '?')}"
    ) for n in G.nodes}

    nx.draw_networkx_nodes(G, pos, node_color=colors, node_size=2200, alpha=0.9, ax=ax)
    nx.draw_networkx_labels(G, pos, labels=labels, font_size=7, font_weight="bold", ax=ax)

    for relation, color in EDGE_COLORS.items():
        edges = [(u, v) for u, v, d in G.edges(data=True) if d.get("relation") == relation]
        if edges:
            nx.draw_networkx_edges(G, pos, edgelist=edges, edge_color=color,
                                   arrows=True, arrowsize=20, width=2,
                                   connectionstyle="arc3,rad=0.1", ax=ax)

    edge_labels = {(u, v): d.get("relation", "").replace("_", " ")
                   for u, v, d in G.edges(data=True)}
    nx.draw_networkx_edge_labels(G, pos, edge_labels=edge_labels, font_size=7, ax=ax)

    legend_handles = [
        mpatches.Patch(color=c, label=chem.title())
        for chem, c in CHEMISTRY_COLORS.items() if chem != "unknown"
    ] + [
        mpatches.Patch(color=c, label=rel.replace("_", " ").title())
        for rel, c in EDGE_COLORS.items()
    ]
    ax.legend(handles=legend_handles, loc="upper left", fontsize=8)
    ax.set_title("Knowledge Graph — Batteries extraites des PDFs", fontsize=14, fontweight="bold")
    ax.axis("off")
    plt.tight_layout()
    plt.savefig(OUT_POPULATED, dpi=150, bbox_inches="tight")
    print(f"Sauvegardé : {OUT_POPULATED}")
    plt.show()


# ─────────────────────────────────────────────────────────────
# MAIN
# ─────────────────────────────────────────────────────────────

if __name__ == "__main__":
    print("=== Graphe 1 : Ontologie OWL ===")
    draw_ontology()

    print("\n=== Graphe 2 : Batteries peuplées ===")
    draw_populated()
