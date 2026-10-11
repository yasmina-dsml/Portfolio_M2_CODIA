"""
Battery Datasheet Information Extraction System

Module principal pour l'extraction automatique d'informations 
depuis les datasheets de batteries via LLM et validation.
"""

from .battery_extractor import BatteryExtractor, BatteryData
from .pdf_processor import PDFProcessor
from .llm_interface import LLMInterface, ExtractionPrompt
from .validator import DataValidator, ValidationResult, ValidationLevel
from .knowledge_graph import KnowledgeGraph

__version__ = "1.0.0"
__author__ = "M1 SDSC Student"

__all__ = [
    'BatteryExtractor',
    'BatteryData', 
    'PDFProcessor',
    'LLMInterface',
    'ExtractionPrompt',
    'DataValidator',
    'ValidationResult',
    'ValidationLevel',
    'KnowledgeGraph'
]