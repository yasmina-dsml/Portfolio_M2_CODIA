"""
Module de validation des données extraites
Détecte les erreurs et hallucinations des LLM
"""

import re
import logging
from typing import Dict, List, Optional, Tuple
from dataclasses import dataclass
from enum import Enum


class ValidationLevel(Enum):
    """Niveaux de validation"""
    CRITICAL = "critical"  # Erreurs critiques (valeurs impossibles)
    MAJOR = "major"        # Erreurs importantes (valeurs suspectes)
    MINOR = "minor"        # Avertissements (valeurs inhabituelles)
    INFO = "info"         # Informations (valeurs manquantes)


@dataclass
class ValidationResult:
    """Résultat de validation"""
    is_valid: bool
    confidence: float  # Score 0-1
    errors: List[str]
    warnings: List[str]
    missing_fields: List[str]
    suspicious_values: Dict[str, str]
    
    def add_error(self, message: str, level: ValidationLevel = ValidationLevel.MAJOR):
        """Ajouter une erreur selon son niveau"""
        if level == ValidationLevel.CRITICAL:
            self.errors.append(f"CRITIQUE: {message}")
            self.confidence *= 0.3
        elif level == ValidationLevel.MAJOR:
            self.errors.append(f"MAJEUR: {message}")
            self.confidence *= 0.7
        else:
            self.warnings.append(f"MINEUR: {message}")
            self.confidence *= 0.9


class DataValidator:
    """
    Validateur intelligent pour les données de batteries
    
    Vérifie:
    1. Cohérence des valeurs (plages physiquement possibles)
    2. Unités correctes
    3. Correlations entre champs
    4. Détection d'hallucinations
    """
    
    def __init__(self, config: Dict):
        """
        Initialiser le validateur
        
        Args:
            config: Configuration de validation (règles, seuils)
        """
        self.config = config
        self.logger = logging.getLogger(__name__)
        
        # Règles de validation chargées depuis config
        self.voltage_rules = config.get('voltage', {})
        self.capacity_rules = config.get('capacity', {})
        self.temperature_rules = config.get('temperature', {})
        
        # Patterns de détection d'unités
        self.unit_patterns = {
            'voltage': re.compile(r'(\d+\.?\d*)\s*(V|mV|volt)', re.IGNORECASE),
            'capacity': re.compile(r'(\d+\.?\d*)\s*(mAh|Ah|Wh)', re.IGNORECASE),
            'current': re.compile(r'(\d+\.?\d*)\s*(A|mA|amp)', re.IGNORECASE),
            'resistance': re.compile(r'(\d+\.?\d*)\s*(mΩ|Ω|ohm)', re.IGNORECASE),
            'temperature': re.compile(r'(-?\d+\.?\d*)\s*(°C|°F|K|C)', re.IGNORECASE)
        }
    
    def validate(self, battery_data) -> ValidationResult:
        """
        Valider les données d'une batterie
        
        Args:
            battery_data: Objet BatteryData à valider
            
        Returns:
            ValidationResult: Résultat détaillé de la validation
        """
        result = ValidationResult(
            is_valid=True,
            confidence=1.0,
            errors=[],
            warnings=[],
            missing_fields=[],
            suspicious_values={}
        )
        
        self.logger.info(f"Validation démarrée pour batterie: {battery_data.model}")
        
        # 1. Vérifier les champs obligatoires
        self._check_required_fields(battery_data, result)
        
        # 2. Valider le voltage
        self._validate_voltage(battery_data, result)
        
        # 3. Valider la capacité
        self._validate_capacity(battery_data, result)
        
        # 4. Valider la température
        self._validate_temperature(battery_data, result)
        
        # 5. Valider les courants
        self._validate_currents(battery_data, result)
        
        # 6. Vérifier la cohérence entre champs
        self._check_field_consistency(battery_data, result)
        
        # 7. Détecter les hallucinations potentielles
        self._detect_hallucinations(battery_data, result)
        
        # Score final et statut
        result.is_valid = len(result.errors) == 0 and result.confidence > 0.5
        
        self.logger.info(f"Validation terminée - Valid: {result.is_valid}, Confidence: {result.confidence:.2f}")
        
        return result
    
    def _check_required_fields(self, data, result: ValidationResult):
        """Vérifier la présence des champs obligatoires"""
        required_fields = ['voltage', 'capacity']
        
        for field in required_fields:
            value = getattr(data, field, None)
            if value is None or str(value).lower() in ['unknown', 'none', '']:
                result.missing_fields.append(field)
                result.add_error(f"Champ obligatoire manquant: {field}", ValidationLevel.CRITICAL)
    
    def _validate_voltage(self, data, result: ValidationResult):
        """Valider le voltage"""
        voltage = data.voltage
        
        if voltage is None:
            return
        
        try:
            voltage_float = float(voltage)
            
            # Vérifier la plage générale
            min_v = self.voltage_rules.get('min', 0.1)
            max_v = self.voltage_rules.get('max', 12.0)
            
            if voltage_float < min_v or voltage_float > max_v:
                result.add_error(f"Voltage hors plage réaliste: {voltage}V (attendu: {min_v}-{max_v}V)", 
                               ValidationLevel.CRITICAL)
            
            # Vérifier plages typiques pour lithium
            typical_lithium = self.voltage_rules.get('typical_lithium', [3.2, 3.7, 4.2])
            if not any(abs(voltage_float - typical) < 0.5 for typical in typical_lithium):
                result.add_error(f"Voltage inhabituel pour Li-ion: {voltage}V", ValidationLevel.MINOR)
                result.suspicious_values['voltage'] = f"Valeur inhabituelle: {voltage}V"
                
        except (ValueError, TypeError):
            result.add_error(f"Voltage invalide (non numérique): {voltage}", ValidationLevel.CRITICAL)
    
    def _validate_capacity(self, data, result: ValidationResult):
        """Valider la capacité"""
        capacity = data.capacity
        capacity_unit = getattr(data, 'capacity_unit', 'mAh')
        
        if capacity is None:
            return
        
        try:
            capacity_float = float(capacity)
            
            # Normaliser en mAh pour comparaison
            if capacity_unit.lower() == 'ah':
                capacity_mah = capacity_float * 1000
            else:
                capacity_mah = capacity_float
            
            # Vérifier plage réaliste
            min_cap = self.capacity_rules.get('min', 10)  # mAh
            max_cap = self.capacity_rules.get('max', 100000)  # mAh
            
            if capacity_mah < min_cap or capacity_mah > max_cap:
                result.add_error(f"Capacité hors plage: {capacity}{capacity_unit} ({min_cap}-{max_cap} mAh attendu)", 
                               ValidationLevel.CRITICAL)
            
            # Détecter les ordres de grandeur suspects
            if capacity_mah > 50000:  # > 50Ah inhabituel pour batteries portables
                result.add_error(f"Capacité très élevée: {capacity_mah}mAh", ValidationLevel.MINOR)
                result.suspicious_values['capacity'] = f"Très élevée: {capacity_mah}mAh"
                
        except (ValueError, TypeError):
            result.add_error(f"Capacité invalide: {capacity}", ValidationLevel.CRITICAL)
    
    def _validate_temperature(self, data, result: ValidationResult):
        """Valider la plage de température"""
        temp_range = data.temperature_range
        
        if not temp_range or str(temp_range).lower() == 'unknown':
            return
        
        # Parser la plage de température
        temp_pattern = re.compile(r'(-?\d+\.?\d*).*?(-?\d+\.?\d*)', re.IGNORECASE)
        match = temp_pattern.search(str(temp_range))
        
        if match:
            try:
                temp_min = float(match.group(1))
                temp_max = float(match.group(2))
                
                # Vérifier plages réalistes
                extreme_min = self.temperature_rules.get('operating_min', -50)
                extreme_max = self.temperature_rules.get('operating_max', 100)
                
                if temp_min < extreme_min or temp_max > extreme_max:
                    result.add_error(f"Température hors plage extrême: {temp_range}", ValidationLevel.MAJOR)
                
                # Vérifier cohérence min < max
                if temp_min >= temp_max:
                    result.add_error(f"Plage température incohérente: {temp_range}", ValidationLevel.CRITICAL)
                    
            except (ValueError, TypeError):
                result.add_error(f"Plage température non parsable: {temp_range}", ValidationLevel.MINOR)
    
    def _validate_currents(self, data, result: ValidationResult):
        """Valider les courants de charge/décharge"""
        for current_type in ['charge_current', 'discharge_current']:
            current = getattr(data, current_type, None)
            
            if not current or str(current).lower() == 'unknown':
                continue
            
            try:
                current_float = float(str(current).replace('A', '').replace('mA', '').strip())
                
                # Détecter unité probable
                if 'mA' in str(current) and current_float > 10000:  # > 10A en mA suspect
                    result.add_error(f"{current_type} suspect: {current} (unité mA mais valeur élevée)", 
                                   ValidationLevel.MINOR)
                elif 'A' in str(current) and current_float > 100:  # > 100A suspect pour batteries portables
                    result.add_error(f"{current_type} très élevé: {current}", ValidationLevel.MINOR)
                    
            except (ValueError, TypeError):
                result.add_error(f"{current_type} invalide: {current}", ValidationLevel.MINOR)
    
    def _check_field_consistency(self, data, result: ValidationResult):
        """Vérifier la cohérence entre champs"""
        
        # 1. Cohérence voltage vs chemistry
        voltage = data.voltage
        chemistry = getattr(data, 'chemistry', '')
        
        if voltage and chemistry:
            try:
                voltage_float = float(voltage)
                chemistry_lower = str(chemistry).lower()
                
                # Règles de cohérence chimie/voltage
                if 'lifepo4' in chemistry_lower and not (3.0 <= voltage_float <= 3.6):
                    result.add_error(f"Incohérence LiFePO4/voltage: {chemistry}/{voltage}V", ValidationLevel.MAJOR)
                
                elif 'li-ion' in chemistry_lower and not (3.6 <= voltage_float <= 4.2):
                    result.add_error(f"Incohérence Li-ion/voltage: {chemistry}/{voltage}V", ValidationLevel.MAJOR)
                    
            except (ValueError, TypeError):
                pass
        
        # 2. Cohérence capacité vs dimensions (si disponibles)
        capacity = data.capacity
        dimensions = getattr(data, 'dimensions', '')
        
        if capacity and dimensions and str(dimensions).lower() != 'unknown':
            # Logique simple: très haute capacité devrait avoir dimensions importantes
            try:
                capacity_float = float(capacity)
                if capacity_float > 10000 and any(dim in str(dimensions).lower() for dim in ['small', 'mini', 'coin']):
                    result.add_error("Incohérence capacité/dimensions: haute capacité mais petites dimensions", 
                                   ValidationLevel.MINOR)
            except:
                pass
    
    def _detect_hallucinations(self, data, result: ValidationResult):
        """
        Détecter les hallucinations potentielles
        Basé sur des patterns suspects typiques des LLM
        """
        
        # 1. Valeurs trop "parfaites" ou rondes (souvent inventées)
        voltage = data.voltage
        if voltage:
            try:
                voltage_float = float(voltage)
                # Voltages exactement ronds suspects (3.0, 4.0, 5.0)
                if voltage_float in [1.0, 2.0, 3.0, 4.0, 5.0, 6.0]:
                    result.suspicious_values['voltage'] = "Valeur trop ronde, possiblement inventée"
            except:
                pass
        
        # 2. Capacités multiples de 1000 exactement (souvent inventées)
        capacity = data.capacity
        if capacity:
            try:
                capacity_float = float(capacity)
                if capacity_float > 1000 and capacity_float % 1000 == 0:
                    result.suspicious_values['capacity'] = "Capacité multiple de 1000, possiblement inventée"
            except:
                pass
        
        # 3. Textes génériques typiques d'hallucinations
        suspect_texts = ['standard', 'typical', 'normal', 'varies', 'depends on usage']
        for field in ['chemistry', 'dimensions', 'temperature_range']:
            value = getattr(data, field, '')
            if value and any(suspect in str(value).lower() for suspect in suspect_texts):
                result.suspicious_values[field] = f"Texte générique suspect: {value}"
        
        # 4. Combinaisons impossibles
        voltage = data.voltage
        capacity = data.capacity
        if voltage and capacity:
            try:
                v_float = float(voltage)
                c_float = float(capacity)
                # Puissance théorique Wh = V * Ah
                if hasattr(data, 'capacity_unit') and getattr(data, 'capacity_unit') == 'mAh':
                    wh = v_float * (c_float / 1000)
                else:
                    wh = v_float * c_float
                
                # Détecter puissances impossibles pour batteries portables
                if wh > 1000:  # > 1kWh pour une batterie portable suspect
                    result.add_error(f"Puissance théorique trop élevée: {wh:.1f}Wh", ValidationLevel.MAJOR)
                    
            except:
                pass
        
        # Réduire confiance si trop de valeurs suspectes
        if len(result.suspicious_values) >= 3:
            result.add_error("Trop de valeurs suspectes détectées - possible hallucination", ValidationLevel.MAJOR)


    def validate_batch(self, battery_list: List) -> Dict:
        """
        Valider un lot de batteries et générer un rapport
        
        Args:
            battery_list: Liste d'objets BatteryData
            
        Returns:
            Dict: Rapport de validation du lot
        """
        results = []
        for battery in battery_list:
            validation_result = self.validate(battery)
            results.append({
                'model': battery.model,
                'is_valid': validation_result.is_valid,
                'confidence': validation_result.confidence,
                'error_count': len(validation_result.errors),
                'warning_count': len(validation_result.warnings)
            })
        
        # Statistiques globales
        total_batteries = len(results)
        valid_batteries = sum(1 for r in results if r['is_valid'])
        avg_confidence = sum(r['confidence'] for r in results) / total_batteries if total_batteries > 0 else 0
        
        return {
            'total_batteries': total_batteries,
            'valid_batteries': valid_batteries,
            'validation_rate': valid_batteries / total_batteries if total_batteries > 0 else 0,
            'average_confidence': avg_confidence,
            'detailed_results': results
        }


if __name__ == "__main__":
    # Test du validateur
    from battery_extractor import BatteryData
    
    # Configuration de test
    config = {
        'voltage': {'min': 1.0, 'max': 5.0, 'typical_lithium': [3.2, 3.7, 4.2]},
        'capacity': {'min': 10, 'max': 100000},
        'temperature': {'operating_min': -40, 'operating_max': 85}
    }
    
    validator = DataValidator(config)
    
    # Test avec données valides
    valid_data = BatteryData(
        model="Test_Battery",
        voltage=3.7,
        capacity=2500,
        capacity_unit="mAh",
        temperature_range="-10°C to +60°C"
    )
    
    result = validator.validate(valid_data)
    print(f"Données valides - Valid: {result.is_valid}, Confidence: {result.confidence:.2f}")
    print(f"Erreurs: {result.errors}")
    
    # Test avec données suspectes
    suspicious_data = BatteryData(
        model="Suspect_Battery",
        voltage=10.0,  # Trop élevé
        capacity=100000,  # Très élevé
        capacity_unit="mAh"
    )
    
    result2 = validator.validate(suspicious_data)
    print(f"\\nDonnées suspectes - Valid: {result2.is_valid}, Confidence: {result2.confidence:.2f}")
    print(f"Erreurs: {result2.errors}")
    print(f"Valeurs suspectes: {result2.suspicious_values}")