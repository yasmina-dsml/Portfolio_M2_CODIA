"""
Module de traitement des fichiers PDF
Extrait le texte et les tableaux des datasheets de batteries
"""

import pypdf
import pdfplumber
import logging
from typing import Dict, List, Optional
from pathlib import Path


class PDFProcessor:
    """
    Processeur PDF optimisé pour les datasheets de batteries
    
    Utilise PyPDF2 pour l'extraction basique et pdfplumber 
    pour les tableaux complexes
    """
    
    def __init__(self):
        self.logger = logging.getLogger(__name__)
    
    def extract_text(self, pdf_path: str) -> str:
        """
        Extraire tout le texte d'un PDF
        
        Args:
            pdf_path: Chemin vers le fichier PDF
            
        Returns:
            str: Texte extrait du PDF
        """
        if not Path(pdf_path).exists():
            raise FileNotFoundError(f"Fichier PDF non trouvé: {pdf_path}")
        
        try:
            # Méthode 1: PyPDF2 (rapide)
            text = self._extract_with_pypdf2(pdf_path)
            
            # Méthode 2: pdfplumber (plus précis pour les tableaux)
            if len(text.strip()) < 100:  # Si peu de texte extrait
                text = self._extract_with_pdfplumber(pdf_path)
            
            # Nettoyage du texte
            text = self._clean_text(text)
            
            self.logger.debug(f"Texte extrait de {pdf_path}: {len(text)} caractères")
            return text
            
        except Exception as e:
            self.logger.error(f"Erreur extraction PDF {pdf_path}: {str(e)}")
            raise
    
    def _extract_with_pypdf2(self, pdf_path: str) -> str:
        """Extraction avec PyPDF2"""
        text = ""
        
        with open(pdf_path, 'rb') as file:
            reader = pypdf.PdfReader(file)
            
            for page_num, page in enumerate(reader.pages):
                try:
                    page_text = page.extract_text()
                    text += f"\\n--- Page {page_num + 1} ---\\n"
                    text += page_text
                except Exception as e:
                    self.logger.warning(f"Erreur page {page_num + 1}: {str(e)}")
                    continue
        
        return text
    
    def _extract_with_pdfplumber(self, pdf_path: str) -> str:
        """Extraction avec pdfplumber (meilleur pour tableaux)"""
        text = ""
        
        with pdfplumber.open(pdf_path) as pdf:
            for page_num, page in enumerate(pdf.pages):
                try:
                    # Extraire le texte
                    page_text = page.extract_text() or ""
                    
                    # Extraire les tableaux
                    tables = page.extract_tables()
                    table_text = self._tables_to_text(tables)
                    
                    text += f"\\n--- Page {page_num + 1} ---\\n"
                    text += page_text + "\\n" + table_text
                    
                except Exception as e:
                    self.logger.warning(f"Erreur page {page_num + 1}: {str(e)}")
                    continue
        
        return text
    
    def _tables_to_text(self, tables: List[List[List[str]]]) -> str:
        """Convertir les tableaux en texte structuré"""
        if not tables:
            return ""
        
        table_text = "\\n=== TABLEAUX ===\\n"
        
        for i, table in enumerate(tables):
            table_text += f"\\n--- Tableau {i + 1} ---\\n"
            
            for row in table:
                if row:  # Éviter les lignes vides
                    # Nettoyer et joindre les cellules
                    clean_row = [str(cell).strip() if cell else "" for cell in row]
                    table_text += " | ".join(clean_row) + "\\n"
        
        return table_text
    
    def _clean_text(self, text: str) -> str:
        """
        Nettoyer le texte extrait
        - Supprimer caractères spéciaux problématiques
        - Normaliser espaces et retours à la ligne
        - Préserver structure importante
        """
        if not text:
            return ""
        
        # Remplacer caractères problématiques
        text = text.replace('\\x00', ' ')  # Caractères null
        text = text.replace('\\uf0b7', '•')  # Puces
        text = text.replace('\\uf0a7', '§')  # Sections
        
        # Normaliser les espaces
        lines = text.split('\\n')
        cleaned_lines = []
        
        for line in lines:
            line = line.strip()
            if line:  # Garder seulement lignes non vides
                cleaned_lines.append(line)
        
        # Rejoindre avec un seul retour à la ligne
        cleaned_text = '\\n'.join(cleaned_lines)
        
        # Limiter à une taille raisonnable (éviter token overflow)
        max_chars = 50000
        if len(cleaned_text) > max_chars:
            cleaned_text = cleaned_text[:max_chars] + "\\n... [TEXTE TRONQUÉ] ..."
            self.logger.warning(f"Texte tronqué à {max_chars} caractères")
        
        return cleaned_text
    
    def extract_metadata(self, pdf_path: str) -> Dict:
        """
        Extraire les métadonnées du PDF
        
        Returns:
            Dict: Métadonnées (titre, auteur, nombre de pages, etc.)
        """
        metadata = {
            "file_path": pdf_path,
            "file_size_mb": 0,
            "number_of_pages": 0,
            "title": "",
            "author": "",
            "creation_date": "",
        }
        
        try:
            # Taille du fichier
            file_size = Path(pdf_path).stat().st_size
            metadata["file_size_mb"] = round(file_size / (1024 * 1024), 2)
            
            # Métadonnées PDF
            with open(pdf_path, 'rb') as file:
                reader = pypdf.PdfReader(file)
                
                metadata["number_of_pages"] = len(reader.pages)
                
                if reader.metadata:
                    metadata.update({
                        "title": reader.metadata.get('/Title', ''),
                        "author": reader.metadata.get('/Author', ''),
                        "creation_date": str(reader.metadata.get('/CreationDate', '')),
                    })
        
        except Exception as e:
            self.logger.warning(f"Erreur métadonnées {pdf_path}: {str(e)}")
        
        return metadata
    
    def extract_images_info(self, pdf_path: str) -> List[Dict]:
        """
        Extraire les informations des images (pour analyse future)
        
        Returns:
            List[Dict]: Infos sur les images trouvées
        """
        images_info = []
        
        try:
            with pdfplumber.open(pdf_path) as pdf:
                for page_num, page in enumerate(pdf.pages):
                    if hasattr(page, 'images'):
                        for i, img in enumerate(page.images):
                            images_info.append({
                                "page": page_num + 1,
                                "image_id": i,
                                "width": img.get('width', 0),
                                "height": img.get('height', 0),
                                "x0": img.get('x0', 0),
                                "y0": img.get('y0', 0),
                            })
        
        except Exception as e:
            self.logger.warning(f"Erreur extraction images {pdf_path}: {str(e)}")
        
        return images_info


if __name__ == "__main__":
    # Test du processeur
    processor = PDFProcessor()
    
    # Test avec un fichier d'exemple
    test_file = "3.7V9059156.pdf"
    if Path(test_file).exists():
        text = processor.extract_text(test_file)
        print(f"Texte extrait ({len(text)} chars):")
        print(text[:500] + "..." if len(text) > 500 else text)
        
        metadata = processor.extract_metadata(test_file)
        print(f"\\nMétadonnées: {metadata}")
    else:
        print(f"Fichier de test non trouvé: {test_file}")