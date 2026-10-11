"""
Module Knowledge Graph pour représenter les relations entre données de batteries
Utilise RDF et NetworkX pour créer des graphes de connaissances
"""

import logging
import json
from pathlib import Path
from typing import Dict, List, Optional, Set, Tuple
from dataclasses import asdict

import networkx as nx
from rdflib import Graph, Literal, RDF, RDFS, URIRef, Namespace
from rdflib.namespace import XSD


# Définir les namespaces RDF
BATTERY = Namespace("http://example.com/battery#")
SPEC = Namespace("http://example.com/specs#")


ONTOLOGY_PATH = Path(__file__).parent.parent / "ontology" / "battery_ontology.owl"

# Contraintes de validation tirées de l'ontologie
ONTOLOGY_CONSTRAINTS = {
    "voltage":             {"min": 0.5,   "max": 12.0},
    "capacity":            {"min": 1.0,   "max": 1_000_000.0},
    "internal_resistance": {"min": 0.1,   "max": 1000.0},
    "cycle_life":          {"min": 1,     "max": 10_000},
    "extractionConfidence":{"min": 0.0,   "max": 1.0},
    "minOperatingTemperature": {"min": -60.0},
    "maxOperatingTemperature": {"max": 100.0},
    # cohérence chimie → plage de voltage attendue
    "chemistry_voltage": {
        "lifepo4":  (3.0, 3.6),
        "li-ion":   (3.6, 4.2),
        "li-po":    (3.6, 4.2),
        "nimh":     (1.1, 1.3),
    }
}


class KnowledgeGraph:
    """
    Gestionnaire de Knowledge Graph pour les données de batteries.

    Fonctionnalités :
    1. Chargement de l'ontologie OWL formelle
    2. Construction de graphe RDF à partir des données extraites
    3. Requêtes SPARQL pour analyser les relations
    4. Export dans différents formats (Turtle, JSON-LD, XML)
    5. Visualisation avec NetworkX
    """

    def __init__(self, output_dir: str = "data/knowledge_graph"):
        """
        Initialiser le Knowledge Graph et charger l'ontologie OWL.

        Args:
            output_dir: Dossier de sortie pour les fichiers RDF
        """
        self.logger = logging.getLogger(__name__)
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(parents=True, exist_ok=True)

        # Graph RDF principal — commence par charger l'ontologie
        self.rdf_graph = Graph()
        self._load_ontology()

        # Graph NetworkX pour visualisation
        self.nx_graph = nx.DiGraph()

        # Bind des namespaces
        self.rdf_graph.bind("battery", BATTERY)
        self.rdf_graph.bind("spec", SPEC)

        # Statistiques
        self.stats = {
            'batteries_added': 0,
            'relations_created': 0,
            'properties_added': 0
        }

        self.logger.info("Knowledge Graph initialisé avec ontologie OWL")

    def _load_ontology(self):
        """Charger l'ontologie OWL dans le graphe RDF."""
        if ONTOLOGY_PATH.exists():
            try:
                self.rdf_graph.parse(str(ONTOLOGY_PATH), format="xml")
                self.logger.info(f"Ontologie chargée : {ONTOLOGY_PATH} ({len(self.rdf_graph)} triples)")
            except Exception as e:
                self.logger.warning(f"Impossible de charger l'ontologie : {e}")
        else:
            self.logger.warning(f"Fichier ontologie introuvable : {ONTOLOGY_PATH}")
    
    def add_battery(self, battery_data) -> URIRef:
        """
        Ajouter une batterie au graphe de connaissances
        
        Args:
            battery_data: Objet BatteryData ou dict
            
        Returns:
            URIRef: URI de la batterie dans le graphe
        """
        # Convertir en dict si nécessaire
        if hasattr(battery_data, 'to_dict'):
            data = battery_data.to_dict()
        else:
            data = dict(battery_data) if hasattr(battery_data, '__dict__') else battery_data
        
        # Créer URI unique pour la batterie
        battery_uri = BATTERY[f"battery_{data.get('model', 'unknown').replace(' ', '_')}"]
        
        # Ajouter type Battery
        self.rdf_graph.add((battery_uri, RDF.type, BATTERY.Battery))
        self.rdf_graph.add((battery_uri, RDFS.label, Literal(data.get('model', 'Unknown Battery'))))
        
        # Ajouter propriétés techniques
        self._add_technical_properties(battery_uri, data)
        
        # Ajouter au graph NetworkX
        self._add_to_networkx(battery_uri, data)
        
        # Créer relations avec autres batteries
        self._create_battery_relations(battery_uri, data)
        
        self.stats['batteries_added'] += 1
        self.logger.debug(f"Batterie ajoutée: {data.get('model')}")
        
        return battery_uri
    
    def _add_technical_properties(self, battery_uri: URIRef, data: Dict):
        """Ajouter les propriétés techniques d'une batterie"""
        
        property_mappings = {
            'manufacturer':           (SPEC.manufacturer,           XSD.string),
            'chemistry':              (SPEC.chemistry,              XSD.string),
            'voltage':                (SPEC.hasVoltage,             XSD.float),
            'voltage_unit':           (SPEC.voltageUnit,            XSD.string),
            'capacity':               (SPEC.hasCapacity,            XSD.float),
            'capacity_unit':          (SPEC.capacityUnit,           XSD.string),
            'charge_current':         (SPEC.chargeFlowRate,         XSD.float),
            'charge_current_unit':    (SPEC.chargeCurrentUnit,      XSD.string),
            'discharge_current':      (SPEC.dischargeCurrent,       XSD.float),
            'discharge_current_unit': (SPEC.dischargeCurrentUnit,   XSD.string),
            'internal_resistance':    (SPEC.internalResistance,     XSD.float),
            'internal_resistance_unit':(SPEC.internalResistanceUnit,XSD.string),
            'temperature_range':      (SPEC.temperatureRange,       XSD.string),
            'dimensions':             (SPEC.physicalDimensions,     XSD.string),
            'weight':                 (SPEC.weight,                 XSD.float),
            'weight_unit':            (SPEC.weightUnit,             XSD.string),
            'cycle_life':             (SPEC.cycleLife,              XSD.integer),
            'charge_cutoff_voltage':  (SPEC.chargeCutoffVoltage,    XSD.float),
            'discharge_cutoff_voltage':(SPEC.dischargeCutoffVoltage,XSD.float),
        }
        
        # Normaliser la capacité en mAh pour comparaisons cohérentes dans le graphe
        capacity_unit = str(data.get('capacity_unit', 'mAh')).strip().lower()

        for field, (predicate, datatype) in property_mappings.items():
            value = data.get(field)
            if value is not None and str(value).lower() not in ['unknown', 'none', '']:
                try:
                    if datatype in [XSD.float, XSD.integer]:
                        clean_value = str(value).replace('V', '').replace('A', '').replace('mAh', '').replace('Ah', '').strip()
                        typed_value = float(clean_value) if datatype == XSD.float else int(float(clean_value))
                        # Convertir Ah → mAh pour hasCapacity
                        if field == 'capacity' and capacity_unit == 'ah':
                            typed_value *= 1000
                        self.rdf_graph.add((battery_uri, predicate, Literal(typed_value, datatype=datatype)))
                    else:
                        self.rdf_graph.add((battery_uri, predicate, Literal(str(value), datatype=datatype)))
                    
                    self.stats['properties_added'] += 1
                    
                except (ValueError, TypeError) as e:
                    self.logger.warning(f"Erreur conversion {field}={value}: {e}")
    
    def _add_to_networkx(self, battery_uri: URIRef, data: Dict):
        """Ajouter la batterie au graphe NetworkX"""
        node_id = str(battery_uri).split('#')[-1]
        
        # Créer attributs pour le noeud
        node_attrs = {
            'type': 'Battery',
            'model': data.get('model', 'Unknown'),
            'voltage': data.get('voltage'),
            'capacity': data.get('capacity'),
            'chemistry': data.get('chemistry')
        }
        
        self.nx_graph.add_node(node_id, **node_attrs)
    
    def _create_battery_relations(self, battery_uri: URIRef, data: Dict):
        """Créer des relations entre batteries basées sur leurs propriétés."""
        node1 = str(battery_uri).split('#')[-1]

        # Relations par voltage similaire (±0.2V)
        voltage = data.get('voltage')
        if voltage:
            try:
                voltage_float = float(str(voltage).replace('V', ''))
                for similar_uri in self._find_similar_voltage_batteries(voltage_float, tolerance=0.2):
                    if similar_uri != battery_uri:
                        self.rdf_graph.add((battery_uri, BATTERY.similarVoltage, similar_uri))
                        self.stats['relations_created'] += 1
                        node2 = str(similar_uri).split('#')[-1]
                        self.nx_graph.add_edge(node1, node2, relation='similar_voltage', weight=0.8)
            except Exception:
                pass

        # Relations par même chimie
        chemistry = data.get('chemistry')
        if chemistry and str(chemistry).lower() not in ('unknown', 'none', ''):
            for chem_uri in self._find_same_chemistry_batteries(chemistry):
                if chem_uri != battery_uri:
                    self.rdf_graph.add((battery_uri, BATTERY.sameChemistry, chem_uri))
                    self.stats['relations_created'] += 1
                    node2 = str(chem_uri).split('#')[-1]
                    self.nx_graph.add_edge(node1, node2, relation='same_chemistry', weight=0.9)

        # Relations par capacité similaire — tout convertir en mAh avant de comparer
        capacity = data.get('capacity')
        capacity_unit = str(data.get('capacity_unit', 'mAh')).strip()
        if capacity:
            try:
                cap_float = float(str(capacity))
                if capacity_unit.lower() == 'ah':
                    cap_float *= 1000  # Ah → mAh
                for sim_uri in self._find_similar_capacity_batteries(cap_float, factor=3.0):
                    if sim_uri != battery_uri:
                        self.rdf_graph.add((battery_uri, BATTERY.similarCapacityRange, sim_uri))
                        self.stats['relations_created'] += 1
                        node2 = str(sim_uri).split('#')[-1]
                        self.nx_graph.add_edge(node1, node2, relation='similar_capacity', weight=0.6)
            except Exception:
                pass
    
    def _find_similar_voltage_batteries(self, voltage: float, tolerance: float = 0.2) -> List[URIRef]:
        """Trouver batteries avec voltage similaire"""
        similar_batteries = []
        
        # Requête SPARQL pour batteries avec voltage proche
        query = f"""
        SELECT ?battery ?voltage WHERE {{
            ?battery spec:hasVoltage ?voltage .
            FILTER (abs(?voltage - {voltage}) <= {tolerance})
        }}
        """
        
        try:
            results = self.rdf_graph.query(query)
            similar_batteries = [row.battery for row in results]
        except Exception as e:
            self.logger.warning(f"Erreur requête voltage similaire: {e}")
        
        return similar_batteries
    
    def _find_same_chemistry_batteries(self, chemistry: str) -> List[URIRef]:
        """Trouver batteries avec même chimie"""
        same_chem = []

        query = f"""
        SELECT ?battery WHERE {{
            ?battery spec:chemistry ?chem .
            FILTER (lcase(str(?chem)) = lcase("{chemistry}"))
        }}
        """

        try:
            results = self.rdf_graph.query(query)
            same_chem = [row.battery for row in results]
        except Exception as e:
            self.logger.warning(f"Erreur requête chimie: {e}")

        return same_chem

    def _find_similar_capacity_batteries(self, capacity: float, factor: float = 3.0) -> List[URIRef]:
        """Trouver batteries dont la capacité est dans un ratio ≤ factor."""
        similar = []
        if capacity <= 0:
            return similar

        low = capacity / factor
        high = capacity * factor

        query = f"""
        SELECT ?battery WHERE {{
            ?battery spec:hasCapacity ?cap .
            FILTER (?cap >= {low} && ?cap <= {high})
        }}
        """
        try:
            results = self.rdf_graph.query(query)
            similar = [row.battery for row in results]
        except Exception as e:
            self.logger.warning(f"Erreur requête capacité similaire: {e}")

        return similar

    def validate_with_ontology(self, battery_data) -> Dict:
        """
        Valider les données d'une batterie contre les contraintes de l'ontologie OWL.

        Vérifie les bornes min/max définies dans ONTOLOGY_CONSTRAINTS et la cohérence
        chimie-voltage issue des règles de l'ontologie.

        Returns:
            Dict avec 'valid' (bool), 'violations' (list) et 'warnings' (list)
        """
        if hasattr(battery_data, 'to_dict'):
            data = battery_data.to_dict()
        else:
            data = dict(battery_data)

        violations = []
        warnings = []

        numeric_checks = [
            ('voltage',             data.get('voltage')),
            ('capacity',            data.get('capacity')),
            ('internal_resistance', data.get('internal_resistance')),
            ('cycle_life',          data.get('cycle_life')),
        ]

        for field, value in numeric_checks:
            if value is None:
                continue
            try:
                v = float(value)
                rules = ONTOLOGY_CONSTRAINTS.get(field, {})
                if 'min' in rules and v < rules['min']:
                    violations.append(f"[OWL] {field}={v} < min={rules['min']}")
                if 'max' in rules and v > rules['max']:
                    violations.append(f"[OWL] {field}={v} > max={rules['max']}")
            except (ValueError, TypeError):
                warnings.append(f"[OWL] {field} non numérique : {value}")

        # Cohérence chimie ↔ voltage
        chemistry = str(data.get('chemistry', '')).lower()
        voltage = data.get('voltage')
        if chemistry and voltage:
            try:
                v = float(voltage)
                for chem_key, (vmin, vmax) in ONTOLOGY_CONSTRAINTS["chemistry_voltage"].items():
                    if chem_key in chemistry and not (vmin <= v <= vmax):
                        violations.append(
                            f"[OWL] Incohérence chimie/voltage : {chemistry} attend {vmin}-{vmax}V, trouvé {v}V"
                        )
            except (ValueError, TypeError):
                pass

        return {
            "valid": len(violations) == 0,
            "violations": violations,
            "warnings": warnings
        }

    def validate_with_graph(self, battery_data) -> Dict:
        """
        Validation basée sur le graphe peuplé.

        Compare la nouvelle batterie avec celles déjà dans le graphe :
        - Voltage cohérent avec les batteries de même chimie
        - Capacité dans l'ordre de grandeur observé
        - Cycle life dans la plage observée

        Returns:
            Dict avec 'violations' (list), 'warnings' (list), 'graph_stats' (dict)
        """
        if hasattr(battery_data, 'to_dict'):
            data = battery_data.to_dict()
        else:
            data = dict(battery_data)

        violations = []
        warnings   = []
        graph_stats = {}

        chemistry = str(data.get('chemistry', '')).lower()
        voltage   = data.get('voltage')
        capacity  = data.get('capacity')
        cycle_life = data.get('cycle_life')
        TOLERANCE = 0.30  # ±30% autour de la plage observée

        # ── 1. Cohérence voltage par chimie (depuis le graphe peuplé) ──
        if chemistry and voltage:
            query = f"""
            PREFIX spec: <http://example.com/specs#>
            SELECT ?v WHERE {{
                ?b spec:hasVoltage ?v .
                ?b spec:chemistry ?chem .
                FILTER (contains(lcase(str(?chem)), "{chemistry[:6]}"))
            }}
            """
            try:
                voltages = [float(row.v) for row in self.rdf_graph.query(query)]
                if voltages:
                    v_min = min(voltages) * (1 - TOLERANCE)
                    v_max = max(voltages) * (1 + TOLERANCE)
                    graph_stats['voltage_range_observed'] = (round(min(voltages),2), round(max(voltages),2))
                    graph_stats['voltage_range_accepted'] = (round(v_min,2), round(v_max,2))
                    try:
                        v = float(voltage)
                        if not (v_min <= v <= v_max):
                            violations.append(
                                f"[GRAPH] Voltage {v}V incohérent avec les {chemistry[:6]}... "
                                f"du graphe (plage observée : {min(voltages):.1f}–{max(voltages):.1f}V)"
                            )
                    except (ValueError, TypeError):
                        pass
            except Exception as e:
                self.logger.warning(f"Erreur requête graph voltage: {e}")

        # ── 2. Capacité dans l'ordre de grandeur du graphe ──
        if capacity:
            query = """
            PREFIX spec: <http://example.com/specs#>
            SELECT ?c WHERE { ?b spec:hasCapacity ?c . }
            """
            try:
                capacities = [float(row.c) for row in self.rdf_graph.query(query)]
                if capacities:
                    cap_min = min(capacities) / 10
                    cap_max = max(capacities) * 10
                    graph_stats['capacity_range_observed'] = (round(min(capacities),1), round(max(capacities),1))
                    try:
                        c = float(capacity)
                        if not (cap_min <= c <= cap_max):
                            violations.append(
                                f"[GRAPH] Capacité {c} hors de l'ordre de grandeur du graphe "
                                f"({min(capacities):.0f}–{max(capacities):.0f} mAh)"
                            )
                    except (ValueError, TypeError):
                        pass
            except Exception as e:
                self.logger.warning(f"Erreur requête graph capacité: {e}")

        # ── 3. Cycle life dans la plage observée ──
        if cycle_life:
            query = """
            PREFIX spec: <http://example.com/specs#>
            SELECT ?cl WHERE { ?b spec:cycleLife ?cl . }
            """
            try:
                cycles = [int(row.cl) for row in self.rdf_graph.query(query)]
                if cycles:
                    graph_stats['cycle_life_range_observed'] = (min(cycles), max(cycles))
                    try:
                        cl = int(cycle_life)
                        if cl > max(cycles) * 2:
                            warnings.append(
                                f"[GRAPH] Cycle life {cl} très supérieur au maximum observé ({max(cycles)})"
                            )
                    except (ValueError, TypeError):
                        pass
            except Exception as e:
                self.logger.warning(f"Erreur requête graph cycles: {e}")

        return {
            "valid":       len(violations) == 0,
            "violations":  violations,
            "warnings":    warnings,
            "graph_stats": graph_stats
        }

    def query_batteries_by_criteria(self, voltage_min: float = None, voltage_max: float = None,
                                  capacity_min: float = None, chemistry: str = None) -> List[Dict]:
        """
        Requêter les batteries selon des critères
        
        Args:
            voltage_min: Voltage minimum
            voltage_max: Voltage maximum  
            capacity_min: Capacité minimum
            chemistry: Type de chimie
            
        Returns:
            List[Dict]: Batteries correspondantes
        """
        # Construire requête SPARQL dynamique
        where_clauses = ["?battery rdf:type battery:Battery ."]
        
        if voltage_min is not None or voltage_max is not None:
            where_clauses.append("?battery spec:hasVoltage ?voltage .")
            if voltage_min is not None:
                where_clauses.append(f"FILTER (?voltage >= {voltage_min})")
            if voltage_max is not None:
                where_clauses.append(f"FILTER (?voltage <= {voltage_max})")
        
        if capacity_min is not None:
            where_clauses.append("?battery spec:hasCapacity ?capacity .")
            where_clauses.append(f"FILTER (?capacity >= {capacity_min})")
        
        if chemistry:
            where_clauses.append("?battery spec:chemistry ?chemistry .")
            where_clauses.append(f'FILTER (lcase(str(?chemistry)) = lcase("{chemistry}"))')
        
        # Ajouter sélection optionnelle
        select_vars = ["?battery", "?label"]
        optional_clauses = ["?battery rdfs:label ?label ."]
        
        if voltage_min is not None or voltage_max is not None:
            select_vars.append("?voltage")
        if capacity_min is not None:
            select_vars.append("?capacity")
        if chemistry:
            select_vars.append("?chemistry")
        
        query = f"""
        SELECT {' '.join(select_vars)} WHERE {{
            {' '.join(where_clauses)}
            {' '.join(optional_clauses)}
        }}
        """
        
        results = []
        try:
            query_results = self.rdf_graph.query(query)
            for row in query_results:
                result_dict = {'uri': str(row.battery), 'label': str(row.label)}
                if hasattr(row, 'voltage'):
                    result_dict['voltage'] = float(row.voltage)
                if hasattr(row, 'capacity'):
                    result_dict['capacity'] = float(row.capacity)
                if hasattr(row, 'chemistry'):
                    result_dict['chemistry'] = str(row.chemistry)
                results.append(result_dict)
                
        except Exception as e:
            self.logger.error(f"Erreur requête: {e}")
        
        return results
    
    def get_battery_relations(self, battery_model: str) -> Dict:
        """Obtenir toutes les relations d'une batterie"""
        battery_uri = BATTERY[f"battery_{battery_model.replace(' ', '_')}"]
        
        relations = {
            'similar_voltage': [],
            'same_chemistry': [],
            'all_properties': {}
        }
        
        # Relations voltage similaire
        similar_voltage_query = """
        SELECT ?related WHERE {
            <%s> battery:similarVoltage ?related .
        }
        """ % battery_uri
        
        try:
            results = self.rdf_graph.query(similar_voltage_query)
            relations['similar_voltage'] = [str(row.related) for row in results]
        except:
            pass
        
        # Relations même chimie
        same_chem_query = """
        SELECT ?related WHERE {
            <%s> battery:sameChemistry ?related .
        }
        """ % battery_uri
        
        try:
            results = self.rdf_graph.query(same_chem_query)
            relations['same_chemistry'] = [str(row.related) for row in results]
        except:
            pass
        
        # Toutes propriétés
        props_query = """
        SELECT ?property ?value WHERE {
            <%s> ?property ?value .
        }
        """ % battery_uri
        
        try:
            results = self.rdf_graph.query(props_query)
            for row in results:
                prop_name = str(row.property).split('#')[-1] if '#' in str(row.property) else str(row.property)
                relations['all_properties'][prop_name] = str(row.value)
        except:
            pass
        
        return relations
    
    def export_graph(self, format_type: str = "turtle", filename: str = None) -> str:
        """
        Exporter le graphe RDF
        
        Args:
            format_type: Format (turtle, xml, json-ld, nt)
            filename: Nom de fichier (optionnel)
            
        Returns:
            str: Chemin du fichier exporté
        """
        if filename is None:
            filename = f"battery_knowledge_graph.{format_type}"
        
        output_path = self.output_dir / filename
        
        try:
            self.rdf_graph.serialize(destination=str(output_path), format=format_type)
            self.logger.info(f"Graphe exporté: {output_path}")
            return str(output_path)
        except Exception as e:
            self.logger.error(f"Erreur export {format_type}: {e}")
            raise
    
    def export_networkx_graph(self, filename: str = "battery_network.json") -> str:
        """Exporter le graphe NetworkX en JSON"""
        output_path = self.output_dir / filename
        
        try:
            # Convertir en format sérialisable
            data = nx.node_link_data(self.nx_graph)
            
            with open(output_path, 'w', encoding='utf-8') as f:
                json.dump(data, f, indent=2, ensure_ascii=False)
            
            self.logger.info(f"Réseau NetworkX exporté: {output_path}")
            return str(output_path)
            
        except Exception as e:
            self.logger.error(f"Erreur export NetworkX: {e}")
            raise
    
    def generate_statistics(self) -> Dict:
        """Générer des statistiques sur le graphe"""
        stats = {
            'rdf_graph': {
                'total_triples': len(self.rdf_graph),
                'batteries_count': len(list(self.rdf_graph.subjects(RDF.type, BATTERY.Battery))),
            },
            'networkx_graph': {
                'nodes_count': self.nx_graph.number_of_nodes(),
                'edges_count': self.nx_graph.number_of_edges(),
                'density': nx.density(self.nx_graph) if self.nx_graph.number_of_nodes() > 0 else 0
            },
            'processing_stats': self.stats
        }
        
        # Analyse des propriétés les plus communes
        property_counts = {}
        for s, p, o in self.rdf_graph:
            prop_name = str(p).split('#')[-1] if '#' in str(p) else str(p)
            property_counts[prop_name] = property_counts.get(prop_name, 0) + 1
        
        stats['common_properties'] = dict(sorted(property_counts.items(), key=lambda x: x[1], reverse=True)[:10])
        
        return stats
    
    def visualize_summary(self) -> str:
        """Générer un résumé textuel du graphe"""
        stats = self.generate_statistics()
        
        summary = f"""
=== KNOWLEDGE GRAPH SUMMARY ===

RDF Graph:
- Total triples: {stats['rdf_graph']['total_triples']}
- Batteries: {stats['rdf_graph']['batteries_count']}

Network Graph:
- Nodes: {stats['networkx_graph']['nodes_count']}
- Edges: {stats['networkx_graph']['edges_count']}
- Density: {stats['networkx_graph']['density']:.3f}

Processing:
- Batteries added: {stats['processing_stats']['batteries_added']}
- Relations created: {stats['processing_stats']['relations_created']}
- Properties added: {stats['processing_stats']['properties_added']}

Top Properties:
"""
        for prop, count in list(stats['common_properties'].items())[:5]:
            summary += f"- {prop}: {count}\n"
        
        return summary


if __name__ == "__main__":
    # Test du Knowledge Graph
    from battery_extractor import BatteryData
    
    # Créer graphe de test
    kg = KnowledgeGraph("test_output")
    
    # Ajouter quelques batteries de test
    test_batteries = [
        BatteryData(
            model="Battery_3.7V_2500mAh",
            voltage=3.7,
            capacity=2500,
            capacity_unit="mAh",
            chemistry="Li-ion"
        ),
        BatteryData(
            model="Battery_3.2V_5000mAh", 
            voltage=3.2,
            capacity=5000,
            capacity_unit="mAh",
            chemistry="LiFePO4"
        ),
        BatteryData(
            model="Battery_3.7V_1800mAh",
            voltage=3.7,
            capacity=1800,
            capacity_unit="mAh",
            chemistry="Li-ion"
        )
    ]
    
    # Ajouter au graphe
    for battery in test_batteries:
        kg.add_battery(battery)
    
    # Test requête
    li_ion_batteries = kg.query_batteries_by_criteria(chemistry="Li-ion")
    print(f"Batteries Li-ion trouvées: {len(li_ion_batteries)}")
    
    # Export
    kg.export_graph("turtle", "test_graph.ttl")
    kg.export_networkx_graph("test_network.json")
    
    # Statistiques
    print(kg.visualize_summary())