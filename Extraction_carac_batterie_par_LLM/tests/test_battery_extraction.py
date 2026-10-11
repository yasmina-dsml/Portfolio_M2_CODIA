"""
Tests unitaires pour le système d'extraction de batteries
Vérifie le bon fonctionnement des composants principaux
"""

import pytest
import sys
from pathlib import Path
import json
from unittest.mock import Mock, patch

# Ajouter le module src au path
sys.path.append(str(Path(__file__).parent.parent))

from src.battery_extractor import BatteryData, BatteryExtractor
from src.validator import DataValidator, ValidationLevel, ValidationResult
from src.pdf_processor import PDFProcessor
from src.llm_interface import LLMInterface, ExtractionPrompt


class TestBatteryData:
    """Tests pour la classe BatteryData"""
    
    def test_battery_data_creation(self):
        """Test création d'un objet BatteryData"""
        battery = BatteryData(
            model="Test_Battery",
            voltage=3.7,
            capacity=2500,
            capacity_unit="mAh"
        )
        
        assert battery.model == "Test_Battery"
        assert battery.voltage == 3.7
        assert battery.capacity == 2500
        assert battery.capacity_unit == "mAh"
    
    def test_battery_data_to_dict(self):
        """Test conversion en dictionnaire"""
        battery = BatteryData(
            model="Test_Battery",
            voltage=3.7,
            capacity=2500
        )
        
        data_dict = battery.to_dict()
        
        assert data_dict["model"] == "Test_Battery"
        assert data_dict["voltage"] == 3.7
        assert data_dict["capacity"] == 2500
        assert "errors" not in data_dict or data_dict["errors"] == []


class TestDataValidator:
    """Tests pour le validateur de données"""
    
    @pytest.fixture
    def validator(self):
        """Fixture pour créer un validateur"""
        config = {
            'voltage': {'min': 1.0, 'max': 5.0, 'typical_lithium': [3.2, 3.7, 4.2]},
            'capacity': {'min': 10, 'max': 100000},
            'temperature': {'operating_min': -40, 'operating_max': 85}
        }
        return DataValidator(config)
    
    def test_valid_battery(self, validator):
        """Test avec une batterie valide"""
        battery = BatteryData(
            model="Valid_Battery",
            voltage=3.7,
            capacity=2500,
            capacity_unit="mAh",
            temperature_range="-10°C to +60°C"
        )
        
        result = validator.validate(battery)
        
        assert result.is_valid == True
        assert result.confidence > 0.8
        assert len(result.errors) == 0
    
    def test_invalid_voltage(self, validator):
        """Test avec un voltage invalide"""
        battery = BatteryData(
            model="Invalid_Battery",
            voltage=10.0,  # Trop élevé
            capacity=2500
        )
        
        result = validator.validate(battery)
        
        assert result.is_valid == False
        assert result.confidence < 0.5
        assert any("voltage" in error.lower() for error in result.errors)
    
    def test_missing_required_fields(self, validator):
        """Test avec des champs obligatoires manquants"""
        battery = BatteryData(
            model="Incomplete_Battery"
            # Voltage et capacité manquants
        )
        
        result = validator.validate(battery)
        
        assert result.is_valid == False
        assert len(result.missing_fields) >= 2
        assert "voltage" in result.missing_fields
        assert "capacity" in result.missing_fields
    
    def test_suspicious_values_detection(self, validator):
        """Test détection de valeurs suspectes"""
        battery = BatteryData(
            model="Suspicious_Battery",
            voltage=4.0,  # Valeur ronde suspecte
            capacity=10000,  # Multiple de 1000 exactement
            capacity_unit="mAh"
        )
        
        result = validator.validate(battery)
        
        # Peut être valide mais avec des valeurs suspectes
        assert len(result.suspicious_values) > 0


class TestPDFProcessor:
    """Tests pour le processeur PDF"""
    
    @pytest.fixture
    def processor(self):
        """Fixture pour créer un processeur PDF"""
        return PDFProcessor()
    
    def test_clean_text(self, processor):
        """Test nettoyage du texte"""
        dirty_text = "  Hello\x00World\uf0b7Test  \n\n\n  Clean  "
        clean_text = processor._clean_text(dirty_text)
        
        assert "\\x00" not in clean_text
        assert "Hello" in clean_text
        assert "World" in clean_text
        assert clean_text.strip() == clean_text
    
    def test_tables_to_text(self, processor):
        """Test conversion de tableaux"""
        tables = [
            [["Voltage", "3.7V"], ["Capacity", "2500mAh"]],
            [["Temperature", "-10°C to +60°C"]]
        ]
        
        table_text = processor._tables_to_text(tables)
        
        assert "TABLEAUX" in table_text
        assert "Voltage" in table_text
        assert "3.7V" in table_text
        assert "Capacity" in table_text
    
    def test_extract_metadata_nonexistent_file(self, processor):
        """Test métadonnées d'un fichier inexistant"""
        metadata = processor.extract_metadata("nonexistent.pdf")
        
        assert metadata["file_path"] == "nonexistent.pdf"
        assert metadata["number_of_pages"] == 0
        assert metadata["file_size_mb"] == 0


class TestLLMInterface:
    """Tests pour l'interface LLM"""
    
    def test_extraction_prompt_creation(self):
        """Test création des prompts"""
        test_text = "Voltage: 3.7V\\nCapacity: 2500mAh"
        prompt = ExtractionPrompt.USER_PROMPT_TEMPLATE.format(text=test_text)
        
        assert "voltage" in prompt.lower()
        assert "capacity" in prompt.lower()
        assert test_text in prompt
    
    def test_parse_response_valid_json(self):
        """Test parsing d'une réponse JSON valide"""
        llm = LLMInterface({'model': 'test', 'api_key': 'test'})

        valid_json = '{"voltage": 3.7, "capacity": 2500}'
        result = llm._parse_response(valid_json)

        assert result["voltage"] == 3.7
        assert result["capacity"] == 2500

    def test_parse_response_invalid_json(self):
        """Test parsing d'une réponse JSON invalide"""
        llm = LLMInterface({'model': 'test', 'api_key': 'test'})

        invalid_json = "Cette réponse n'est pas du JSON valide"
        result = llm._parse_response(invalid_json)

        assert "voltage" in result
        assert result["voltage"] == "unknown"

    def test_default_structure(self):
        """Test structure par défaut"""
        llm = LLMInterface({'model': 'test', 'api_key': 'test'})

        default = llm._default_structure()

        required_fields = ['voltage', 'capacity', 'chemistry', 'temperature_range']
        for field in required_fields:
            assert field in default
            assert default[field] == "unknown"

    def test_is_missing_variants(self):
        """Test détection des valeurs absentes — toutes les variantes"""
        llm = LLMInterface({'model': 'test', 'api_key': 'test'})
        for val in [None, "unknown", "unkown", "n/a", "na", "none", "", "null",
                    "Unknown", "UNKNOWN", "N/A", "None", "NULL"]:
            assert llm._is_missing(val), f"{val!r} devrait être considéré absent"
        for val in [0, 3.7, "3.7", "Li-ion", "mAh"]:
            assert not llm._is_missing(val), f"{val!r} ne devrait pas être absent"

    def test_unit_conversion_current(self):
        """1000 mA == 1 A"""
        llm = LLMInterface({'model': 'test', 'api_key': 'test'})
        pred = {"discharge_current": 1000, "discharge_current_unit": "mA"}
        gt   = {"discharge_current": 1.0,  "discharge_current_unit": "A"}
        assert llm._values_match("discharge_current", 1000, 1.0, pred, gt)

    def test_unit_conversion_capacity(self):
        """86 Ah == 86000 mAh"""
        llm = LLMInterface({'model': 'test', 'api_key': 'test'})
        pred = {"capacity": 86,    "capacity_unit": "Ah"}
        gt   = {"capacity": 86000, "capacity_unit": "mAh"}
        assert llm._values_match("capacity", 86, 86000, pred, gt)

    def test_unit_conversion_weight(self):
        """2.25 kg == 2250 g"""
        llm = LLMInterface({'model': 'test', 'api_key': 'test'})
        pred = {"weight": 2.25, "weight_unit": "kg"}
        gt   = {"weight": 2250, "weight_unit": "g"}
        assert llm._values_match("weight", 2.25, 2250, pred, gt)

    def test_temperature_normalization(self):
        """-20°C to +60°C == -20°C to 60°C"""
        llm = LLMInterface({'model': 'test', 'api_key': 'test'})
        pred = {"temperature_range": "-20°C to +60°C"}
        gt   = {"temperature_range": "-20°C to 60°C"}
        assert llm._values_match("temperature_range", "-20°C to +60°C", "-20°C to 60°C", pred, gt)

    def test_chemistry_partial_match(self):
        """Li-ion Battery contient Li-ion"""
        llm = LLMInterface({'model': 'test', 'api_key': 'test'})
        pred = {"chemistry": "Li-ion Battery"}
        gt   = {"chemistry": "Li-ion rechargeable battery"}
        assert llm._values_match("chemistry", "Li-ion Battery", "Li-ion rechargeable battery", pred, gt)


class TestBatteryExtractor:
    """Tests pour l'extracteur principal"""
    
    def test_safe_float_conversion(self):
        """Test conversion sécurisée en float"""
        # Mock pour éviter l'initialisation complète
        with patch.object(BatteryExtractor, '__init__', lambda x: None):
            extractor = BatteryExtractor()
            
            # Tests de conversion
            assert extractor._safe_float("3.7") == 3.7
            assert extractor._safe_float("3.7V") == 3.7
            assert extractor._safe_float("invalid") is None
            assert extractor._safe_float(None) is None
            assert extractor._safe_float("") is None
    
    def test_safe_int_conversion(self):
        """Test conversion sécurisée en int"""
        with patch.object(BatteryExtractor, '__init__', lambda x: None):
            extractor = BatteryExtractor()
            
            assert extractor._safe_int("1000") == 1000
            assert extractor._safe_int("1000.5") == 1000
            assert extractor._safe_int("invalid") is None
            assert extractor._safe_int(None) is None


class TestIntegration:
    """Tests d'intégration basiques"""
    
    def test_full_pipeline_simulation(self):
        """Test simulation du pipeline complet"""
        # Données d'entrée simulées
        input_text = """
        Model: Test Battery 3.7V
        Nominal Voltage: 3.7V
        Capacity: 2500mAh
        Operating Temperature: -10°C to +60°C
        """
        
        # Simulation extraction LLM
        extracted_data = {
            'voltage': 3.7,
            'capacity': 2500,
            'capacity_unit': 'mAh',
            'temperature_range': '-10°C to +60°C'
        }
        
        # Création objet batterie
        battery = BatteryData(
            model="Test_Battery_3.7V",
            **extracted_data
        )
        
        # Validation
        config = {
            'voltage': {'min': 1.0, 'max': 5.0},
            'capacity': {'min': 10, 'max': 100000},
            'temperature': {'operating_min': -40, 'operating_max': 85}
        }
        validator = DataValidator(config)
        result = validator.validate(battery)
        
        # Vérifications
        assert battery.voltage == 3.7
        assert battery.capacity == 2500
        assert result.is_valid == True
        assert result.confidence > 0.8


def test_project_structure():
    """Test que la structure du projet est correcte"""
    project_root = Path(__file__).parent.parent

    # Dossiers principaux
    for folder in ["src", "config", "data", "tests", "ontology"]:
        assert (project_root / folder).exists(), f"Dossier manquant: {folder}"

    # Fichiers principaux
    assert (project_root / "README.md").exists()
    assert (project_root / "requirements.txt").exists()
    assert (project_root / "main.py").exists()
    assert (project_root / "config" / "config.yml").exists()
    assert (project_root / "ontology" / "battery_ontology.owl").exists()

    # Modules src
    for module in ["battery_extractor.py", "llm_interface.py",
                   "pdf_processor.py", "validator.py", "knowledge_graph.py"]:
        assert (project_root / "src" / module).exists(), f"Module manquant: {module}"


def test_battery_data_ontology_violations():
    """Test que ontology_violations est initialisé vide par défaut"""
    battery = BatteryData(model="Test")
    assert battery.ontology_violations == []
    data = battery.to_dict()
    # Les listes vides ne doivent pas apparaître dans le dict
    assert "ontology_violations" not in data
    assert "errors" not in data


if __name__ == "__main__":
    # Lancer les tests avec pytest
    pytest.main([__file__, "-v"])