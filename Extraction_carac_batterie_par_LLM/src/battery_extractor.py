"""
Module principal pour l'extraction d'informations de datasheets de batteries
Utilise des LLM pour extraire et structurer les données techniques
"""

import os
import json
import yaml
import logging
from dotenv import load_dotenv
load_dotenv()
from pathlib import Path
from typing import Dict, List, Optional, Union
from dataclasses import dataclass

from src.pdf_processor import PDFProcessor
from src.llm_interface import LLMInterface
from src.validator import DataValidator
from src.knowledge_graph import KnowledgeGraph


@dataclass
class BatteryData:
    """Structure pour stocker les données d'une batterie"""
    model: str
    manufacturer: Optional[str] = None
    chemistry: Optional[str] = None
    voltage: Optional[float] = None
    voltage_unit: str = "V"
    capacity: Optional[float] = None
    capacity_unit: str = "mAh"
    charge_current: Optional[float] = None
    charge_current_unit: str = "A"
    discharge_current: Optional[float] = None
    discharge_current_unit: str = "A"
    internal_resistance: Optional[float] = None
    internal_resistance_unit: str = "mΩ"
    temperature_range: Optional[str] = None
    dimensions: Optional[str] = None
    weight: Optional[float] = None
    weight_unit: str = "g"
    cycle_life: Optional[int] = None
    charge_cutoff_voltage: Optional[float] = None
    discharge_cutoff_voltage: Optional[float] = None
    confidence_score: Optional[float] = None
    errors: List[str] = None
    ontology_violations: List[str] = None

    def __post_init__(self):
        if self.errors is None:
            self.errors = []
        if self.ontology_violations is None:
            self.ontology_violations = []

    def to_dict(self) -> Dict:
        """Convertir en dictionnaire pour sérialisation JSON"""
        return {k: v for k, v in self.__dict__.items()
                if v is not None and v != [] and v != {}}


class BatteryExtractor:
    """
    Extracteur principal pour les datasheets de batteries
    
    Pipeline:
    1. Extraction PDF → Texte
    2. LLM → Extraction structurée  
    3. Validation → Vérification cohérence
    4. Stockage → JSON + Knowledge Graph
    """
    
    def __init__(self, config_path: str = "config/config.yml"):
        """Initialiser l'extracteur avec la configuration"""
        self.config = self._load_config(config_path)
        # Créer les dossiers en premier (logs/ doit exister avant le FileHandler)
        self._setup_directories()
        self._setup_logging()

        # Initialiser les composants
        self.pdf_processor = PDFProcessor()
        self.llm = LLMInterface(self.config['llm'])
        self.validator = DataValidator(self.config['validation'])
        
        # Knowledge Graph optionnel
        if self.config['knowledge_graph']['enable']:
            self.kg = KnowledgeGraph()
        else:
            self.kg = None
        
        self.logger.info("BatteryExtractor initialisé avec succès")
    
    def _load_config(self, config_path: str) -> Dict:
        """Charger la configuration YAML avec substitution des variables d'environnement"""
        import os
        import re
        try:
            with open(config_path, 'r', encoding='utf-8') as f:
                content = f.read()
            # Remplacer ${VAR_NAME} par la valeur de la variable d'environnement
            content = re.sub(
                r'\$\{([^}]+)\}',
                lambda m: os.environ.get(m.group(1), ''),
                content
            )
            return yaml.safe_load(content)
        except FileNotFoundError:
            raise FileNotFoundError(f"Fichier de configuration non trouvé: {config_path}")
    
    def _setup_logging(self):
        """Configurer le logging"""
        log_level = getattr(logging, self.config['logging']['level'])
        logging.basicConfig(
            level=log_level,
            format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
            handlers=[
                logging.FileHandler(self.config['paths']['logs_dir'] + '/extraction.log'),
                logging.StreamHandler()
            ]
        )
        self.logger = logging.getLogger(__name__)
    
    def _setup_directories(self):
        """Créer les dossiers nécessaires"""
        for dir_path in self.config['paths'].values():
            Path(dir_path).mkdir(parents=True, exist_ok=True)
    
    def extract_from_pdf(self, pdf_path: str, method: str = "zero_shot") -> BatteryData:
        """
        Extraire les données d'un fichier PDF de battery
        
        Args:
            pdf_path: Chemin vers le fichier PDF
            
        Returns:
            BatteryData: Données extraites et validées
        """
        self.logger.info(f"Extraction démarrée pour: {pdf_path}")
        
        try:
            # 1. Extraire le texte du PDF
            text = self.pdf_processor.extract_text(pdf_path)
            self.logger.debug(f"Texte extrait: {len(text)} caractères")
            
            # 2. Extraction avec LLM
            raw_data = self.llm.extract_battery_info(text, method=method)
            self.logger.debug(f"Données brutes extraites: {raw_data}")
            
            # 3. Créer l'objet BatteryData
            battery_data = self._parse_raw_data(raw_data, pdf_path)
            
            # 4. Validation règles métier (plages physiques, cohérences)
            validation_result = self.validator.validate(battery_data)
            battery_data.confidence_score = validation_result.confidence
            battery_data.errors.extend(validation_result.errors)

            # 5. Validation par contraintes OWL de l'ontologie (Niveau 3)
            if self.kg:
                ontology_check = self.kg.validate_with_ontology(battery_data)
                battery_data.ontology_violations.extend(ontology_check["violations"])
                if ontology_check["violations"]:
                    # Pénaliser la confiance proportionnellement au nombre de violations
                    penalty = max(0.5, 1.0 - 0.15 * len(ontology_check["violations"]))
                    battery_data.confidence_score = (battery_data.confidence_score or 1.0) * penalty
                    self.logger.warning(
                        f"Violations OWL pour {battery_data.model}: {ontology_check['violations']}"
                    )

            # 6. Sauvegarder les résultats
            self._save_results(battery_data, pdf_path)

            # 7. Ajouter au Knowledge Graph si activé
            if self.kg:
                self.kg.add_battery(battery_data)
            
            self.logger.info(f"Extraction terminée avec succès pour: {pdf_path}")
            return battery_data
            
        except Exception as e:
            self.logger.error(f"Erreur lors de l'extraction de {pdf_path}: {str(e)}")
            raise
    
    def _parse_raw_data(self, raw_data: Dict, pdf_path: str) -> BatteryData:
        """Convertir les données brutes en BatteryData"""
        llm_model = raw_data.get('model')
        model = llm_model if llm_model and llm_model != 'unknown' else Path(pdf_path).stem

        return BatteryData(
            model=model,
            manufacturer=raw_data.get('manufacturer'),
            chemistry=raw_data.get('chemistry'),
            voltage=self._safe_float(raw_data.get('voltage')),
            voltage_unit=raw_data.get('voltage_unit', 'V'),
            capacity=self._safe_float(raw_data.get('capacity')),
            capacity_unit=raw_data.get('capacity_unit', 'mAh'),
            charge_current=self._safe_float(raw_data.get('charge_current')),
            charge_current_unit=raw_data.get('charge_current_unit', 'A'),
            discharge_current=self._safe_float(raw_data.get('discharge_current')),
            discharge_current_unit=raw_data.get('discharge_current_unit', 'A'),
            internal_resistance=self._safe_float(raw_data.get('internal_resistance')),
            internal_resistance_unit=raw_data.get('internal_resistance_unit', 'mΩ'),
            temperature_range=raw_data.get('temperature_range'),
            dimensions=raw_data.get('dimensions'),
            weight=self._safe_float(raw_data.get('weight')),
            weight_unit=raw_data.get('weight_unit', 'g'),
            cycle_life=self._safe_int(raw_data.get('cycle_life')),
            charge_cutoff_voltage=self._safe_float(raw_data.get('charge_cutoff_voltage')),
            discharge_cutoff_voltage=self._safe_float(raw_data.get('discharge_cutoff_voltage')),
        )
    
    def _safe_float(self, value) -> Optional[float]:
        """Conversion sécurisée en float"""
        if value is None:
            return None
        try:
            # Nettoyer la valeur (supprimer unités, espaces)
            if isinstance(value, str):
                value = ''.join(c for c in value if c.isdigit() or c in '.-')
            return float(value) if value else None
        except (ValueError, TypeError):
            return None
    
    def _safe_int(self, value) -> Optional[int]:
        """Conversion sécurisée en int""" 
        if value is None:
            return None
        try:
            return int(float(str(value)))
        except (ValueError, TypeError):
            return None
    
    def _save_results(self, battery_data: BatteryData, pdf_path: str):
        """Sauvegarder les résultats en JSON"""
        output_file = Path(self.config['paths']['output_dir']) / f"{battery_data.model}.json"
        
        with open(output_file, 'w', encoding='utf-8') as f:
            json.dump(battery_data.to_dict(), f, indent=2, ensure_ascii=False)
        
        self.logger.debug(f"Résultats sauvés: {output_file}")
    
    def extract_batch(self, pdf_directory: str, method: str = "zero_shot") -> List[BatteryData]:
        pdf_files = list(Path(pdf_directory).glob("*.pdf"))
        results = []
        self.logger.info(f"Traitement par lot: {len(pdf_files)} fichiers trouvés (méthode: {method})")
        for pdf_file in pdf_files:
            try:
                results.append(self.extract_from_pdf(str(pdf_file), method=method))
            except Exception as e:
                self.logger.error(f"Échec pour {pdf_file}: {str(e)}")
        return results
    
    def generate_report(self, results: List[BatteryData]) -> Dict:
        """Générer un rapport de performance"""
        total = len(results)
        successful = len([r for r in results if (r.confidence_score or 0) > 0.8])
        
        report = {
            "total_files": total,
            "successful_extractions": successful,
            "success_rate": successful / total if total > 0 else 0,
            "average_confidence": sum(r.confidence_score or 0 for r in results) / total if total > 0 else 0,
            "common_errors": self._analyze_errors(results)
        }
        
        return report
    
    def _analyze_errors(self, results: List[BatteryData]) -> Dict:
        """Analyser les erreurs communes"""
        error_counts = {}
        for result in results:
            for error in result.errors:
                error_counts[error] = error_counts.get(error, 0) + 1
        
        return dict(sorted(error_counts.items(), key=lambda x: x[1], reverse=True)[:5])


if __name__ == "__main__":
    # Test rapide
    extractor = BatteryExtractor()
    result = extractor.extract_from_pdf("3.7V9059156.pdf")
    print(json.dumps(result.to_dict(), indent=2))